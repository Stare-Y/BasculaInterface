# BasculaInterface

this is an api, that hosts a websocket that connects to a serial weight, so that the client can attach to it and react to real time weight changes



Comando para preparar la migracion antes del deploy: 
dotnet ef migrations add LogicDeletion --project Infrastructure --startup-project BasculaTerminalApi --context WeightDBContext
on dir: \repos\ees.core.telemetry>

then: 
dotnet ef database update --project Infrastructure --startup-project BasculaTerminalApi --context WeightDBContext



publish things:
first, in the csproj file:
<PropertyGroup Condition="'$(TargetFramework)' == 'net8.0-windows10.0.19041.0'">
	<OutputType>WinExe</OutputType>
	<RuntimeIdentifier>win-x64</RuntimeIdentifier>

	<!-- Publish as single file and self-contained -->
	<SelfContained>true</SelfContained>
	<PublishSingleFile>true</PublishSingleFile>
	<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
	
	<!-- avoid MSIX -->
	<WindowsPackageType>None</WindowsPackageType>
	
	<!-- Name and title-->
	<AssemblyName>BasculaInterface</AssemblyName>
	<ApplicationTitle>BasculaInterface</ApplicationTitle>

	<!-- Icon -->
	<!--<ApplicationIcon>Resources\AppIcon\appicon.ico</ApplicationIcon>-->
</PropertyGroup>

and the command is:
dotnet publish BasculaInterface\BasculaInterface.csproj -f net8.0-windows10.0.19041.0 -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false

gg
