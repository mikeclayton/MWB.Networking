# Expanded Input State Machine Diagrams
## REL vs ABS Aggregation and Normalisation

This section expands the earlier high‑level diagrams to show how **relative** and **absolute** mouse movement events are handled inside the input state machine, how they are aggregated, and how mixed representations are normalised.

---

## Terminology

- **REL(dx, dy)**  
  Relative mouse movement. Movement is expressed as a delta from the *current pointer position*.

- **ABS(x, y)**  
  Absolute mouse movement. Movement directly specifies the *target pointer position*.

- **Virtual Pointer State**  
  Internal state held by the input state machine that represents the current pointer position and mode.

---

## High‑Level Context (unchanged)

```
Input Producer
    ↓
ConcurrentQueue<RawInputEvent>
    ↓
Input State Machine
    ↓
ConcurrentQueue<CanonicalInputEvent>
    ↓
Pump → ProtocolSession
```

The diagrams below **zoom into the Input State Machine**.

---

## Case 1: Aggregating Pure REL Events

### Input (raw events from producer)

```
REL(dx=5, dy=1)
REL(dx=5, dy=1)
REL(dx=0, dy=3)
```

### State Machine View

```
┌───────────────────────────────────┐
│ Virtual Pointer State             │
│                                   │
│ Mode: RELATIVE                    │
│ Accumulated Δx = 0                │
│ Accumulated Δy = 0                │
└───────────────────────────────────┘
```

### Step‑by‑step accumulation

```
Incoming: REL(5, 1)
→ Accumulate → Δx=5,  Δy=1

Incoming: REL(5, 1)
→ Accumulate → Δx=10, Δy=2

Incoming: REL(0, 3)
→ Accumulate → Δx=10, Δy=5
```

### Flush (semantic boundary or timeout)

```
Emit:
REL(dx=10, dy=5)
```

### Output (canonical events)

```
REL(dx=10, dy=5)
```

✅ No semantic loss  
✅ Fewer messages  
✅ Same net effect  

---

## Case 2: Pure ABS Events (Last‑Write‑Wins)

### Input

```
ABS(x=400, y=300)
ABS(x=405, y=320)
```

### State Machine Logic

- Absolute events overwrite pointer position
- Earlier ABS movements become irrelevant

### Internal State

```
ABS(400, 300) → overwritten by → ABS(405, 320)
```

### Flush

```
Emit:
ABS(x=405, y=320)
```

✅ Correct and minimal  
✅ No need to emit intermediate positions  

---

## Case 3: Mixed REL → ABS → REL (Normalisation Example)

---

### Initial pointer state

```
Virtual pointer position starts at:
(x=100, y=100)
```

---

### Input sequence (raw)

```
REL(dx=+5, dy=+0)
REL(dx=+5, dy=+0)
ABS(x=200, y=300)
REL(dx=+3, dy=-2)
```

---

### Step‑by‑step state evolution

```
Initial:
Pointer = (100, 100)
Mode    = REL
```

#### 1️⃣ REL(dx=+5, dy=0)

```
Pointer becomes (105, 100)
Accumulated REL = (+5, 0)
```

#### 2️⃣ REL(dx=+5, dy=0)

```
Pointer becomes (110, 100)
Accumulated REL = (+10, 0)
```

*(At this point REL aggregation exists, but is not yet emitted.)*

#### 3️⃣ ABS(x=200, y=300)

Semantic boundary reached.

- Previous REL deltas are **discarded**
- Absolute movement defines new ground truth

```
Pointer = (200, 300)
Mode    = ABS
```

#### 4️⃣ REL(dx=+3, dy=-2)

Relative adjustment applied *on top of* absolute position:

```
Pointer becomes (203, 298)
```

---

### Final flush decision

Instead of emitting:

```
REL(5,0)
REL(5,0)
ABS(200,300)
REL(3,-2)
```

…the state machine emits:

```
ABS(x=203, y=298)
```

---

### Output (canonical events)

```
ABS(x=203, y=298)
```

✅ Fully equivalent effect  
✅ No redundant events  
✅ Order preserved  
✅ Perfect for transport  

---

## Mixed Example with Keyboard Boundary

Keyboard events always force a flush.

### Input

```
REL(dx=5, dy=0)
REL(dx=5, dy=0)
KeyDown(CTRL)
REL(dx=2, dy=1)
```

### Output

```
REL(dx=10, dy=0)
KeyDown(CTRL)
REL(dx=2, dy=1)
```

Keyboard events **must not be crossed**, even if further aggregation would be possible.

---

## Why This Works

### Key properties

- Aggregation **never crosses semantic boundaries**
- Absolute coordinates redefine ground truth
- Relative movements can be folded into absolute ones
- The state machine emits the **minimal representation**
- Transport sees a clean, intention‑level stream

---

## Key Mental Model

```
REL events = adjustments
ABS events = resets of reference frame
```

The state machine owns the reference frame, not the producer and not the transport.

---

## Summary Table

| Sequence Type | Output |
|--------------|--------|
| REL + REL | single REL |
| ABS + ABS | last ABS |
| REL → ABS | ABS only |
| ABS → REL | ABS (rel adjusted) |
| REL → ABS → REL | ABS (rel adjusted) |
| Any → KeyEvent | flush before key |

---

## Final Takeaway

By **explicitly modelling REL and ABS inside the input state machine**, you:

- Preserve user intent
- Reduce message volume
- Keep semantics deterministic
- Make RPC and transport layers simpler
- Avoid hidden coupling between input and delivery

This is exactly the kind of *semantic compression* that the forwarder/state‑machine approach is designed to enable.