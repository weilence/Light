Set-Location $PSScriptRoot
dotnet clean
dotnet pack -c Release -o ./pack
dotnet nuget push ".\pack\*.nupkg" -s nuget.org -k $env:NugetApiKey
Remove-Item pack -Recurse