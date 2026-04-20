# Networking Subsystem Architecture & Design Rationale (Extended)

**Version:** 1.1  
**Audience:** Future maintainers, new contributors, AI context restoration  
**Scope:** End-to-end architectural reasoning, not API reference

---

## 1. Purpose of this Document

This document exists to preserve *architectural intent*. It records not only **what** the networking subsystem looks like, but **why** it looks this way, including alternatives that were considered and rejected. The goal is to ensure that future changes reinforce the original design rather than accidentally erode it.

This is not a README, tutorial, or API guide. It is a **context reconstruction document**.

---

## 2. Problem Statement and Constraints

The system solves the problem of transporting structured protocol messages over arbitrary byte-stream transports (TCP, pipes, in-memory channels), while maintaining:

- strict separation of concerns
- predictable streaming behavior
- clean async boundaries
- testability across transport types

Key constraints:
- Transports are stream-based, not message-based
- Partial reads and writes must be supported
- Protocol logic must remain transport-agnostic
- Encodings (compression, encryption, framing) must be composable

---

## 3. Design Philosophy

### 3.1 Explicit Boundaries Over Convenience

The system favors explicit wiring and visible handoff points over convenience abstractions. If two layers interact, that interaction is represented by a concrete type.

Examples:
- FrameEncoderBridge exists solely to mark the boundary between encoding and transport
- NetworkFrameReader explicitly bridges decoded frames back into protocol space

This avoids "action-at-a-distance" designs and makes evolution safer.

---

### 3.2 Frames as First-Class Units

The entire architecture treats **frames** as the atomic unit of meaning. Encoders and decoders never split or merge frames arbitrarily.

This enables:
- composable pipelines
- reversible transformations
- straightforward diagnostics

A frame may change its *byte representation*, but never its *boundary*.

---

### 3.3 Push-Based Decoding, Pull-Based Consumption

Decoding is stream-driven:
- bytes arrive when they arrive
- decoders react to input

Consumption is demand-driven:
- higher layers pull decoded frames from queues

This avoids coupling reads to application pacing and prevents deadlocks common in pull-only designs.

---

### 3.4 Transport Is the Edge of Meaning

The transport layer processes bytes only. From its perspective, there are no frames, messages, headers, or fields.

All interpretation of bytes happens *above* the transport. This allows transports to be swapped freely and tested uniformly.

---

### 3.5 Async-Capable, Not Async-by-Default

Async is used where I/O or concurrency require it. Hot paths avoid unnecessary state machines.

This principle guided:
- use of ValueTask
- explicit synchronous fast paths
- avoidance of implicit background tasks

---

## 4. Network Pipeline Mental Model

The system is structured around a bidirectional transformation pipeline.

### Outbound Pipeline

```
ProtocolFrame
   ↓
ConvertToNetworkFrame
   ↓
NetworkFrame
   ↓
NetworkFrameWriter
   ↓
Serialized ByteSegments
   ↓
Encoder Chain
   • compression
   • encryption
   • framing
   ↓
Encoded ByteSegments
   ↓
FrameEncoderBridge
   ↓
INetworkConnection
   ↓
Native Transport
```

Each stage performs exactly one transformation. No stage is skipped, and no stage performs multiple responsibilities.

---

### Inbound Pipeline

```
Native Transport
   ↓
INetworkConnection
   ↓
ProtocolDriver Read Loop
   ↓
Decoder Chain
   • deframing
   • decryption
   • decompression
   ↓
Decoded ByteSegments
   ↓
NetworkFrameReader
   ↓
NetworkFrame
   ↓
ConvertToProtocolFrame
   ↓
ProtocolFrame
```

Inbound processing is the strict inverse of outbound processing.

---

## 5. Rationale for Nested Encodings

Encodings are applied as nested transforms because each addresses a distinct concern:

- **Compression:** bandwidth and throughput
- **Encryption:** confidentiality and integrity
- **Framing:** boundary reassembly for streaming transports

Keeping them separate allows individual policies to be applied, removed, or reordered without redesign.

---

## 6. Layer Responsibilities

### 6.1 Protocol Layer

Owns semantics, state, and behavior.

Must never:
- touch raw bytes
- assume framing strategy

---

### 6.2 Framing / Encoding Layer

Owns byte representation and transformation.

Must never:
- perform I/O
- interpret protocol meaning

---

### 6.3 Transport Layer

Owns byte movement and I/O lifecycle.

Must never:
- frame messages
- emit or consume frames

---

## 7. Major Components and Their Intent

### ProtocolDriver

Coordinates:
- byte ingestion
- decoder invocation
- protocol session access

It is an orchestrator, not a transformer.

---

### NetworkAdapter

Provides a thin façade for application use:
- Send frame
- Receive frame

This isolates protocol code from pipeline mechanics.

---

### NetworkFrameWriter

Defines the start of the encoding pipeline. It represents the moment protocol meaning becomes bytes.

---

### NetworkFrameReader

Defines the re-entry point into semantic space. All decoded frames pass through it.

---

### FrameEncoderBridge

A deliberate *tool*, not a feature. It connects frame encoders to the transport.

Rationale:
- makes the boundary explicit
- prevents transport from knowing about encoders
- prevents encoders from knowing about transports

---

### IFrameEncoder / IFrameDecoder

Abstract transformation contracts. Implementations must preserve frame boundaries.

Decoder asymmetry (0..N outputs per call) is intentional and reflects streaming reality.

---

### INetworkConnection

Represents the physical or virtual transport.

It is both pipeline origin (inbound) and terminal (outbound).

---

## 8. Rejected Alternatives and Reasons

### Transport-Level Framing

Rejected because it:
- hides frame boundaries
- prevents multi-encoder pipelines
- complicates partial read handling

---

### Block-Oriented Reads (ReadBlockAsync)

Rejected because it:
- misrepresents stream behavior
- couples protocol pacing to I/O pacing

---

### Dual-Purpose Adapters

Adapters performing multiple roles were rejected to avoid implicit coupling and brittle evolution.

---

## 9. Invariants – Do Not Violate

- Transport never knows about frames
- Encoders preserve frame boundaries
- Decoders may buffer but never emit partial frames
- NetworkAdapter never handles raw bytes
- Protocol code never sees ByteSegments

Violating these rules collapses layers and introduces hidden dependencies.

---

## 10. Summary Mental Checklist

If only one idea survives:

- Frames enter at NetworkFrameWriter
- Frames exit at NetworkFrameReader
- Bytes exist only in between
- Transport is dumb by design
- Every boundary is explicit

This predictability is the entire point of the design.

---

**End of Document**
