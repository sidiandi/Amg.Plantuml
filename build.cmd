set NUGET_ORG=https://api.nuget.org/v3/index.json
dotnet test
del out\*.nupkg
dotnet pack -c Release -o out
dotnet nuget push --source %NUGET_ORG% out\*.nupkg
