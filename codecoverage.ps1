dotnet tool restore

dotnet test -c Debug --filter FullyQualifiedName!~integration /p:CollectCoverage=true /p:CoverletOutput=./TestResults/coverage.info /p:CoverletOutputFormat=cobertura --logger "trx;LogFileName=test_results.trx" 

dotnet reportgenerator -reports:./tests/**/coverage.info -targetdir:./reports/codecoverage -reporttypes:Html,Cobertura