dotnet tool restore

dotnet nukit -f --glob ./reports/** --glob ./package/**

dotnet fantomas ./

dotnet build

dotnet test