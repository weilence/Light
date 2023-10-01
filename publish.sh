dotnet clean
rm -rf ./pack
dotnet pack -c Release -o ./pack
dotnet nuget push "./pack/*.nupkg" -s nuget.org -k $NugetApiKey --skip-duplicate
