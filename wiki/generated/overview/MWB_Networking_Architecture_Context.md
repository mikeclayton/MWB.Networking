# MWB Networking Architecture – Context and Design Narrative

## Purpose of this Document

This document exists to *capture the full architectural intent, rationale, vocabulary, and design decisions* behind the MWB networking codebase as it currently stands. It is designed to be pasted into a new Copilot chat to rehydrate full context when continuing refactors or discussions, without needing to rediscover or re‑explain the design from scratch.

The document is **narrative, not API documentation**. It intentionally avoids concrete code listings except where concepts must be illustrated. The goal is to explain *why* the code looks the way it does, not just *what* it does.

---

## High‑Level Architecture Overview

The networking stack is intentionally layered, with strict separation of responsibility and explicit authority boundaries.

### Layers (conceptual)

- **Layer 0 – Transport**
  - Raw byte transport
  - Connection establishment
  - Reconnection
  - Arbitration between inbound/outbound transports
  - Concurrency and lifecycle management

- **Layer 1 – Framing**
  - Converts byte streams into framed messages
  - Knows nothing about reconnection or transport details

- **Layer 2 – Protocol / Session**
  - Semantic protocol logic
  - Message intent and meaning
  - Multiple logical roles over a single session

Each layer depends *only* on abstractions defined immediately beneath it.

---

## Layer 0: Transport Design

### Core Principles

1. **There is exactly one logical connection** from the perspective of upper layers.
2. Underneath that logical connection, *multiple physical connections may exist over time*.
3. Transport churn (connect, disconnect, reconnect, inbound/outbound races) is **completely hidden** from layers above.
4. Failure semantics are *honest*:
   - Reads and writes may block.
   - Reads and writes may fail.
   - Seamless migration is *explicitly not promised*.

This was summarized early as:

> *There is one connection. Sometimes it blocks. Sometimes it reconnects. If it fails, it fails.*

This statement is the contract of Layer 0.

---

## Physical vs Logical Connections

### INetworkConnection (Physical)

`INetworkConnection` represents a **single concrete transport**:

- One TCP socket
- One pipe
- One QUIC stream
- Etc.

Properties:

- Full‑duplex byte stream
- No reconnection logic
- No readiness logic
- No arbitration
- Disposal closes the underlying resource

If a physical connection fails mid‑read or mid‑write, that failure propagates upward.

---

### ILogicalConnection (Stable Abstraction)

`ILogicalConnection` is what *everything above Layer 0* interacts with.

It represents:

- A **stable logical abstraction** over an unstable reality
- Exactly one conceptual connection

Capabilities:

- Read/write operations
- `WhenReadyAsync` (block until a physical backing connection exists)

Important semantics:

- Read/write *implicitly* wait for readiness
- Read/write may observe concurrent replacement and fail
- Upper layers must tolerate failure

`ILogicalConnection` deliberately hides:

- How many physical connections exist
- Whether inbound or outbound won
- Reconnect timing
- Arbitration rules

---

## Authority vs Usage: Capability Design

### The Problem

Transport providers must be able to *change reality* (attach, replace, dispose physical connections), while consumers must *never be able to do that*.

Assembly‑level visibility (`internal`) was insufficient because:

- Multiple transport providers live in separate assemblies
- Layer boundaries should not be enforced by assembly layout

### The Solution: Capability Interfaces

Authority is granted via **possession**, not via visibility.

Two distinct interfaces exist:

- `ILogicalConnection` – **safe, consumer‑facing usage interface**
- `ILogicalConnectionControl` – **privileged authority interface**

The same concrete object (`LogicalConnection`) implements both, but callers only receive the interface they are entitled to use.

---

## Naming Philosophy: “Here Be Dragons”

The name `ILogicalConnectionControl` was chosen deliberately because it:

- Sounds *slightly wrong* at call sites
- Signals authority and danger
- Discourages casual usage
- Makes misuse obvious even without documentation

This naming is intentionally stronger than terms such as *Commands* or *Attachable*.

---

## LogicalConnectionHandle

### Motivation

Although capability interfaces enforce correctness, exposing them directly caused human‑level confusion.

To improve discoverability and intuition, roles are now **explicitly packaged** in a handle object.

### LogicalConnectionHandle

`LogicalConnectionHandle` bundles:

- `Connection` → `ILogicalConnection` (safe)
- `Control` → `ILogicalConnectionControl` (privileged)

Key traits:

- Constructor is **internal**
- Handle cannot be minted externally
- Authority is only granted by receiving the handle

This mirrors the ergonomic benefits of `ProtocolSessionHandle` while preserving capability security.

---

## Factories – Centralized Creation

### Rule

> *Handles are given, never constructed.*

### LogicalConnectionFactory

A dedicated factory in Layer 0:

- Creates `LogicalConnection`
- Wraps it in a `LogicalConnectionHandle`
- Ensures a single authoritative creation path

Transport providers *retrieve*, never `new`, a handle.

This matches the established `ProtocolSessionFactory` pattern in Layer 2.

---

## Transport Providers

### INetworkConnectionProvider

The provider interface now returns a **handle**, not just a connection:

- Provider establishes transport reality
- Provider owns lifecycle and concurrency
- Provider retains control authority
- Provider hands safe connection upward

The provider is responsible for:

- Listening for inbound connections
- Initiating outbound connections
- Retrying connections
- Arbitrating between candidates
- Attaching physical connections via `ILogicalConnectionControl`

Only one `LogicalConnectionHandle` exists per logical connection.

---

## TCP Transport (Illustrative)

The TCP implementation follows all Layer 0 principles:

- `TcpNetworkConnection`
  - Wraps one `TcpClient`
  - No reconnect logic
  - Enforces `MaxFrameSize`

- `TcpNetworkConnectionProvider`
  - Holds a single `LogicalConnectionHandle`
  - Attaches inbound or outbound candidates
  - Never exposes control to higher layers

Transport policy (retry, arbitration, delays) lives *entirely inside the provider*.

---

## Layer 1: NetworkAdapter

The framing layer depends only on:

- `INetworkConnection`

It explicitly **does not**:

- Call `WhenReadyAsync`
- Reason about connection lifecycle
- Reason about reconnection

Framing operations:

- May block
- May throw
- Propagate failures honestly

Layer 1 trusts Layer 0 to manage connectivity.

---

## Layer 2: Protocol Sessions

### ProtocolSessionHandle

Protocol sessions use a *different* handle pattern:

- Handle is a **role projection**, not authority
- Commands, Observers, Runtime are intentionally public
- No capability security is involved

This distinction is deliberate:

- Transport requires authority segregation
- Protocol requires role clarity

Similar shapes, different semantics.

---

## Concurrency Model Summary

- Physical connection swaps are atomic
- Old physical connections are disposed immediately
- Read/write may race with replacement and fail
- Read/write capture the active connection per operation
- No ref‑counting or seamless migration is attempted

This is a conscious trade‑off favoring honesty over complexity.

---

## Design Invariants (Do Not Break)

1. No layer above Layer 0 observes multiple connections
2. No layer above Layer 0 performs attachment or replacement
3. All authority originates from factories
4. All privileged operations require explicit capability possession
5. All handles are created once and reused

---

## How to Continue Refactoring Safely

When modifying this codebase, always ask:

- *Am I creating authority where there should be none?*
- *Am I leaking transport concerns upward?*
- *Am I introducing hidden lifecycle coupling?*
- *Can the compiler enforce this rule instead of documentation?*

If the answer to any of the above is “yes”, reconsider the change.

---

## Closing Note

This architecture deliberately favors:

- Explicitness over convenience
- Honest failure over illusion
- Capability security over trust
- Compiler enforcement over convention

The result is a transport layer that is:

- Extensible
- Testable
- Understandable
- Safe under concurrency

This document captures the intent behind those decisions and should be used as the shared mental model when evolving the system further.
