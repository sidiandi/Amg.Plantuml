set NUGET_ORG=https://api.nuget.org/v3/index.json
dotnet test
dotnet pack
dotnet nuget push --source %NUGET_ORG% Amg.Plantuml\bin\Debug\Amg.Plantuml.0.1.7.nupkg
dotnet nuget push --source %NUGET_ORG% edit-plantuml\bin\Debug\edit-plantuml.0.1.0.nupkg