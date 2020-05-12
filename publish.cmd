set NUGET_ORG=https://api.nuget.org/v3/index.json
dotnet test
dotnet pack Amg.Plantuml
dotnet nuget push nupkg\Amg.Plantuml-0.1.6.nupkg --source %NUGET_ORG%
pushd edit-plantuml
dotnet pack 
popd
dotnet nuget push nupkg\edit-plantuml.0.1.0.nupkg --source %NUGET_ORG%
