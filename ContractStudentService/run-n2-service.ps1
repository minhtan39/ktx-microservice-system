$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:Integration__GatewayBaseUrl = "http://172.16.19.206:8080"

$logFile = Join-Path $PSScriptRoot (
    "n2-service-{0}.log" -f (Get-Date -Format "yyyyMMdd-HHmmss"))

dotnet run --no-launch-profile --urls http://0.0.0.0:5052 *>&1 |
    Tee-Object -FilePath $logFile
