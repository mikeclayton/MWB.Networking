# Network Pipeline Overview

This document explains how data flows through the network stack, from protocol logic down to the actual transport (socket, pipe, or in-memory connection). It focuses on *what happens* at each step, without deep theoretical terminology.

---

## Outbound Flow (Sending data)

```
ProtocolFrame
   |
   | ConvertToNetworkFrame
   v
NetworkFrame
   |
   | NetworkFrameWriter   <-- pipeline entry point
   v
ByteSegments (serialized frame)
   |
   | Frame encoder chain
   |   (length-prefix, compression, encryption, etc.)
   v
ByteSegments (encoded bytes)
   |
   | TransportEncoderSink
   v
INetworkConnection        <-- pipeline end
   |
   v
Socket / Pipe / Memory
```

### What happens step-by-step

1. **ProtocolFrame**
   - Created by the protocol logic.
   - Represents events, requests, responses, etc.

2. **ConvertToNetworkFrame**
   - Adapts protocol concepts into a wire-level format.
   - Keeps protocol logic independent of transport details.

3. **NetworkFrameWriter**
   - Serializes the NetworkFrame into bytes.
   - This is where the network pipeline starts.

4. **Encoder chain**
   - Optional transformations:
     - length-prefix framing
     - compression
     - encryption
   - Each encoder keeps frame boundaries intact.

5. **TransportEncoderSink**
   - Final adapter that hands encoded bytes to the transport.

6. **INetworkConnection**
   - Writes raw bytes to the OS or runtime transport.
   - No knowledge of frames or protocol meaning.

---

## Inbound Flow (Receiving data)

```
Socket / Pipe / Memory
   |
   v
INetworkConnection
   |
   | ProtocolDriver read loop
   v
Raw bytes
   |
   | Frame decoder chain
   |   (decryption, decompression, deframing)
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

### What happens step-by-step

1. **INetworkConnection**
   - Produces raw bytes from the transport.
   - Data may be partial or combined.

2. **ProtocolDriver**
   - Owns the read loop.
   - Feeds bytes into the decoder pipeline.

3. **Decoder chain**
   - Restores frame boundaries.
   - May buffer until a full frame is available.

4. **NetworkFrameReader**
   - Turns a decoded byte frame back into a NetworkFrame.
   - Queues frames for consumption.

5. **ConvertToProtocolFrame**
   - Restores protocol-level meaning.

---

## Key ideas to remember

- `NetworkFrameWriter` is **where the pipeline starts**.
- `INetworkConnection` is **where the pipeline ends**.
- Everything in between only transforms bytes.
- The transport never understands frames.
- Protocol code never touches raw bytes.

---

## One-line summary

> NetworkFrameWriter starts the transformation pipeline, encoders modify bytes, and INetworkConnection is the final bridge to the native transport.
