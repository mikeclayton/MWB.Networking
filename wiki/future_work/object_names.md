Add a "Name" property to objects - e.g.

ProtocolSession.Name

to assist with debugging when multiple objects exist in the same context - for example

* Mouse + Keyboard session
* Clipboard session

If the application crashes with an exception like:

```Protocol Violation in session named 'Mouse + Keyboard```

it would be a llot clearer than the current exception like:

```A session encountered a Protocol Violation``

Add names to relevant objects - ProtocolSession, TcpNetworkConnection, NetworkProvider etc.

Even if they're random guids by default it will allow correlation in log files.

The session could also potentially transmit its guid as part of the handshake to allow correlation across peers.

Note if the session has an app-supplied name like "Mouse + Keyboard" *that* should stay local and
transmit a guid instead during handshake to prevent leaking app configuration in the handshake - the app shouldn't be able
to use the name of the session as a key for usage - that should be something the application's **own
communication protocol** finds out (if it needs it)

Basically "Session identity for diagnostics should not be used as application identity for functionality."
