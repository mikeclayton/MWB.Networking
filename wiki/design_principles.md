## Design Principles

Some simple principles that guide design decisions:

* **Simplicity** beats generalisation
* **Reliability** beats performance
* **Enterprise deployability** beats bleeding edge
* **Maintainability** beats cleverness

---

### Simplicity beats generalisation

* reject abstract, over‑general machinery rather than useful features.

---

### Reliability beats performance

* Predictability > benchmarks
* No surprises on bad networks
* Latency jitter is worse than throughput loss
* not chasing QUIC or exotic transports

---

### Enterprise deployability beats bleeding edge

* explains TCP + custom crypto
* explains cautious stance on QUIC
* defends conservative choices
* signals awareness of real production environments
* "beats" vs "rejects" - leaves space for optional or experimental paths later.

---

### Maintainability beats cleverness

* clearer semantics
* fewer implicit rules
* explicit lifecycles
* avoiding "one mechanism does everything"
