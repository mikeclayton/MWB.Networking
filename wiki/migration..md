## Hypothetical Migration Plan

### Goals

* Introduce the new networking stack through deliberately small, safe, and reversible changes that keep risk bounded and recovery straightforward at every stage.
* Provide explicit, human‑configurable opt‑in and opt‑out mechanisms to allow immediate local recovery from critical issues without requiring redeployments or external intervention.
* Establish a clear long‑term path to completely replacing the existing Mouse Without Borders networking stack with this library once it has demonstrated sustained stability, reliability, and real‑world operational readiness.

### Phase 0 - Deploy new stack codebase disabled

* Do nothing except ship the new stack codebase in the Mouse Without Borders product
* No feature flags, no invocation of new code paths, new stack code is an inert passenger

#### Benefits

* New stack ships in the binary
* New ports exist but are never opened
* Old behavior is provably identical

#### This phase is about:

* CI validation
* Security review
* Defender / firewall / AV behavior
* Deployment confidence

### Phase 1 - Mouse Jump integration (opt-in)

* Add new feature switch to Mouse Without Borders - "Enable Mouse Jump integration"
* This spins up the new stack *in parallel* on a different port for Mouse Jump integration
  * Screen toplology request, response
  * Screenshot thumnail request, response

#### At this point:

* New stack is used only for Mouse Jump capabilities
* Only if the user explicitly opts in
* Legacy MWB remains authoritative for core behavior
* Start capturing telemetry
  * port connectivity
  * connection events
  * failure events
  * frames per second
  * frame transit times (via in-code diagnostics)
  * etc?

#### This is ideal because Mouse Jump:

* Is already an integration feature
* Is additive, not foundational
* Has clear, visible outputs (topology, screenshots)

## Phase 2 — Mouse Jump Integration - on by default

> On for everyone, just for Mouse Jump

### This is the point where:

* The new stack has proven itself
* The feature is no longer experimental
* Most users are exercising the code path without knowing it

### Now the new stack has:

* Scale
* Diversity of environments
* Long-lived sessions
* Real support exposure

### Phase 3 — Incremental migration to new stack, message by message

> Enable new stack migration and collect telemetry

This is a separate setting—and that separation is very important.

This switch means:

* Attempt new stack for migrated capabilities (mouse, keyboard, etc)
* Negotiate per-message routing
* Collect telemetry
* Allow users to opt out if problems appear

### This gives you:

* Early adopters
* Field validation
* Per-message migration confidence

And because of your capability routing model:

* Users can be “partially upgraded” safely
* Failures are localized and diagnosable

### Phase 4 — Default new stack, legacy fallback

* New stack preferred
* Legacy still available
* Capability-based fallback remains explicit

Only after this phase do you even think about removal.

### Phase 5 — Disable old stack

Disable (not remove) old stack so it can still be turned back on quickly in a patch release if there's a P1 regression

* Still have a reliable escape hatch that is only a patch release away

### Phase 6 — Remove old stack code

Remove all old-stack code from the codebase
