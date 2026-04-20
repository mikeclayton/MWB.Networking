To bundle the *.cs files for upload to copilot:

```
pwsh .\extract.ps1 -sourceroot "C:\src\github\mikeclayton\MWB.Networking\src" -destination "C:\src\github\mikeclayton\MWB.Networking\scripts\bundle"
```

```
pwsh .\concat.ps1 -sourceroot "C:\src\github\mikeclayton\MWB.Networking\src" -outputfile "C:\src\github\mikeclayton\MWB.Networking\scripts\concat.txt"
```