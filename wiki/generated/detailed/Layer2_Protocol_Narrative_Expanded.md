# MWB Networking – Layer 2: Protocol & Session Architecture (Expanded)

## Purpose

This document describes **Layer 2 (Protocol / Session)** in detail: its responsibilities, internal structure, lifecycle, APIs, and how it relates to lower layers. It is intended to fully rehydrate context for future refactors and design work.

Layer 2 is where **bytes become meaning**.

---

## Responsibilities of Layer 2

Layer 2 is responsible for:

- Defining protocol semantics
- Interpreting framed messages
- Managing session lifecycle
- Exposing a clear API to application logic
- Coordinating multiple roles over a single session

Layer 2 is *explicitly not* responsible for:

- Transport establishment
- Reconnection
- Ensuring delivery
- Framing or byte handling

Those concerns are handled by lower layers.

---

## Relationship to Lower Layers

Layer 2 depends on:

- Layer 1 for framed messages
- Layer 0 for connectivity and lifecycle hiding

Layer 2 assumes:

- Reads and writes may block
- Reads and writes may fail
- Connections may silently change beneath it

Protocol logic must therefore be:

- Resilient to failure
- Explicit about retries and recovery
- Stateless where possible

---

## Session Concept

A **protocol session** represents:

- A negotiated semantic context between peers
- A long‑lived logical interaction
- A boundary for protocol state

A session typically encapsulates:

- Negotiated parameters
- Authentication state
- Feature support
- In‑flight operations

---

## Lifecycle of a Session

Typical lifecycle:

1. Session factory is invoked
2. Session binds to a framed connection
3. Protocol handshake occurs
4. Session enters steady‑state operation
5. Errors or disconnects occur
6. Session shuts down or is replaced

Layer 2 never attempts to fix transport problems; it reacts to them.

---

## API Surface Philosophy

Layer 2 exposes **role‑specific APIs**, not a monolithic interface.

Different responsibilities are separated intentionally.

This avoids:

- God objects
- Ambiguous ownership
- Unclear call intent

---

## ProtocolSessionHandle

### Role Projection Handle

`ProtocolSessionHandle` is a **projection handle**, not a capability handle.

It exists to:

- Make session roles explicit
- Clarify intent at call sites
- Package multiple interfaces safely

It does **not**:

- Grant authority
- Perform logic itself
- Enforce security boundaries

---

## Common Session Roles

Typical roles exposed via the handle include:

### Commands

- Intentional operations
- API used by application code
- Examples: SendMessage, CloseSession

### Observer

- Passive observation
- Events, notifications, telemetry

### Runtime

- Operational state
- Diagnostics or status queries

Each of these roles is represented by a distinct interface.

---

## Why This Pattern Is Different from Layer 0

Layer 2 deals in **semantic intent**, not authority.

Calling a session command is:

- Expected
- Safe
- Part of normal usage

By contrast, Layer 0 control operations mutate transport reality and must be protected.

This is why:

- Layer 2 uses projection handles
- Layer 0 uses capability + control patterns

---

## Error Handling and Failure Semantics

Layer 2:

- Receives failures from lower layers
- Decides whether recovery is possible
- Decides whether retries occur
- Decides whether errors are fatal

Failures are part of normal operation.

Layer 2 must never assume a reliable transport.

---

## Session Streams and Concurrency

Protocol sessions often involve:

- Concurrent operations
- Request/response correlation
- Independent message flows

Concurrency control belongs to Layer 2.

Layer 0 only guarantees that bytes eventually arrive or fail.

---

## Factories and Construction

Sessions are created via factories:

- Ensures consistent setup
- Ensures correct wiring
- Prevents casual construction

Factories may:

- Bind sessions to adapters
- Start background tasks
- Register observers

---

## Invariants (Do Not Break)

1. Layer 2 never manipulates transport directly
2. Session roles remain separated
3. Factories remain the only creation path
4. Session APIs express intent, not mechanics

---

## Design Philosophy

Layer 2 prioritizes:

- Clarity of intent
- Explicit roles
- Testability
- Separation of concerns

It assumes everything below it is unreliable by design.
