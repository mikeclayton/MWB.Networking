# MWB Networking – Layer 1: Framing Architecture Narrative

## Purpose

This document captures the architectural intent and rules for **Layer 1 (Framing)** in the MWB networking stack.

Layer 1 exists to convert raw byte transports into structured frames, while remaining completely ignorant of transport lifecycle.

---

## Role of Layer 1

Layer 1 performs **pure framing**.

It:

- Converts bytes to frames
- Converts frames to bytes

It does **nothing else**.

---

## Dependency Rules

Layer 1 depends **only** on:

- `INetworkConnection`

It must not depend on:

- Providers
- Logical connection control
- Transport details
- Reconnection semantics

---

## Blocking and Failure Semantics

Frame read/write operations:

- May block
- May throw
- Must propagate failure honestly

Layer 1 must not attempt to:

- Wait for readiness explicitly
- Retry operations
- Hide failure

Those responsibilities belong to Layer 0.

---

## NetworkAdapter

`NetworkAdapter` is the canonical Layer 1 component.

Responsibilities:

- Accept an `INetworkConnection`
- Use a frame reader/writer
- Expose frame‑level operations

It must never:

- Call `WhenReadyAsync`
- Reason about connection lifecycle
- Know whether the connection is logical or physical

---

## Design Invariants

- Layer 1 must be transport‑agnostic
- Layer 1 must be stateless with respect to connectivity
- Layer 1 must not contain policy

If Layer 1 knows *why* a connection blocks, the design has been violated.

---

## Design Philosophy

Layer 1 prioritizes:

- Purity
- Single responsibility
- Explicit failure propagation

It is intentionally boring.
