### Compression

The application may currently compress its data at the protocol level, but it
would also be possible to introduce compression as a low-level transport layer.

This would mirror the encryption layer: a wrapping network connection that
operates on serialized `NetworkFrame`s, compressing frame payloads before
forwarding them to a subordinate network connection for transmission, and
decompressing them on receipt.

Compression at this layer would be non-semantic, preserve frame boundaries,
and remain transparent to the protocol layer.

### Example Pipeline

```
[ Upstream frame producer ]
        |
        |  NetworkFrame
        |  (serialized bytes)
        v
+--------------------------+
|  Compression connection  |
|  - compress incoming frame bytes |
|  - reframe compressed data |
+--------------------------+
        |
        |  NetworkFrame
        |  (compressed bytes)
        v
+--------------------------+
|   Next network layer     |
|   (e.g. encryption or    |
|    TCP connection)       |
+--------------------------+
```


#### Example Pipeline

```
[ Upstream frame producer ]
        |
        v
+--------------------------+
|  Compression connection  |
+--------------------------+
        |
        v
+--------------------------+
|   Encryption connection  |
+--------------------------+
        |
        v
+--------------------------+
|      TCP connection      |
+--------------------------+
```