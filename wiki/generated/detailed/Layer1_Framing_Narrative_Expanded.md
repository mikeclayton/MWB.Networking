# MWB Networking – Layer 1: Framing Architecture (Expanded)

## Purpose

This document describes **Layer 1 (Framing)** in greater detail. Layer 1 exists as the mechanical translator between byte streams and structured protocol messages.

---

## Responsibilities

Layer 1:

- Transforms byte streams into frames
- Transforms frames into byte streams

Nothing more.

---

## Framing Boundary

Layer 1 enforces message boundaries.

It does *not*:

- Infer meaning
- Infer intent
- Manage connection state
- Retry operations

---

## NetworkAdapter

`NetworkAdapter` is the core Layer 1 abstraction.

It:

- Accepts an `INetworkConnection`
- Uses a reader/writer pair to read and write frames

The adapter does not care whether:

- The connection is logical or physical
- The connection is newly established or reconnected

---

## Blocking Semantics

Layer 1 assumes:

- Reads may block indefinitely
- Writes may block
- Either may fail

It must never attempt to mask this behavior.

---

## Failure Propagation

Layer 1:

- Does not retry
- Does not reconnect
- Does not swallow errors

Failures propagate directly to Layer 2.

---

## Design Constraints

Layer 1 must remain:

- Stateless regarding connectivity
- Transport‑agnostic
- Easy to test in isolation

If Layer 1 grows policy, the architecture has been violated.
