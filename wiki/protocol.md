MWB.Network uses a custom network protocol layered on top of a raw byte stream transport.

### Network Blocks

Layer 0 is expected to be able to encode and decode discrete blocks of bytes
and be able to recover the exact same length of bytes at the remote end - the
exact mechanism is left to individual INetworkConnection implementations, but
an obvious choice is to use a length-prefixed framing protocol where each
message is sent with a length prefix (e.g. 4 bytes) followed by the message payload:

| Offset | Length | Description                     |
|--------| ------ |---------------------------------|
| 0-3    | 4      | Length of the message payload   |
| 5      | N      | Message payload (N bytes)       |

[Length (4 bytes)][Message Payload (Length bytes)]]
```

### Network Frames

| Offset | Length | Name | Description                     |
|--------| ------ |------| ---------------------------|
| 0-3    | 4      | Kind | of the message (e.g. Event, Request, Response, Stream) |
| 4-7    | 4      | Event type | (only for Event frames) |
| 8-11   | 4      | Request ID | (only for Request and Response frames) |
| 12-15   | 4      | Stream ID | (only for Stream frames) |
| 16-19   | 4      | Chunk index | (only for Stream frames) |
| 20     | 1      | Is final chunk | (only for Stream frames) |
| 21-     | N      | Payload | (remaining bytes)       |


The protocol defines the structure and semantics of messages exchanged between clients and servers,
including framing, message types, session management, and error handling. It ensures reliable
communication and supports features like request-response patterns, event broadcasting, and streaming data.
