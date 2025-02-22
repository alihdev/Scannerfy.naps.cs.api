# Scannerfy.naps.cs.api
# using dotnet - [naps sdk](https://github.com/cyanfish/naps2/tree/master/NAPS2.Sdk)


# Build Exe Instructions
- create scannerfy-publish folder
- to publish the project with .net sdk, excute this cmd inside scannerfy api  
```
dotnet publish -c Release -r win-x64 --self-contained true -o "path to scannerfy-publish"
```

- install inno setup [From here](https://jrsoftware.org/isdl.php)
- select open existing script (create a new script then load it || load setup/script.iss)
- add the scannerfy-publish path inside the script under [Files] change path only leave * 
- click build then compile
- click on build then open output folder to see the exe file

- Scannerfy.Api.exe file in publish folder