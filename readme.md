## MWB.Networking

---

### What is this?

**MWB.Networking** is a blue sky experiment in writing a replacement networking stack for
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
to be able to feed some incrememtal ideas and improvements into the existing code.

---

### Status

This project is in heavy flux - it's an experiment not a product, so expect things
to be changing a lot and for there to be a lot of rough edges and incomplete areas.

The main goal is to explore ideas and learn, not to build a polished library, so don't
take a dependency in stable systems (yet!).

---

### Design Notes

Here's some links to other documentation in this repo.

This documentation is also a work in progress. The pages below are not intended to
be a fixed or authoritative source of truth - they capture current intent and reasoning,
and may lag behind the implementation. There are no absolute rules here - decisions
may evolve, assumptions may be revised, and if an action is sound and improves the
system, it is allowed.

In short, nothing is true, everything is permitted.

| Name | Link |
| ---- | ---- |
| Architecture | [architecture.md](wiki/architecture.md) |
| Design Principles | [design_principles.md](wiki/design_principles.md) |
| Protocol | [protocol.md](wiki/protocol.md) |
