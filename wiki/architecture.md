## Architecture

This project is built on a layered network stack where each layer handles a single responsibility
and has a strictly defined boundary, as follows:

| Layer | Name | Description |
| ----- | ---- | ----------- |
| Layer 0 | Transport | Sends and receives bounded collections of bytes over a communication channel (e.g. tcp, pipes). |
| Layer 1 | Framing   | Converts communication primitives (network frames) into byte streams and vice versa to be transmitted over the Transport layer. |
| Layer 2 | Protocol  | Defines the protocol's semantic messages (e.g. Event, Request, Response), rules, session state and lifecycle invariants.|
| Layer 3 | Runtime   | Pumps protocol messages between a protocol session and its transport layer to advance protocol session state. |
| Layer 4 | Application API | Exposes a clean, intent‑based interface for applications to send and receive protocol data (fire-and-forget Event messages, Request-Response behaviour, native Stream objects). |

This structure ensures that:

* Each layer has a single, well-defined responsibility.
* Lower layers remain reusable and testable in isolation.
* Application code can evolve without coupling to transport or protocol internals.

See below for more details about each layer.

---

## Layer Responsibilities

#### Layer 0 — Transport

* Provides raw byte-oriented communication (e.g. pipes, sockets, streams).
* Handles connection and reconnecting, shutdown, and basic I/O errors.
* Makes no assumptions about message boundaries or protocol semantics.
* Exposes read/write primitives to higher layers.

#### Layer 1 — Framing

* Defines how bytes are grouped into discrete frames or blocks.
* Implements length-prefixing and basic structural validation.
* Converts between raw byte streams and framed byte messages.
* Does not interpret protocol meaning.

#### Layer 2 — Protocol

* Defines the protocol’s semantic concepts (events, requests, streams, responses).
* Enforces protocol invariants and lifecycle rules.
* Translates framed data into structured protocol objects.
* Exposes semantic callbacks and intent-based operations.

#### Layer 3 — Runtime

* Orchestrates asynchronous read/write loops over the transport.
* Connects protocol semantics to framing and I/O.
* Manages concurrency, cancellation, ordering, and shutdown behavior.
* Normalizes transport and framing errors into protocol-level failures.

#### Layer 4 — Application API

* Exposes a clean, intent-driven API for application code.
* Hides protocol mechanics, identifiers, and transport details.
* Presents events, requests, responses, and streams as first-class objects.
* Translates protocol errors into application-meaningful outcomes.
---
