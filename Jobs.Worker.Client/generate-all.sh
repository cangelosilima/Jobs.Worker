#!/bin/bash

# Jobs.Worker Client SDK - Generate All Clients
# This script generates all client SDKs from the running API

set -e

echo "======================================"
echo "Jobs.Worker Client SDK Generator"
echo "======================================"
echo ""

# Check if API is running
echo "Checking if API is running at https://localhost:5001..."
if ! curl -k -s -f -o /dev/null https://localhost:5001/health; then
    echo "ERROR: API is not running at https://localhost:5001"
    echo "Please start the Jobs.Worker.Api first"
    exit 1
fi
echo "✓ API is running"
echo ""

# Generate .NET 8 REST Client
echo "Generating .NET 8 REST Client..."
nswag run nswag-dotnet.json
echo "✓ .NET 8 REST client generated"
echo ""

# Generate .NET Framework 4.8 REST Client
echo "Generating .NET Framework 4.8 REST Client..."
nswag run nswag-net48.json
echo "✓ .NET Framework 4.8 REST client generated"
echo ""

# Generate TypeScript REST Client
echo "Generating TypeScript REST Client..."
nswag run nswag-typescript.json
echo "✓ TypeScript REST client generated"
echo ""

echo "======================================"
echo "All clients generated successfully!"
echo "======================================"
echo ""
echo "Next steps:"
echo "  1. Build .NET 8:           dotnet build -c Release"
echo "  2. Build .NET Framework:   cd Jobs.Worker.Client.Net48 && dotnet build -c Release"
echo "  3. Build TypeScript:       npm install && npm run build"
echo ""
