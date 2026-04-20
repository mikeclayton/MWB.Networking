

### Review the TCP connection along five axes

When reviewing TcpNetworkConnection, I recommend doing it in layers, not line‑by‑line. The mistake people make is reviewing TCP code “locally”; you want to review it systemically.

---

1. Lifecycle invariants (most important)

Before touching code, write down—explicitly—the invariants that must always hold:

* A TcpNetworkConnection is either:
  * connected
  * connecting
  * shutting down
  * closed
* There is exactly one active socket at a time
* After disposal:
  * no reads succeed
  * no writes succeed
  * pending operations are unblocked deterministically
* EOF semantics are consistent:
  * ReadAsync returns 0 only for true remote closure
* Exceptions are classified, not leaked randomly

If any of these invariants are not clearly enforced in code, that’s a review issue—even if the code “works”.
👉 I strongly recommend writing these invariants in comments (private, not XML docs) at the top of the file before reviewing. It sharpens everything else.

---

### 2. Cancellation semantics (easy to get subtly wrong)

Go through every async call and ask:

Is cancellation:

* prompt?
* cooperative?
* distinguishable from remote close?

Key rules to enforce:

* CancellationToken never causes the connection to become corrupted
* Cancelling one read/write does not poison the socket
* Cancellation ≠ Dispose ≠ EOF

For TCP, a classic mistake is:

> using cancellation to close the socket underneath a pending operation

Your ILogicalConnection waits for readiness first, which helps, but inside TcpNetworkConnection you still need clear boundaries.

**Red flags to look for:**

* OperationCanceledException being caught and ignored without context
* Cancellation triggering Socket.Close() implicitly
* Cancellation completing a read with 0

---

### 3. Error classification & propagation

TCP errors are noisy and inconsistent across platforms. Your job is to collapse entropy.

During review, identify:

* Which exceptions escape ReadAsync / WriteAsync
* Which are translated (and how)
* Which are swallowed or logged

Ideal outcome:

* Transient I/O failures → surface as recoverable errors to the provider
* Protocol‑breaking failures → close the connection deterministically
* Programmer errors → fail fast

Ask yourself:

> “If this exception shows up in a log at 3am, will I know what layer it came from?”

If the answer is no, add classification or logging.

---

### 4. Concurrency and reentrancy

TCP code tends to “mostly work” until concurrency stress hits.
Explicitly answer these questions during review:

* Are concurrent reads allowed?
* Are concurrent writes allowed?
* If so, are they serialized or parallel?
* What happens if:
  * write while disposing?
  * read while reconnecting?
  * attach swap happens mid‑I/O?

Given your abstractions, a perfectly valid rule is:

> One reader, one writer at a time; anything else is undefined.

What matters is that the rule is:

* enforced
* documented (even if only in code comments)
* tested

If the code implicitly relies on this without enforcing it, add guards.

---

5. Shutdown & replacement behavior

This is where TCP implementations most often leak resources.

Review the exact sequence for:

* Remote FIN
* Remote RST
* Local dispose
* Provider‑initiated replacement via Attach

For each path, verify:

* Socket is closed exactly once
* Pending tasks are awakened exactly once
* No background loops keep running
* No unobserved exceptions

Your LogicalConnectionControl.Attach disposes the old connection — good.

Now ensure TcpNetworkConnection.Dispose is idempotent and aggressive.

A good mental model is:

> Dispose should be a one‑way trapdoor.

---

### What “bulletproof” means here (important)

Bulletproof does not mean:

* “Never throws”
* “Automatically reconnects everywhere”
* “Hides all failures”

Bulletproof means:

* Fails predictably
* Fails locally
* Fails diagnosably
* Never poisons shared state
* Never wedges the application

Your architecture already supports this. The TCP class just needs to live up to it.

---

### Tests to add specifically for TCP (later)

Once the review is done, the TCP‑only tests that matter most are:

* Cancellation during read
* Cancellation during write
* Dispose during read/write
* Remote close during read
* Socket error injection (RST, aborted connection)
* Replace‑while‑active via Attach

You don’t need many — just the nasty ones.