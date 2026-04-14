## Design Principles

Some simple principles that drive design decisions:

* **Simplicity** beats generalisation
* **Reliability** beats performance
* **Compatibility** beats novelty
* **Maintainability** beats cleverness
* **Consistency** beats convention

*but*, they're not set in stone - where there's a compelling reason to
override them, do so *responsibly*.

> "They're more like guidelines, really."
>
> -- Captain Barbossa

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

### Compatibility beats novelty

* Explains the choice of TCP with custom crypto over newer protocols
* Explains a cautious stance on QUIC and similar technologies
* Defends conservative choices where interoperability and predictability matter
* Reflects awareness of real, heterogeneous production environments
* "Beats" does not mean "rejects" — optional or experimental paths remain open

---

### Maintainability beats cleverness

* clearer semantics
* fewer implicit rules
* explicit lifecycles
* avoiding "one mechanism does everything"

---

### Consistency beats convention

* one way to do a thing across the codebase
* predictable structure over optimised variations
* fewer patterns to learn
* optimised for long‑term readers, not first impressions
