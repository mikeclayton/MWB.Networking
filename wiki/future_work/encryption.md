### Encryption

Provide a mechanism for encryption at a low level - it would be intialised
as part of the peer connection and then automatically manage itself.

* Potentially as a passthrough "NetworkConnection" that sits
above a real network connection like TcpNetworkConnection and encrypts
blocks before passing them through to the lower network

### Wire format

Encryption *must* protect *all* network frame data:

Instead of the plain text version:

```
[4 bytes: length prefix] [N bytes: block payload]
```

do *not* do this:

```
[4 bytes: length prefix] [N bytes: encrypted block payload]
```

as this reveals "length prefix" on the wire, so do this instead

```
[X bytes: *encrypted* header] [Y bytes: encrypted block payload]
```

```X``` would be a fixed size so the reader could pull a
deterministic number of bytes off the wire before decrypting the header.

### Encryption Keys

Encryption would use a shared key (e.g. in Mouse Without Borders this would
be the existing "secret" from the config file to authenticate the remote
and encrypt data.

---

### Generated content


Scope & Layering

Encryption should operate below ProtocolFrame semantics, but above the raw TCP stream.
The natural boundary is between NetworkFrame and the transport I/O:

ProtocolFrame → NetworkFrame (plain, semantic)
NetworkFrame → encrypted bytes
encrypted bytes → TCP



Handshake & Session Establishment

Encryption requires an explicit handshake phase before normal frame exchange.
Responsibilities of the handshake:

Key exchange (e.g. ephemeral session keys)
Peer authentication (optional / pluggable)
Capability negotiation (encryption on/off, version)


Handshake should complete before LogicalConnection.WhenReadyAsync() is satisfied.

Key Material & State

Encryption state should be owned by the transport/session, not the protocol.
Keys are:

Per-connection
Ephemeral (regenerated on reconnect)


Rekeying can be deferred until needed (not required initially).

Framing Interaction

Encryption operates on serialized NetworkFrames, not protocol payloads.
Encryption must preserve:

Frame boundaries
Payload length integrity


Encryption metadata (IVs, tags) should not leak into protocol semantics.

Authentication & Trust

Authentication is orthogonal to encryption:

Encryption-only mode must be possible
Authentication can be layered later without reworking framing


Peer identity should never be required by the protocol layer.

Failure Semantics

Decryption failures are fatal to the connection.
Corrupt frames must not propagate to the protocol layer.
Connection teardown should trigger a clean reconnect & re-handshake.

Versioning & Compatibility

Encryption parameters (algorithms, modes) must be versioned.
Mixed-version peers should:

Negotiate a common mode, or
Fail fast during handshake


Wire protocol should remain extensible for future crypto upgrades.

Diagnostics Interaction

Diagnostics must remain non-semantic and optional.
Encryption should operate after diagnostics timestamps are set on outbound frames.
Wire-level diagnostics (if added later) must not weaken encryption guarantees.

Performance Considerations

Encryption belongs on the I/O path, not enqueue/dequeue hot paths.
Should be fully stream-based and allocation-minimal.
Instrumentation (timings, counters) should be optional and local.


One‑line architecture summary (useful to keep):

Protocol semantics are framed first, diagnostics are recorded second, encryption is applied last, and the result is written to the transport.

This encryption plan fits cleanly with:

your existing layering discipline
your diagnostics approach
your current TCP provider design

And it leaves plenty of room to evolve without re‑architecting the stack later.