
# System vs Application Error Model

This document describes the **error-handling model** used in the MWB networking stack. It is intended as an **implementation guide** and a long-term reference.

The core goal is to **separate protocol/system failures from application-level failures**, ensuring:
- clean abstraction boundaries
- reliable request lifecycles
- correct logging and diagnostics
- future compatibility with RPC-style APIs

---

## 1. Mental Model

There are **two fundamentally different kinds of failure**:

| Category | Who Detects It | Who Owns It | Who Sees It |
|--------|---------------|------------|------------|
| **System / Protocol Error** | Networking library | Protocol/runtime | Logs only (not app) |
| **Application Error** | Application code | Application | Remote peer (via response) |

> **Applications declare outcomes. Protocols diagnose failures.**

---

## 2. System / Protocol Errors

### Definition

A **system error** is any failure that occurs because:
- the protocol invariants were violated
- the runtime entered an unexpected state
- the implementation failed to execute correctly

These errors are **never blamed on the application** and are **never part of normal request semantics**.

### Examples

- Unknown or invalid frame kind
- Invalid frame sequence
- Duplicate request or stream IDs
- Missing required structural fields
- OutOfMemoryException
- NullReferenceException
- Unexpected runtime or library failures

### Properties

System / protocol errors:
- are detected by the library
- are logged centrally with full diagnostics
- may terminate the request or entire session
- are not surfaced to the application as structured errors
- are not sent to the peer with semantic meaning

### Logging Rule

✅ **Only system errors are logged by the protocol layer.**

Application errors are expected outcomes and must not pollute system logs.

---

## 3. Application Errors

### Definition

An **application error** occurs when:
- the request is syntactically valid
- the protocol is healthy
- but the application intentionally rejects the request

### Examples

- Invalid request payload
- Unsupported operation
- Permission denied
- Domain/business rule violation

### Properties

Application errors:
- are deliberate decisions
- are request-scoped
- do not imply protocol failure
- do not require logging by the protocol
- are communicated back to the peer

---

## 4. Protocol Wire Model

At the protocol level, request handling simplifies to:

```
Request
 ├─ Response(payload)
 └─ ErrorResponse(payload)
```

There is **no semantic protocol error taxonomy on the wire**.

---

## 5. Request Failure Classification

Requests expose **exactly two failure outcomes**:

```csharp
public enum RequestFailureKind
{
    InternalError,     // System / library failure
    ApplicationError   // Application-declared rejection
}
```

### Meaning

| Kind | Interpretation |
|-----|----------------|
| InternalError | The system failed to process the request correctly |
| ApplicationError | The application rejected the request for semantic reasons |

---

## 6. Application Error Payloads

All application error semantics live **inside the payload**, not the protocol.

Example (conceptual):

```json
{
  "code": "InvalidPayload",
  "message": "Key must be a printable UTF-8 character",
  "details": {
    "received": "\u0007"
  }
}
```

Payload format and schema are:
- application-defined
- versioned by the application
- opaque to the protocol layer

---

## 7. IncomingRequest API Rules

### Application-facing API

Applications may:
- Respond successfully
- Fail with **ApplicationError** only

They may **not**:
- Raise protocol errors
- Signal internal/system failure
- Log infrastructure failures

Example:

```csharp
public void OnRequestReceived(IncomingRequest request, ReadOnlyMemory<byte> payload)
{
    try
    {
        var result = Handle(payload);
        request.Respond(result);
    }
    catch (ValidationException ex)
    {
        request.Fail(
            RequestFailureKind.ApplicationError,
            BuildApplicationErrorPayload(ex));
    }
}
```

---

## 8. System Error Handling

The protocol/runtime layer is solely responsible for:
- catching unexpected exceptions
- logging full diagnostics
- failing requests with InternalError
- terminating sessions when safety requires

Example (library-internal):

```csharp
try
{
    DispatchRequest(...);
}
catch (Exception ex)
{
    LogCritical(ex);
    FailRequestInternally(RequestFailureKind.InternalError);
}
```

---

## 9. Key Rules (Summary)

- ✅ Only the library raises **system errors**
- ✅ Only the library logs **system errors**
- ✅ The application only reports **application errors**
- ✅ Request failures are always closed exactly once
- ✅ Protocol diagnostics never leak into app semantics
- ✅ Payloads define all application error meaning

---

## 10. Design Outcome

This model ensures:

- Strong abstraction boundaries
- Stable wire protocol
- Clean logging and diagnostics
- Predictable request lifecycles
- Natural evolution toward RPC frameworks

---

**Authoritative principle**:

> *If a failure could happen even when the peer behaved perfectly, it is a system error and must remain internal.*

