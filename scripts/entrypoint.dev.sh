#!/usr/bin/env bash
set -euo pipefail

echo "[rivage] Restoring packages…"
dotnet restore Rivage.sln

echo "[rivage] Ensuring EF tools…"
dotnet tool restore >/dev/null 2>&1 || true
if ! command -v dotnet-ef >/dev/null 2>&1; then
  dotnet tool install --global dotnet-ef --version 8.0.11 || true
  export PATH="$PATH:/root/.dotnet/tools"
fi

if [ ! -d "/src/src/Rivage.Infrastructure/Migrations" ] || [ -z "$(ls -A /src/src/Rivage.Infrastructure/Migrations 2>/dev/null || true)" ]; then
  echo "[rivage] Creating initial EF migration…"
  dotnet ef migrations add InitialCreate \
    --project src/Rivage.Infrastructure/Rivage.Infrastructure.csproj \
    --startup-project src/Rivage.Web/Rivage.Web.csproj \
    --output-dir Migrations
fi

echo "[rivage] Starting web with hot reload on :8080…"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://+:8080}"
exec dotnet watch --project src/Rivage.Web/Rivage.Web.csproj run --urls "$ASPNETCORE_URLS"
