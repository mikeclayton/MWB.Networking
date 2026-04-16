# MWB Networking – Layer 2 Session Stream Model

## Purpose

This document defines the **Session Stream Model** used in **Layer 2 (Protocol / Session)** of the MWB networking stack.

It explains how a protocol session structures communication *over time*, how concurrent operations are represented, correlated, and managed, and how session stream semantics deliberately differ from transport‑level streams.

This document is intended to complement the Layer 2 Architecture Narrative by focusing specifically on **message flow, concurrency, and stream semantics**.

---

## What “Session Streams” Mean (and What They Do Not)

A *session stream* is **not**:

- A TCP stream
- A transport connection
- A physical or logical byte channel

Those concepts live in **Layer 0**.

A session stream *is*:

- A **logical flow of related protocol messages**
- Scoped to a single protocol session
- Defined entirely by protocol semantics

Session streams exist **above framing and transport**.

---

## Why a Session Stream Model Exists

Layer 2 must simultaneously support:

- Multiple in‑flight protocol operations
- Concurrent requests and responses
- Independent flows of control and data
- Asynchronous message delivery

Without a clear model, protocol code tends to become:

- Ad‑hoc
- Heavily stateful
- Implicitly serialized
- Error‑prone under concurrency

The session stream model provides a **structured way to think about concurrency**.

---

## Core Concepts

### Session

A **session** is the top‑level container:

- One negotiated semantic context
- One logical peer relationship
- One lifetime

All session streams live *within* a session.

---

### Stream Identity

Each session stream has:

- A unique **stream identifier** (protocol‑defined)
- A well‑defined lifecycle

The identifier is used to:

- Correlate messages
- Route responses
- Match requests to state

Stream identifiers are purely protocol data.

---

### Directionality

Streams may be:

- Bidirectional
- Request‑initiated (client → server)
- Server‑initiated (push / notification)

Direction is part of the protocol definition, not transport behavior.

---

## Message Classification

All protocol messages belong to exactly one of the following categories:

### 1. Session‑Scoped Messages

- Affect the session as a whole
- Not associated with a specific stream
- Examples: handshake, keepalive, termination

Handled by session‑level logic.

---

### 2. Stream‑Scoped Messages

- Belong to one stream
- Carry stream identifier
- Examples: request, response, continuation

Handled by stream dispatch logic.

---

### 3. Control Messages

- Affect stream lifecycle
- Open, close, cancel
- May originate from either side

Handled by stream control logic.

---

## Stream Lifecycle

A typical stream lifecycle:

1. Stream is created (locally or remotely)
2. Stream enters active state
3. Zero or more messages flow
4. Stream completes or is cancelled
5. Stream resources are released

Streams are **ephemeral**, even when the session is long‑lived.

---

## Concurrency Model

### Independence

Each stream:

- Owns its own state
- Progresses independently
- Must not block unrelated streams

Concurrency is a **core assumption**, not a special case.

---

### Execution Model

Typical execution patterns include:

- Task‑per‑stream
- Cooperative async processing
- Event‑driven dispatch

The model explicitly avoids:

- Global session locks
- Serial message processing
- Hidden synchronization

---

## Failure and Cancellation

### Stream Failure

A stream may fail due to:

- Protocol error
- Timeout
- Remote cancellation

Failure:

- Affects the stream only
- Must not implicitly fail the session

---

### Session Failure

Session termination:

- Terminates all streams
- Propagates failure to callers
- Releases all stream state

This is the only case where streams are force‑closed en masse.

---

## Relationship to Transport Failures

Transport failures:

- Are surfaced as read/write errors
- May occur mid‑stream
- Are not recoverable at the stream level

Session logic may:

- Retry operations
- Re‑establish protocol state

But transport recovery itself is outside Layer 2’s scope.

---

## Role Interfaces and Streams

### Commands Interface

- Typically initiates new streams
- Returns task‑like abstractions
- Owns caller intent

---

### Observer Interface

- Observes inbound streams
- Passive consumption
- Handles push messages

---

### Runtime Interface

- Exposes session and stream statistics
- Diagnostic insight
- No mutation authority

---

## Why This Model Matters

The session stream model ensures:

- Protocol code scales under concurrency
- Responsibilities remain localized
- Failure does not cascade implicitly
- Testing can isolate independent flows

---

## Design Invariants (Do Not Break)

1. Stream state is isolated from other streams
2. Stream failure does not imply session failure
3. Session logic never assumes reliable transport
4. Stream identity is protocol‑defined, not inferred
5. Session streams are protocol concepts, not transport concepts

---

## Design Philosophy

The session stream model emphasizes:

- Explicit concurrency
- Clear ownership
- Predictable lifecycles
- Honest failure semantics

If a protocol seems to “need” transport knowledge, the design has leaked.

---

## Closing Note

This document should be used when:

- Adding new protocol operations
- Introducing concurrency
- Refactoring session internals
- Debugging complex message flows

It exists to keep protocol complexity **structured**, not accidental.
