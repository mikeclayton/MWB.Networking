# The Processing Pipeline - Inbound

This document explains the idea behind the network pipeline: a sequence of frame transformations that turn protocol messages into raw bytes for transport, and then reverse the process on receipt.

The key idea is simple:

> A frame flows through a chain of transformations. Each step adds one capability (compression, encryption, framing) without knowing anything about the other steps.

---

## Overview

```mermaid
sequenceDiagram
    participant Transport as Native Transport<br/>(Pipe / Socket)
    participant Pipeline  as NetworkPipeline<br/>(Decode)
    participant Driver    as ProtocolDriver
    participant ReadLoop  as RunReadLoopAsync
    participant Consume   as ConsumeLoop
    participant Session   as ProtocolSession
    participant Observer  as Session.Observer
    participant App       as Application

    Note over Transport,App: Inbound flow with explicit consume loop

    rect rgba(255,220,200,0.25)
        Note over Transport,Pipeline: Transport

        Transport ->> Pipeline: ReadAsync(bytes)
        Pipeline  ->> ReadLoop: NetworkFrame
    end

    rect rgba(200,255,200,0.25)
        Note over Driver,Session: Session (Protocol + Driver)

        Driver   ->> ReadLoop: start
        ReadLoop ->> Consume: NetworkFrame
        Note right of Consume: Logical queue / channel<br/>may be implicit

        Driver   ->> Consume: start
        Consume  ->> Session: Apply protocol semantics
    end

    rect rgba(200,220,255,0.25)
        Note over Observer,App: Application

        Session  ->> Observer: Event / Request / Stream callback
        Observer ->> App: Handle message
    end

    Note over ReadLoop,Consume: Read loop and consume loop run concurrently.<br/>Decoding and dispatch are decoupled.<br/>Backpressure may exist at this boundary.
    Note over Consume,Observer: Consume loop owns ordering and dispatch.<br/>Observer callbacks must not block.
```

---

## Details

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
