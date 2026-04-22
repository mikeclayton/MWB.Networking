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

* ✅ **One‑way console app sample (using Events)**
  * Build a console application that transmits individual keystrokes
    **as events** and echoes them at a remote peer
  * Exercises the session builder interface
  * Exercises the session application API
  * Exercises the Event lifecycle model (SendEvent, EventReceived)
  * Exercises TCP network connections

* ✅ **Two‑way console app sample (using Events)** with Pipes
  * Proves bi‑directional communication
  * Exercises the request‑response model
  * Tests Pipes provider and connections
  * Proves Event send and receive works between processes
  * **Defers** testing connection arbitration

* ✅ **Two‑way console app sample (using Request-Response)** with Pipes
  * Proves Request and Response frames and state works between processes

* **Two‑way console app sample (using long-lived Streams)** with Pipes
  * Proves long-lived Stream are stable and state works between processes

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