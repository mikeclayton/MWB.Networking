```
                 ┌─────────────────────┐
                 │ StreamContext       │
                 │  - state            │
                 │  - owning request   │
                 │  - invariant checks │
                 └─────────▲───────────┘
                           │
                ┌──────────┴────────────┐
                │ StreamManager         │
                │  - lookup by streamId │
                │  - lifecycle routing  │
                └──────────▲────────────┘
                           │
      ┌────────────────────┼────────────────────┐
      │                    │                    │
┌────────────────┐ ┌────────────────┐ ┌──────────────┐
│ IncomingStream │ │ OutgoingStream │ │ (future)     │
│  observer-only │ │  command-only  │ │ DuplexStream │
└────────────────┘ └────────────────┘ └──────────────┘
```