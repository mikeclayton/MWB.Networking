

### TCP Connect Loop

#### Current behaviour

In the outbound connect loop, the logic does this:

* Loop forever until cancelled
* If there is already an active connection:
  * await Task.Delay(TimeSpan.FromSeconds(1))
  * continue

Otherwise:

* attempt to connect
* on failure, delay before retrying

This creates two polling behaviors:

* Reconnect backoff (when no connection exists)
* “Do nothing” wait (when a connection does exist)

Both currently use a time‑based delay.

#### What it's not ideal at

* Wakes up needlessly every second
* Adds up to 1s of latency before a reconnect attempt
* Slightly noisy in logs / scheduling
* Conceptually “polling” rather than reactive

#### Proposed behaviour

Conceptually:

* When a connection becomes active → signal “connected”
* When a connection is lost → signal “disconnected”
* The connect loop waits on:
  * cancellation, or
  * “need to reconnect” signal

Instead of:
* ```await Task.Delay(1 second);```

we'd  have something like:

* ```await reconnectSignal.WaitAsync(ct);```

#### What would generate the signal

* Typical signal points would be:

* AttachCandidate succeeds → stop dialing (signal connected)
* Active connection is disposed → signal disconnected
* Provider is disposed → cancel everything
