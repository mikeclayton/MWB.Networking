# Networking Subsystem Architecture & Design Rationale (v2)

**Version:** 2.0  
**Audience:** Future maintainers, new contributors, AI context restoration  
**Scope:** End‑to‑end architectural intent, decisions, trade‑offs, and invariants. Not API documentation.

---

## 1. Purpose of This Document

This document exists to **preserve architectural intent and reasoning** for the networking subsystem. It captures:

- what the system is
- why it is shaped the way it is
- which alternatives were explicitly rejected (and why)
- the mental model required to work safely within the design

Its primary purpose is **context restoration**: enabling a future human or AI collaborator to reconstruct the same conceptual model without re‑deriving weeks of decisions.

This document is intentionally opinionated.

---

## 2. System Overview

The system implements a **layered, event‑driven networking stack** that transports structured protocol messages over arbitrary byte‑stream transports (TCP, pipes, in‑memory channels).

Key characteristics:

- stream‑oriented transports
- message semantics preserved via explicit framing
- strict separation of protocol meaning from byte transport
- deterministic lifecycle and failure semantics

The system is **session‑centric**, not connection‑centric:

> A protocol session represents one continuous conversation over exactly one transport connection.

---

## 3. Core Design Philosophy

### 3.1 Explicit Boundaries Over Convenience

Every interaction between layers is represented by a **concrete boundary type**. There is no implicit magic, background behavior, or hidden coupling.

Boundaries exist even when they appear trivial, because they encode *meaning*, not convenience.

Examples:
- `FrameEncoderBridge` exists solely to mark the boundary between encoding and transport
- `NetworkFrameReader` marks the re‑entry point from bytes into semantic space

This makes evolution safe and local.

---

### 3.2 Frames as First‑Class Units

**Frames are the atomic unit of meaning.**

- Encoders and decoders may change byte representation
- Frame boundaries are never split or merged arbitrarily

This enables:
- composable encoding pipelines
- reversible transforms
- predictable diagnostics

---

### 3.3 Push‑Based Decoding, Pull‑Based Consumption

Decoding is **push‑driven**:
- bytes arrive when they arrive
- decoders react to incoming data

Consumption is **pull‑driven**:
- higher layers consume decoded frames at their own pace

This avoids deadlocks and prevents protocol pacing from being coupled to I/O pacing.

---

### 3.4 Transport Is the Edge of Meaning

The transport layer:
- moves bytes only
- knows nothing about frames, messages, or protocol fields

All interpretation of bytes happens **above** the transport.

This allows transports to be freely swapped and uniformly tested.

---

### 3.5 Async‑Capable, Not Async‑By‑Default

Async is used where required by I/O or concurrency, not as a blanket policy.

Guiding rules:
- hot paths avoid unnecessary state machines
- `ValueTask` is preferred where appropriate
- background tasks are explicit, not implicit

---

## 4. Network Pipeline Mental Model

The system is structured as a **bidirectional transformation pipeline**.

### 4.1 Outbound Pipeline

```
ProtocolFrame
  ↓
NetworkFrame
  ↓
NetworkFrameWriter
  ↓
ByteSegments
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

Each stage performs **exactly one transformation**.

---

### 4.2 Inbound Pipeline

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
ProtocolFrame
```

Inbound processing is the strict inverse of outbound processing.

---

## 5. Layer Responsibilities

### 5.1 Protocol Layer

Owns:
- semantics
- state
- requests and streams
- lifecycle

Must never:
- touch raw bytes
- assume framing strategy

---

### 5.2 Framing / Encoding Layer

Owns:
- byte representation
- transformations

Must never:
- perform I/O
- interpret protocol meaning

---

### 5.3 Transport Layer

Owns:
- byte movement
- connection lifecycle

Must never:
- frame messages
- emit or consume protocol frames

---

## 6. Protocol Session Model

A `ProtocolSession`:

- is bound to **one transport connection**
- has a **linear lifecycle**
- owns all protocol state

### 6.1 Lifecycle

```
Built → Started → Ready → Stopped (terminal)
```

Key properties:
- sessions are **not resettable**
- disconnection is terminal
- reconnection requires a **new session**

Queued or in‑flight protocol state does **not** survive disconnection.

---

### 6.2 Queuing Semantics

The session owns an **outbound frame queue**:

- `SendEvent` is non‑blocking
- events are queued before transmission
- queue is session‑scoped

On session termination:
- queued events are lost
- in‑flight requests and streams are aborted

Durability or replay must live **above the session**.

---

## 7. Connection Providers & Arbitration

### 7.1 OS‑Level Reality

TCP connections are identified by a 4‑tuple:

```
(local IP, local port, remote IP, remote port)
```

Simultaneous inbound and outbound connections between peers are valid and distinct at the OS level.

The OS does **not** provide peer‑level session semantics.

---

### 7.2 The Arbitration Problem

The protocol requires:
- exactly one ordering domain
- one stream/request namespace
- one lifecycle

Therefore:

> Exactly **one canonical connection** must be chosen per peer session.

---

### 7.3 Current State: Deliberately Dumb Arbitration

The system explicitly acknowledges the need for arbitration and provides infrastructure for it.

Current stopgap:
- deterministic **direction‑preference arbitration**
- e.g. “prefer inbound” or “prefer outbound”

This is intentionally naive.

It exists to:
- enforce a single connection
- preserve protocol invariants
- make the arbitration boundary explicit

The *quality* of judgment is secondary to the *existence* of judgment.

---

## 8. Application‑Level Producer / Consumer Model

### 8.1 Producer

`KeyboardProducerLoop`:

- converts local keyboard input into protocol events
- uses `SendEvent`
- sends ESC (0x1B) as a protocol‑level exit sentinel

No transport or lifecycle control.

---

### 8.2 Consumer

`KeyboardEventConsumer`:

- passively reacts to `EventReceived`
- decodes payloads
- renders output
- treats ESC as remote exit intent

No loops, no polling, no session pumping.

The protocol drives delivery.

---

## 9. Rejected Alternatives (Updated)

### Dual One‑Way Connections

Rejected because:
- splits ordering domains
- complicates failure semantics
- leaks transport ambiguity into protocol

Even poor arbitration is strictly superior.

---

### Session Reset / Rebinding

Rejected because:
- violates lifecycle causality
- corrupts protocol state
- introduces ambiguous semantics

Recovery is achieved by **replacement**, not reset.

---

### Arbitrary Startup Delays

Rejected once explicit readiness signals exist.

`session.WhenReady` is the authoritative synchronization point.

---

## 10. Invariants — Do Not Break These

- Transport never knows about frames
- Encoders preserve frame boundaries
- Decoders may buffer but never emit partial frames
- NetworkAdapter never handles raw bytes
- Protocol code never sees `ByteSegments`
- A session binds to exactly one connection
- Session termination is final
- Arbitration happens before session creation

Breaking these collapses layers and invalidates reasoning.

---

## 11. Mental Model Summary

If nothing else is remembered:

- Frames enter at `NetworkFrameWriter`
- Frames exit at `NetworkFrameReader`
- Bytes exist only in between
- Transport is intentionally dumb
- Sessions are disposable
- Arbitration exists (even if naive)
- Control signals travel via the protocol, not teardown

This predictability is the entire point of the design.

---

**End of Document (v2)**
