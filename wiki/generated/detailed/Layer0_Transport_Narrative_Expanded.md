# MWB Networking – Layer 0: Transport Architecture (Expanded)

## Purpose

This document expands on **Layer 0 (Transport)**, detailing its internal structure, authority model, lifecycle, and explicit non‑goals.

---

## Core Mission

Layer 0 exists to create the illusion of a single stable connection over inherently unstable transport mechanisms.

It absorbs:

- Disconnects
- Reconnects
- Races
- Transport variance

---

## Logical vs Physical Recap

Logical connection:

- One identity
- Stable reference
- Visible to upper layers

Physical connections:

- Numerous over time
- Disposable
- Replaceable

---

## Authority and Control

Only Layer 0 may:

- Attach physical connections
- Replace them
- Dispose old ones

This is enforced via explicit control interfaces and handles.

---

## LogicalConnection

`LogicalConnection`:

- Implements both usage and control interfaces
- Exposes only safe usage publicly
- Applies atomic replacement internally

No other object may do this.

---

## Control Surface

`ILogicalConnectionControl`:

- Is intentionally sharp
- Is infrastructure‑only
- Represents authority

Possession implies permission.

---

## Handles and Factories

Handles package:

- Safe usage
- Dangerous authority

Factories ensure:

- Centralized creation
- Controlled authority

This mirrors higher layers but for stronger reasons.

---

## Transport Providers

Providers:

- Own control capability
- Manage concurrency
- Implement retry and arbitration

They are the sole actors allowed to mutate logical connections.

---

## Concurrency Guarantees

Layer 0 guarantees:

- Atomic replacement
- No torn reads
- Immediate disposal of old transports

It does not guarantee seamless migration.

---

## Design Invariants

1. Authority never leaks upward
2. Only one logical connection is visible
3. Physical connections are disposable
4. Reconnection is mechanical, not semantic

---

## Philosophy

Layer 0 favors:

- Honesty
- Sharp edges
- Compiler‑enforced boundaries

It is not friendly, and that is intentional.
