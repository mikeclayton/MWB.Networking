Layer 2 - Protocol *doesn't* perform automatic cleanup of streams or request / responses.

It's possible that:

* an application opens a stream, sends the data and then "forgets" to close the stream
  - the stream id will remain valid for the lifetime of the session, or until *something*
  calls "Close Stream".

* similarly, an application could receive a request and if the sender doesn't disconnect the
  receive could potentially choose to (or fail to) ever send a Response, which means the requestId
  would remain valid until the session ends or something closes the request.

This is *by design* - the Layer 2 Protocol doesn't implement timeouts. It's a state machine, not
a policy machine, so it doesn't ever decide unilaterally to close a stream - it's only ever as a
result of an internal trigger (lost connection) or lifecycle event (e.g. request gets a response)

Timeouts and other *policy* mechanics need to be implemted by the application or by a higher
orchestration layer that can implement *policy*.