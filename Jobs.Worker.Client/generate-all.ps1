# Jobs.Worker Client SDK - Generate All Clients (PowerShell)
# This script generates all client SDKs from the running API

$ErrorActionPreference = "Stop"

Write-Host "======================================"
Write-Host "Jobs.Worker Client SDK Generator"
Write-Host "======================================"
Write-Host ""

# Check if API is running
Write-Host "Checking if API is running at https://localhost:5001..."
try {
    $response = Invoke-WebRequest -Uri "https://localhost:5001/health" -UseBasicParsing -SkipCertificateCheck
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ API is running" -ForegroundColor Green
    }
}
catch {
    Write-Host "ERROR: API is not running at https://localhost:5001" -ForegroundColor Red
    Write-Host "Please start the Jobs.Worker.Api first"
    exit 1
}
Write-Host ""

# Generate .NET 8 REST Client
Write-Host "Generating .NET 8 REST Client..."
nswag run nswag-dotnet.json
Write-Host "✓ .NET 8 REST client generated" -ForegroundColor Green
Write-Host ""

# Generate .NET Framework 4.8 REST Client
Write-Host "Generating .NET Framework 4.8 REST Client..."
nswag run nswag-net48.json
Write-Host "✓ .NET Framework 4.8 REST client generated" -ForegroundColor Green
Write-Host ""

# Generate TypeScript REST Client
Write-Host "Generating TypeScript REST Client..."
nswag run nswag-typescript.json
Write-Host "✓ TypeScript REST client generated" -ForegroundColor Green
Write-Host ""

Write-Host "======================================"
Write-Host "All clients generated successfully!" -ForegroundColor Green
Write-Host "======================================"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Build .NET 8:           dotnet build -c Release"
Write-Host "  2. Build .NET Framework:   cd Jobs.Worker.Client.Net48; dotnet build -c Release"
Write-Host "  3. Build TypeScript:       npm install; npm run build"
Write-Host ""
