## MWB.Networking

---

### What is this?

**MWB.Networking** is a blue sky research experiment in writing a replacement networking stack for
the PowerToys application "Mouse Without Borders".

Mouse Without Borders currently has a networking stack which is deeply entwined into the
application, with logic and state spread across a number of areas that are shared with
application logic and ui interactions. This makes it difficult
to reason about and hard to improve or add new features.

This project is an attempt to build a full replacement for the networking stack that will
encapsulate all of the networking details into an isolated, testable layer so that the
application-level code can focus on behaviour rather than network communication.

Note that there's no expectation that this will ever actually make it into the live
Mouse Without Borders project, but hopefully there'll be enough lessons learned along the way
to be able to feed some incremental ideas and improvements into the existing code.

---

### Objectives

* To implement a subset of features that would be required by a full replacement
network stack for Mouse Without Borders in order to research design requirements,
architecture decisions, implementation details.

* To investigate how a hypothetical integration and deployment of the new network
stack into Mouse Wthout Borders would work, ensuring incremental adoption and
minimising risk at each step.

* To determine how these features might be used in a proposed integration with
Mouse Jump to allow the mouse pointer to be jumped onto remote MWB peers.

---

### Status

This project is in heavy flux - it's an experiment not a product, so expect things
to be changing a lot and for there to be a lot of rough edges and incomplete areas.

The main goal is to explore ideas and learn, not to build a polished library, so don't
take a dependency from stable systems (yet!).

---

### Roadmap

These are some goals to help focus research, and avoid features being built "just because"...

#### Core Functionality

* ✅ **Layer 0 frame transport**
  * Write a *working* Layer 0 unit test that transmits network frames between
    in-memory writer and reader endpoints
  * Requires encoding and decoding of network frames
  * Requires a Layer 0 (transport) write loop and read loop

* ✅ **Layer 1 protocol encoding**
  * Write a *working* Layer 1 unit test that invokes protocol commands (e.g. `SendEvent`)
    on a sender session and raises corresponding events (e.g. `EventReceived`)
    on a receiver session
  * Requires protocol session and driver implementations
  * Requires encoding and decoding of protocol frames
  * Focuses on correctness, ordering, and lifecycle 

* ✅ **Layer 2 protocol semantics (application-facing API)**
  * Encapsulate Layer 1 mechanics in a semantic interface
    * SendEvent
    * "Request -> Response" lifecycle
    * OpenStream, SendData, CloseStream lifecycle
  * Requires an application‑level API

* ✅ **One‑way console app sample**
  * Build a console application that transmits individual keystrokes and
    echoes them at a remote peer
  * Exercises the session builder interface
  * Exercises the session application API
  * Exercises the Event lifecycle model (SendEvent, EventReceived)
  * Exercises TCP network connections

* ✅ **Two‑way console app sample** with Pipes
  * Proves bi‑directional communication
  * Exercises the request‑response model
  * Tests Pipes provider and connections
  * **Defers** testing connection arbitration

* **Two‑way console app sample** with TCP
  * Proves bi‑directional communication
  * Exercises the request‑response model
  * Tests TCP provider and connections
  * **Does** test connection arbitration

#### Mouse Without Borders Prerequisites

* **Layer-0 Transport encryption**
  * Transparently Protects transport / network frames
  * Lives cleanly at Layer 0 (bytes in, bytes out)
  * Does not require identity or trust decisions yet

* **App-layer handshake poc**
  * prove access to a shared secret without revealing the secret itself

* Connection status and health reporting
  * must be readable from the application layer
  * enables user‑visible diagnostics and troubleshooting (e.g. settings UI coloured borders)

* Automated reconnection and recovery
  * recover from transient transport failures
  * validates logical connection stability under real network churn

* Capability negotiation and feature discovery
  * supports incremental protocol evolution and safe rollout
  * allows peers to negotiate capabilities up or down (old stack / new stack)
  
* Telemetry
  * report key transport, protocol, and lifecycle events

#### Mouse Jump Integration

* Passive integration into MWB codebase
  * no features active, just codebase integrated into project
  * in advance of any Mouse Jump features

* Activation with MWB for a PoC message
  * prove integration / negotiation / handshaking
  * in advance of any Mouse Jump features

* Enable Mouse Jump features
  * prove integration / negotiation / handshaking
  * in advance of any Mouse Jump features
---

### Layered Design

In the briefest way possible, this project is split into layers of responsibility:

* **Layer 0** - pushing network bytes
  *  ```WriteAsync```, ```ReadAsync```
* **Layer 1** - protocol encoding layer
  * ```EncodeAsync```, ```DecodeAsync```,
* **Layer 2**
  * Protocol semantics
    * ```SendRequest```, ```OnRequestReceived```
    * ```SendResponse```, ```OnResponseReceived```
  * Application-facing API
    * ```public Response SendRequest(Request r)```

    ---

### Reference
Here's some links to other documentation in this repo.

This documentation is also a work in progress. The pages below are not intended to
be a fixed or authoritative source of truth - they capture current intent and reasoning,
and may lag behind the implementation so there are no absolute rules  - decisions
may evolve, assumptions may be revised, and if an action is sound and improves the
system, it is allowed.

> "Nothing is true, everything is permitted."
>
>  -- Altaïr Ibn‑La’Ahad

| Name | Link |
| ---- | ---- |
| Architecture | [architecture.md](wiki/architecture.md) |
| Design Principles | [design_principles.md](wiki/design_principles.md) |
| Protocol | [protocol.md](wiki/protocol.md) |
