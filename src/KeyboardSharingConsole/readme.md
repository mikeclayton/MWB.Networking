```
start cmd /k "KeyboardSharingConsole.exe --local-peer-name PeerA --remote-peer-name PeerB --listen-port 9001 --connect-port 9000" & start cmd /k "KeyboardSharingConsole.exe --local-peer-name PeerB --remote-peer-name PeerA --listen-port 9000 --connect-port 9001"
```
