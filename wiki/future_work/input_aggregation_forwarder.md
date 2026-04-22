
# Future Work Notes: Input Aggregation, Forwarder, and Deterministic Input State Machine

## Overview

This document summarises a proposed *future* architecture for handling high‑frequency, semantically rich input (keyboard and mouse) in the MWB project. The design introduces a **forwarder with an input state machine** that aggregates raw input events, restores semantic meaning, and feeds a normalised stream of input into the existing `ProtocolSession` via a pump.

The goals of this design are:

- Preserve **semantic correctness** (ordering, modifiers, decoration such as CTRL + WHEEL).
- Reduce **transport pressure** by batching and aggregation.
- Maintain **deterministic behaviour** suitable for exhaustive testing.
- Keep **clear separation of concerns** between capture, semantics, and transport.

---

## High‑Level Pipeline

```
┌─────────────────────┐
│   Input Producer    │  (OS callbacks / Console / WinAPI)
│  (capture only)     │
└─────────┬───────────┘
          │ enqueue(raw)
          ▼
┌─────────────────────┐
│ ConcurrentQueue<T>  │  Raw input queue
└─────────┬───────────┘
          │ dequeue FIFO
          ▼
┌────────────────────────────────────┐
│ Semantic Input State Machine       │
│  - keyboard + mouse combined       │
│  - modifier tracking               │
│  - aggregation / coalescing        │
│  - semantic boundaries             │
│  - injected clock (IStopwatch)     │
└─────────┬──────────────────────────┘
          │ emit canonical events
          ▼
┌─────────────────────┐
│ ConcurrentQueue<T>  │  Canonical output queue
└─────────┬───────────┘
          │ dequeue
          ▼
┌─────────────────────┐
│ Pump (Event / Req / │
│ Stream – one only)  │
└─────────┬───────────┘
          │ send
          ▼
┌─────────────────────┐
│  ProtocolSession    │
└─────────────────────┘
```

---

## Design Rationale

### 1. Producer: Capture Only

**Responsibilities**:
- Capture raw OS input as quickly as possible.
- Preserve exact arrival order.
- Enqueue events without interpretation.

**Non‑responsibilities**:
- No protocol logic
- No batching
- No timing decisions
- No back‑pressure logic

This guarantees the capture path remains extremely fast and safe under load.

---

### 2. Raw Input Queue (ConcurrentQueue)

- Serves as the hand‑off boundary between capture and semantics.
- FIFO ordering defines the authoritative input timeline.
- Multiple producers, single consumer (state machine).

This queue is *owned by the pipeline*, not the producer or pump.

---

### 3. Semantic Input State Machine (Forwarder)

This is the **heart of the design**.

#### Responsibilities

- Consume raw input in strict FIFO order.
- Maintain a *virtual input state*:
  - keyboard modifier state
  - mouse pointer state (relative / absolute)
- Aggregate mouse movement:
  - `(dx=5, dy=1) + (dx=5, dy=1) → (dx=10, dy=2)`
- Normalise mixed representations:
  - relative + absolute + relative → single absolute
- Enforce **semantic boundaries**:
  - keyboard events
  - mouse button up/down
  - mode changes
- Emit the *minimal semantically equivalent* canonical events.

All decisions about *meaning* happen here, in one place.

---

### 4. Combined Keyboard + Mouse Stream

Keyboard and mouse input **must not be split across streams**. Examples like `CTRL + mouse wheel` require total ordering across devices.

The state machine therefore processes:
- KeyDown / KeyUp
- MouseMove (relative / absolute)
- MouseButton
- MouseWheel

as **one combined timeline**.

---

### 5. Time‑Based Flushing and IStopwatch

To prevent unbounded buffering of mouse movement, the state machine uses a small time window (e.g. ~5 ms) to flush accumulated state.

#### Important rule

> **Time affects *when* events flush, not *what* events flush.**

Semantic correctness is *never* time‑dependent.

#### IStopwatch Injection

The state machine depends on an injected time source:

```
interface IStopwatch
{
    long ElapsedTicks { get; }
}
```

- Production: wraps `System.Diagnostics.Stopwatch`
- Tests: mock / driven clock

This allows:
- Deterministic tests
- Precise simulation of timeouts
- No reliance on real scheduler behaviour

---

### 6. Deterministic Test Mode

When the timeout is disabled (e.g. zero duration):

- No wall‑clock dependency
- State machine becomes a *pure transformation*:

```
InputEvent[] → CanonicalInputEvent[]
```

Benefits:
- Entire input behaviour testable via input/output fixtures
- Fuzz and property‑based testing possible
- Debugging via replay and early divergence detection

---

### 7. Canonical Output Queue

- State machine emits canonical input events into a second `ConcurrentQueue`.
- Represents fully normalised, semantically valid input.
- Single consumer: the pump.

Separation ensures transport concerns cannot affect semantics.

---

### 8. Pump Layer (Exactly One Active)

A **pump** bridges canonical events to the `ProtocolSession`.

Only *one* pump runs at a time:

- **Event pump**: fire‑and‑forget
- **Request pump**: send + await response
- **Stream pump**: setup once, send many, teardown

All pumps:
- Own `session.WhenReady`
- Respect cancellation tokens
- Treat transport exceptions as normal shutdown

This makes lifecycle semantics explicit in code.

---

### 9. ProtocolSession Interaction

The `ProtocolSession`:
- Receives already‑normalised input
- Enforces protocol ordering
- Manages request / stream state
- Remains transport‑agnostic

The session is **never exposed to raw or noisy input**.

---

## Why This Architecture Works

- Preserves semantic correctness under extreme input rates
- Minimises transport overhead via aggregation
- Keeps protocol code simple and honest
- Makes shutdown behaviour predictable
- Enables deterministic, exhaustive testing
- Scales naturally to mouse, touch, and future devices

Most importantly:

> **Meaning is restored *before* transport, not after.**

---

## Summary

This forwarder‑based, state‑machine‑driven input pipeline establishes a robust foundation for future work in MWB. It draws a clean line between capture, semantics, and transport while remaining efficient, testable, and maintainable.

This design is intentionally deferred, but all current producer / queue / pump work aligns directly with it.
