# The Processing Pipeline - Outbound

This document explains the idea behind the network pipeline: a sequence of frame transformations that turn protocol messages into raw bytes for transport, and then reverse the process on receipt.

The key idea is simple:

> A frame flows through a chain of transformations. Each step adds one capability (compression, encryption, framing) without knowing anything about the other steps.

---

## Outbound Pipeline (Sending Data)

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

* The ProtocolDriver forwards the `NetworkFrame` to the `NetworkAdapter` via `WriteFrameAsync`.

* The NetworkAdapter hands the `NetworkFrame` to the `NetworkFrameWriter`, marking the boundary between protocol semantics and framing/encoding.

* The NetworkFrameWriter serializes the `NetworkFrame` into raw frame bytes (`ByteSegments`) using the frame serializer (producing `[header][payload]`).

* The serialized frame bytes are emitted into the outbound encoder pipeline via the first `PipelineEncoderSink`.

* Each `PipelineEncoderSink` invokes its associated `IFrameEncoder.EncodeFrameAsync`, passing the bytes and the next sink in the chain.

* The `LengthPrefixedFrameEncoder` prepends a length prefix to the serialized frame bytes and emits the resulting `ByteSegments` to the next sink.

* The `AesEncryptingFrameEncoder` encrypts the framed bytes (including the length prefix) and emits encrypted `ByteSegments` to the terminal sink.

* The terminal sink (`FrameEncoderBridge`) forwards the encoded byte segments to the underlying `INetworkConnection` via `WriteAsync`.

* The `INetworkConnection` writes the byte segments to the concrete transport (e.g. TCP stream), resulting in raw bytes being transmitted to the peer.

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

```mermaid
sequenceDiagram
    participant NET as Network Transport
    participant NC  as INetworkConnection
    participant FDB as FrameDecoderBridge
    participant AES as AesDecryptingFrameDecoder
    participant PDS2 as PipelineDecoderSink
    participant LP  as LengthPrefixedFrameDecoder
    participant PDS1 as PipelineDecoderSink
    participant NFR as NetworkFrameReader
    participant NA  as NetworkAdapter
    participant PD  as ProtocolDriver
    participant PS  as ProtocolSession
    participant EM  as EventManager
    participant OBS as Session.Observer

    rect rgba(220,220,220,0.2)
        Note over NET,NC: Layer 0 – Transport
        NET ->> NC: bytes arrive
        NC  ->> FDB: ReadAsync(bytes)
    end

    rect rgba(255,220,220,0.2)
        Note over FDB,NFR: Layer 1 – Framing & Decoding

        FDB ->> AES: DecodeFrameAsync(bytes)
        Note over FDB,AES: ENC([length][header][payload])

        AES ->> PDS2: OnFrameDecoded(bytes)
        Note over AES,PDS2: [length][header][payload]

        PDS2 ->> LP: DecodeFrameAsync(bytes)
        Note over PDS2,LP: [length][header][payload]

        LP  ->> PDS1: OnFrameDecoded(bytes)
        Note over LP,PDS1: [header][payload]

        PDS1 ->> NFR: OnFrameDecoded(bytes)
        Note over PDS1,NFR: [header][payload]
    end

    rect rgba(255,240,200,0.2)
        Note over PD: Protocol Driver
        NFR ->> NA: ReadFrameAsync()
        NA  ->> PD: NetworkFrame
        PD  ->> PD: ToProtocolFrame
        PD  ->> PS: ProcessFrame(ProtocolFrame)
    end

    rect rgba(200,255,200,0.2)
        Note over PS,EM: Layer 2 – Protocol Semantics
        PS ->> EM: ProcessEventFrame<br/>ProcessRequestFrame<br/>ProcessStreamFrame
    end

    rect rgba(200,200,255,0.2)
        Note over OBS: Application API
        EM ->> OBS: EventReceived<br/>RequestReceived<br/>StreamOpened
    end
```


---

## What Each Step Does

### ProtocolFrame

A ProtocolFrame represents application-level intent:
- events
- requests
- responses

It has meaning, but no knowledge of how it will be sent on the wire.

---

### ConvertToNetworkFrame

This step maps protocol concepts into a stable wire-level shape. It isolates protocol logic from transport and encoding details.

---

### NetworkFrameWriter

NetworkFrameWriter is the *entry point* to the network pipeline.

Its responsibilities:
- serialize a NetworkFrame into bytes
- produce one logical unit (ByteSegments)
- hand that unit to the encoder chain

Once data enters this stage, protocol semantics are gone.

---

### Encoder Chain

Encoders are applied in order. Each encoder:
- consumes exactly one frame
- produces exactly one frame
- preserves frame boundaries

Each encoder adds a specific capability:

**Compression (gzip)**
- reduces payload size
- improves bandwidth efficiency

**Encryption (AES)**
- protects confidentiality
- ensures integrity

**Framing (length prefix)**
- allows streaming transports to recover frame boundaries

Encoders do not know about protocol fields or transport details.

---

### TransportEncoderSink

This is the bridge between encoding and transport.

It receives fully-encoded frames and writes the bytes to the network connection.

---

### INetworkConnection

INetworkConnection is the *terminal* of the pipeline.

It:
- writes raw bytes
- reads raw bytes
- knows nothing about frames or protocols

At this boundary, all meaning ends.

---

## Inbound Pipeline (Receiving Data)

The receive path is the exact reverse of the send path.

```
Socket / Pipe / Memory
   |
   v
INetworkConnection
   |
   | ProtocolDriver read loop
   v
Raw byte stream
   |
   | Decoder chain
   |   - remove length prefix
   |   - decrypt
   |   - decompress
   v
ByteSegments (one frame)
   |
   | NetworkFrameReader
   v
NetworkFrame
   |
   | ConvertToProtocolFrame
   v
ProtocolFrame
```

Decoders restore frame boundaries and undo each encoding step until a complete frame is recovered.

---

## Key Properties of the Pipeline

- Each step does *one job*
- No step knows about the whole pipeline
- Frame boundaries are preserved end-to-end
- Transports only move bytes
- Protocol code never touches raw bytes

---

## One-Sentence Summary

> The network pipeline is a sequence of frame transformations where each step adds a capability, and the transport simply moves the resulting bytes without understanding their meaning.
