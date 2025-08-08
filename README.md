# BasculaInterface

this is an api, that hosts a websocket that connects to a serial weight, so that the client can attach to it and react to real time weight changes



Comando para preparar la migracion antes del deploy: dotnet ef migrations add LogBookRefactorMigration --project Infrastructure --startup-project telemetry

on dir: \repos\ees.core.telemetry>

then: dotnet ef database update --project Infrastructure --startup-project telemetry
