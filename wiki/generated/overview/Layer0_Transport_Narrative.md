# MWB Networking – Layer 0: Transport Architecture Narrative

## Purpose

This document captures the architectural intent, vocabulary, invariants, and trade‑offs for **Layer 0 (Transport)** in the MWB networking stack. It is intended to be pasted into a new Copilot chat to restore shared context when continuing refactors or design discussions.

This is a **narrative design document**, not API documentation. It explains *why* Layer 0 is shaped the way it is and what must not be broken.

---

## Role of Layer 0

Layer 0 is responsible for **making unreliable transport reality look like a single, stable logical connection** to the rest of the system.

Everything above Layer 0 assumes:

> There is exactly one connection.

Layer 0 absorbs all of the messiness that contradicts that statement.

---

## Core Contract (The Haiku)

Layer 0 obeys the following contract:

> There is one connection.  
> Sometimes it blocks.  
> Sometimes it reconnects.  
> If it fails, it fails.

This is intentional, explicit, and non‑negotiable.

---

## Physical vs Logical Connections

### Physical Connections

A physical connection represents exactly one underlying transport instance:

- One TCP socket
- One named pipe
- One QUIC stream

Physical connections:

- Implement `INetworkConnection`
- Provide raw byte I/O
- Have no reconnection logic
- Have no readiness semantics
- Are disposable

If a physical connection fails, that failure is surfaced immediately.

---

### Logical Connections

A logical connection is what upper layers interact with.

It:

- Implements `ILogicalConnection`
- Represents exactly one conceptual connection
- May change its backing physical connection over time
- Blocks reads/writes until a backing connection exists

Critically, logical connections:

- DO NOT promise seamless migration
- DO NOT mask mid‑operation failure
- DO NOT retry operations

---

## Authority Model

### The Problem

Layer 0 infrastructure must be allowed to:

- Attach physical connections
- Replace physical connections
- Dispose old physical connections

Upper layers must **never** have this authority.

Assembly boundaries alone are insufficient because multiple transport providers exist in separate assemblies.

---

### Capability‑Based Solution

Authority is granted by **possession of a capability**, not by visibility.

Two separate interfaces exist:

- `ILogicalConnection` – safe, consumer‑facing usage
- `ILogicalConnectionControl` – privileged, infrastructure‑only authority

The same concrete object implements both interfaces, but callers are only given the interface they are entitled to use.

---

## "Here Be Dragons" Naming

`ILogicalConnectionControl` is intentionally named to feel slightly wrong.

This is deliberate.

The name:

- Signals authority
- Signals danger
- Discourages casual usage
- Makes misuse obvious even out of context

---

## LogicalConnectionHandle

To make the authority boundary obvious to humans (not just the compiler), roles are explicitly packaged in a handle.

`LogicalConnectionHandle` contains:

- `Connection` → `ILogicalConnection` (safe)
- `Control` → `ILogicalConnectionControl` (privileged)

The constructor is internal.

Handles are received, not constructed.

---

## Factories

All authority originates from factories.

### LogicalConnectionFactory

- Creates the underlying `LogicalConnection`
- Wraps it in a `LogicalConnectionHandle`
- Ensures a single, authoritative creation path

If you see `new LogicalConnectionHandle` outside this factory, something is wrong.

---

## Transport Providers

Transport providers:

- Implement `INetworkConnectionProvider`
- Create or receive a `LogicalConnectionHandle`
- Retain the `Control` interface
- Attach physical connections as they appear

Providers deal with:

- Listening
- Connecting
- Retrying
- Arbitration
- Concurrency

They never expose authority upward.

---

## Concurrency Semantics

- Physical connection replacement is atomic
- Old connections are disposed immediately
- Read/write may race with replacement and fail
- No ref‑counting or graceful migration is attempted

This simplicity is intentional.

---

## What Must Never Happen

- Layer 1+ attaches or replaces connections
- Consumers obtain `ILogicalConnectionControl`
- Handles are constructed directly
- Multiple logical connections are exposed upward

---

## Design Philosophy

Layer 0 prioritizes:

- Honesty over convenience
- Compiler enforcement over convention
- Explicit authority over implicit trust
- Simplicity over illusion

This layer is intentionally sharp.
