### Diagnostics

Enahnce current ProtocolFrame diagnostic to be wire-compatible:

```
NetworkFrame {
    Header {
        Flags {
            HasDiagnostics = true/false
        }
    }
    Diagnostics?        // Only serialized if HasDiagnostics = true
    Payload
}
```

Diagnostics will need to be versioned for compatibility between 
different local vs remote build versions, and ProtocolFrame
diagnostics will need to be passed down into the NetworkFrame
via:

```
FrameConverter.ToNetworkFrame(...)
FrameConverter.ToProtocolFrame(...)
```

This would allow measuring transit time across the network by correlating "send" timestamps in the sender's diagnostic payload with the local receive time (assuming time is closely synchronised on both machines).