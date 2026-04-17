# The Network Pipeline

This document explains the idea behind the network pipeline: a sequence of frame transformations that turn protocol messages into raw bytes for transport, and then reverse the process on receipt.

The key idea is simple:

> A frame flows through a chain of transformations. Each step adds one capability (compression, encryption, framing) without knowing anything about the other steps.

---

## Outbound Pipeline (Sending Data)

```
ProtocolFrame
   |
   | ConvertToNetworkFrame
   v
NetworkFrame
   |
   | NetworkFrameWriter   (pipeline entry point)
   v
ByteSegments (serialized frame)
   |
   |  - e.g. gzip (compression)
   |
ByteSegments (serialized frame)
   |
   |  - e.g. AES (encryption)
   |
ByteSegments (serialized frame)
   |
   |  - length prefix (framing)
   v
ByteSegments (fully encoded)
   |
   | FrameEncoderBridge
   v
INetworkConnection   (pipeline end)
   |
   v
Socket / Pipe / Memory
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
