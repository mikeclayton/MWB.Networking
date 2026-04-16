### TcpReconnect Policy

Add an explicit Reconnect Backoff Policy configuration:

```
public sealed TcpReconnectBackoffPolicy
{
   ...
}
```

For example:

* double the wait time at each iteration
* max retry attempts before throwing
* etc

instead of the current hardcoded "wait 1 second"
