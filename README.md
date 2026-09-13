# BasculaInterface

this is an api, that hosts a websocket that connects to a serial weight, so that the client can attach to it and react to real time weight changes



Comando para preparar la migracion antes del deploy: 
dotnet ef migrations add LogicDeletion --project Infrastructure --startup-project BasculaTerminalApi --context WeightDBContext
on dir: \repos\ees.core.telemetry>

then: 
dotnet ef database update --project Infrastructure --startup-project BasculaTerminalApi --context WeightDBContext



publish things:
BasculaInterface.csproj already carries everything needed for an unpackaged, self-contained,
single-file Windows exe permanently (WindowsPackageType=None, SelfContained=true,
PublishSingleFile=true, RuntimeIdentifier=win-x64) — there's nothing to paste in or uncomment
by hand anymore before publishing, unlike the old recipe this section used to describe.

just run:
dotnet publish BasculaInterface\BasculaInterface.csproj -f net8.0-windows10.0.19041.0 -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false
