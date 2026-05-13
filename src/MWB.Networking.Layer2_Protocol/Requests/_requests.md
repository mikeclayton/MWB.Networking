```
                    ┌────────────────────────────┐
                    │ RequestContext             │
                    │  - requestId               │
                    │  - requestType             │
                    │  - direction               │
                    │  - state machine           │
                    │  - response tracking       │
                    └─────────────▲──────────────┘
                                  │
                    ┌─────────────┴──────────────┐
                    │ RequestActions             │
                    │  - Respond(...)            │
                    │  - OpenRequestStream(...)  │
                    │  - protocol enforcement    │
                    └─────────────▲──────────────┘
                                  │
                    ┌─────────────┴──────────────┐
                    │ RequestManager             │
                    │  - inbound consumption     │
                    │  - outbound creation       │
                    │  - request lookup/store    │
                    │  - lifecycle coordination  │
                    └───────▲───────────┬────────┘
                            │           │
             inbound frames │           │ outbound intent
                            │           ▼
        ┌───────────────────┼───────────────────┐
        │                   │                   │
┌────────────────┐  ┌─────────────────┐  ┌────────────────┐
│ IncomingRequest│  │ ProtocolSession │  │ OutgoingRequest│
│ (API surface)  │  │ (transport)     │  │ (API surface)  │
└────────▲───────┘  └─────────────────┘  └────────▲───────┘
         │                                        │
         │ exposed to application                 │ returned to caller
         ▼                                        ▼
┌────────────────────────────────────────────────────────────┐
│ Application code                                           │
│  - reads Payload                                           │
│  - IncomingRequest.Respond(...)                            │
│  - OutgoingRequest.Response (await)                        │
│  - Request.OpenRequestStream(...)                          │
└────────────────────────────────────────────────────────────┘
```
