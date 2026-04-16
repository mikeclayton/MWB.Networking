# MWB Networking – Layer 2: Protocol & Session Architecture Narrative

## Purpose

This document explains the architectural intent and patterns used in **Layer 2 (Protocol / Session)**.

Layer 2 is where bytes become meaning.

---

## Role of Layer 2

Layer 2 is responsible for:

- Protocol semantics
- Message meaning
- Session state
- Domain‑level operations

It must remain completely agnostic about transport reality.

---

## Session Design

A protocol session:

- Is long‑lived
- Has multiple conceptual roles
- Represents a negotiated semantic context

Unlike transport, **authority is not the concern here**.

---

## ProtocolSessionHandle

Protocol sessions use a **projection handle**, not a capability handle.

`ProtocolSessionHandle`:

- Exposes multiple role‑specific interfaces
- Performs no behavior itself
- Exists purely to make roles explicit

Examples of roles:

- Commands
- Observer
- Runtime

---

## Role Projection vs Authority

Layer 2 handles:

- Visibility
- Intent clarity
- Role separation

It does NOT handle:

- Privileged mutation of shared reality

This is why its handle pattern differs intentionally from Layer 0.

---

## Factories

Protocol sessions are created through factories:

- Centralized creation
- Controlled lifecycle
- No casual construction

This mirrors the transport layer, but for different reasons.

---

## Failure Semantics

Protocol failures:

- Are semantic failures
- May trigger retries or recovery
- Are visible to application logic

Transport failures are already surfaced faithfully by lower layers.

---

## Design Philosophy

Layer 2 prioritizes:

- Clarity of intent
- Explicit roles
- Testability

It assumes the transport is unreliable by design.
