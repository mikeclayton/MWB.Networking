# The Processing Pipeline - Outbound

This document explains the idea behind the outbound network pipeline, which is  a sequence of frame transformations that turn protocol messages into raw bytes for transport, and then reverse the process on receipt.

The key idea is simple:

> A frame flows through a chain of transformations. Each step adds one capability (compression, encryption, framing) without knowing anything about the other steps.

---

## Overview

```mermaid
sequenceDiagram
    participant App       as Application
    participant Session   as ProtocolSession
    participant Queue     as OutboundFrameQueue
    participant Driver    as ProtocolDriver
    participant WriteLoop as RunWriteLoopAsync
    participant Pipeline  as NetworkPipeline<br/>(Encode)
    participant Transport as Native Transport<br/>(Pipe / Socket)

    %% =====================================================
    %% Application role
    %% =====================================================
    rect rgba(200,220,255,0.25)
        Note over App: Application
        App ->> Session: Commands.SendEvent(eventType, payload)
    end

    %% =====================================================
    %% Session role (protocol + queue + driver)
    %% =====================================================
    rect rgba(200,255,200,0.25)
        Note over Session,WriteLoop: Session (Protocol + Driver)

        Session ->> Queue: Enqueue ProtocolFrame
        Note right of Queue: In-memory<br/>fast, non-blocking

        Driver ->> WriteLoop: start
        Queue ->> WriteLoop : Dequeue ProtocolFrame
    end

    %% =====================================================
    %% Transport role (pipeline + native I/O)
    %% =====================================================
    rect rgba(255,220,200,0.25)
        Note over Pipeline,Transport: Transport

        WriteLoop ->> Pipeline: Serialize + Encode
        Pipeline ->> Transport: WriteAsync(bytes)
    end

    %% =====================================================
    %% Key behaviour note
    %% =====================================================
    Note over Queue,WriteLoop: SendEvent only enqueues.<br/>RunWriteLoopAsync drains the queue asynchronously.<br/>Enqueue and write overlap unless explicitly separated.
```

---

## Details

THe diagram below shows each step in a hypotheical session configured to perform 2 encoding steps in the processing pipeline:

* Length-prefixed encoding
* AES encryption

This session can be constructed using the following SessionBuilder utility methods which configures all of the plumbing in between the two specified codecs (i.e. encoder / decoder pairs):

```csharp
var serverSession =
    new ProtocolSessionBuilder()
        .WithLogger(logger)
        .UseOddStreamIds()
        .ConfigurePipeline(pipeline =>
        {
            pipeline
                .AppendFrameCodec(
                    new LengthPrefixedFrameEncoder(logger),
                    new LengthPrefixedFrameDecoder(logger))
                .AppendFrameCodec(
                    new ASEEncrytionEncoder(logger, aesOptions),
                    new ASEEncrytionDecoder(logger, aesOptions))
                .UseConnection(() => serverConnection);
        })
        .Build();
```

The application then sends an Event (a one-way fire-and-forget message) by invoking `SendEvent(eventType, payload)` on the session’s command interface.

This triggers a sequence of transformations that ultimately results in bytes being sent to the peer (although this has a large number of steps, in tests the library has been able to enqueue over 1,000,000 messages per second onto the network connection for tramsission):

```mermaid
sequenceDiagram
    participant APP as Application
    participant SC as Session.Commands
    participant EM as EventManager
    participant PS as ProtocolSession
    participant PD as ProtocolDriver
    participant NA as NetworkAdapter
    participant NFW as NetworkFrameWriter
    participant PES1 as PipelineEncoderSink
    participant LP as LengthPrefixedFrameEncoder
    participant PES2 as PipelineEncoderSink
    participant AES as AesEncryptingFrameEncoder
    participant FEB as FrameEncoderBridge
    participant NC as INetworkConnection
    participant NET as Network Transport

    rect rgba(200,200,255,0.2)
      Note over APP,SC: Application API
      APP->>SC: SendEvent<br/>SendRequest<br/>OpenSessionStream
    end

    rect rgba(200,255,200,0.2)
      Note over EM,PS: Layer 2 – Protocol Semantics
      SC->>EM: SendEvent
      EM->>PS: EnqueueOutboundFrame(ProtocolFrame)
    end

    rect rgba(255,240,200,0.2)
      Note over PD: Protocol Driver
      PS->>PD: Outbound frame available
      PD->>PD: WaitForOutboundFrameAsync
      PD->>PD: ToNetworkFrame
    end

    rect rgba(255,220,220,0.2)
      Note over NA,FEB: Layer 1 – Framing & Encoding

      PD->>NA: WriteFrameAsync(NetworkFrame)
      NA->>NFW: WriteAsync(NetworkFrame)

      NFW->>NFW: SerializeFrame

      NFW->>PES1: OnFrameEncoded(bytes)
      Note over NFW,PES1: [header][payload]

      PES1 ->> LP: EncodeFrameAsync(bytes)

      LP->>PES2: OnFrameEncoded(bytes)
      Note over LP,PES2: [length][header][payload]

      PES2->>AES: EncodeFrameAsync(bytes)

      AES->>FEB: OnFrameEncoded(bytes)
      Note over AES,FEB: ENC([length][header][payload])
    end

    rect rgba(220,220,220,0.2)
      Note over NC,NET: Layer 0 – Transport
      FEB->>NC: WriteAsync(bytes)
      NC->>NET: bytes on wire
    end
```

---

#### Application API

* The application invokes a protocol command method (`SendEvent`, `SendRequest`, `OpenSessionStream`, etc.) on the session’s `IProtocolSessionCommands` interface.

* The session’s command interface forwards the call to the appropriate domain helper (`EventManager`, `RequestManager`, or `StreamManager`) based on the intent being expressed.

---

#### Layer 2 - Protocol

* The domain helper validates the operation against protocol rules (e.g. stream state, request lifecycle, duplicate IDs) and constructs a `ProtocolFrame` representing the semantic intent.

* The domain helper enqueues the ProtocolFrame onto the session’s internal outbound frame queue via `ProtocolSession.EnqueueOutboundFrame`.

* The ProtocolDriver "write loop" wakes via WaitForOutboundFrameAsync and dequeues the next `ProtocolFrame` from the session.

---

#### Layer 2 - Protocol Driver

* The Protocol Driver converts the `ProtocolFrame` into a transport-agnostic `NetworkFrame` (`ToNetworkFrame`), stripping protocol-only semantics and retaining only framing-relevant metadata and payload bytes.

---

#### Layer 1 - Framing

* The ProtocolDriver forwards the `NetworkFrame` to the `NetworkAdapter` via `WriteFrameAsync`.

* The NetworkAdapter hands the `NetworkFrame` to the `NetworkFrameWriter`, marking the boundary between protocol semantics and framing/encoding.

* The NetworkFrameWriter serializes the `NetworkFrame` into raw frame bytes (`ByteSegments`) using the frame serializer (producing `[header][payload]`).

* The serialized frame bytes are emitted into the outbound encoder pipeline via the first `PipelineEncoderSink`.

* Each `PipelineEncoderSink` invokes its associated `IFrameEncoder.EncodeFrameAsync`, passing the bytes and the next sink in the chain.

* The `LengthPrefixedFrameEncoder` prepends a length prefix to the serialized frame bytes and emits the resulting `ByteSegments` to the next sink.

* The `AesEncryptingFrameEncoder` encrypts the framed bytes (including the length prefix) and emits encrypted `ByteSegments` to the terminal sink.

---

#### Layer 0 - Transport

* The terminal sink (`FrameEncoderBridge`) forwards the encoded byte segments to the underlying `INetworkConnection` via `WriteAsync`.

* The `INetworkConnection` writes the byte segments to the concrete transport (e.g. TCP stream), resulting in raw bytes being transmitted to the peer.
