## Interaction Model Overview

This document describes the three core interaction patterns supported by the protocol:

* Events (fire‑and‑forget notifications)
* Request / Response interactions (optionally returning a stream)
* Independent session‑scoped streams

Each pattern has a clearly defined lifecycle and set of invariants.

---

### Events

Events are fire‑and‑forget notifications sent between peers.

#### Characteristics

* No response is expected or implied
* No lifecycle beyond delivery
* Not associated with any request or stream
* Lightweight and low‑latency

#### Typical use cases

* Mouse movement
* Clipboard change notifications
* State updates
* Heartbeats or telemetry

#### Lifecycle

```
Sender                     Receiver
  |                           |
  |------ Event ------------->|
  |                           |
```

#### Notes

* Events may be sent at any time while the session is open
* Delivery is best‑effort; no acknowledgement is required

---

### Requests and Responses (with Optional Stream)

Requests model explicit “ask / answer” interactions, similar in spirit to HTTP.

Each request produces exactly one response, which may optionally include a single request‑scoped stream for transferring additional data.

#### Characteristics

* One request → one response
* The response may:
  * contain only metadata, or
  * contain metadata and open one request‑scoped stream
* At most one request‑scoped stream is allowed
* Responding finalises the request

#### Lifecycle (without stream)

```
Client                     Server
  |                           |
  |------ Request ----------->|
  |                           |
  |<----- Response -----------|
  |                           |
```

#### Lifecycle (with request‑scoped stream)

```
Client                     Server
  |                           |
  |------ Request ----------->|
  |<-- Response (metadata) ---|
  |     + StreamOpen          |
  |                           |
  |<===== Stream Data ========|
  |<===== Stream Data ========|
  |<===== Stream Data ========|
  |                           |
  |<----- StreamClose --------|
  |           or              |
  |------ StreamClose ------->|
  |
```

#### Rules and invariants

* The stream (if any) must be opened before the response is sent
* Once the response is sent:
  * The request is closed
  * No further request‑scoped operations are permitted
* The request‑scoped stream is closed when transmission completes

#### Typical use cases

* Requesting clipboard contents
* Querying configuration or status
* Fetching a single logical resource

#### Design intent

This model keeps request semantics simple, explicit, and easy to reason about. More complex workflows are handled by issuing multiple requests, not by complicating a single request’s lifecycle.

---

### Independent (Session‑Scoped) Streams

Independent streams are long‑lived, session‑scoped data channels that are not tied to any particular request.

#### Characteristics

* Opened directly on the session
* Not associated with a request or response
* Lifetime is independent of request lifecycles
* Multiple independent streams may exist concurrently

#### Lifecycle

```
Peer A                    Peer B
  |                           |
  |------ StreamOpen -------->|
  |                           |
  |====== Stream Data =======>|
  |====== Stream Data =======>|
  |====== Stream Data =======>|
  |                           |
  |------ StreamClose ------->|
  |           or              |
  |------ StreamClose ------->|
  |                           |
```

#### Rules and invariants

* Independent streams may be opened at any time while the session is open
* Request completion has no effect on independent streams
* Stream lifetime is explicitly controlled via open/close

#### Typical use cases

* Bulk data transfer
* Background synchronisation
* High‑throughput or long‑running channels
* Future extensibility

---

## Summary

The protocol supports three orthogonal interaction patterns:

* **Events** notify without requiring coordination
* **Requests** ask for one answer, optionally accompanied by a single stream
* **Independent** streams provide flexible, long‑lived data channels

By keeping these lifecycles distinct and explicit, the protocol remains simple, predictable, and easy to evolve, while still supporting a wide range of use cases.
