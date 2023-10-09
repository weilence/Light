dotnet clean
Remove-Item pack -Recurse
dotnet pack -c Release -o ./pack
