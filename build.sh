dotnet publish -r linux-x64 -c Release -o ../Releases/linux /p:SelfContained=true /p:PublishSingleFile=true /p:PublishReadyToRun=true
dotnet publish -r osx-x64 -c Release -o ../Releases/osx /p:SelfContained=true /p:PublishSingleFile=true /p:PublishReadyToRun=true
dotnet publish -r win-x64 -c Release -o ../Releases/win /p:SelfContained=true /p:PublishSingleFile=true /p:PublishReadyToRun=true