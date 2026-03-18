#!/usr/bin/env pwsh
# Build and pack the Aelena.FileApi.Core NuGet package

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "$PSScriptRoot/../artifacts"
)

$ErrorActionPreference = "Stop"

$solutionRoot = Split-Path $PSScriptRoot -Parent
$coreProject = Join-Path $solutionRoot "src/Aelena.FileApi.Core/Aelena.FileApi.Core.csproj"

Write-Host "Building and packing Aelena.FileApi.Core..." -ForegroundColor Cyan

# Clean
dotnet clean $coreProject -c $Configuration --nologo -v q

# Pack (includes build)
dotnet pack $coreProject `
    -c $Configuration `
    -o $OutputDir `
    --nologo `
    -p:ContinuousIntegrationBuild=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "Pack failed!" -ForegroundColor Red
    exit 1
}

$packages = Get-ChildItem $OutputDir -Filter "*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host "Package created: $($packages.FullName)" -ForegroundColor Green
Write-Host "Size: $([math]::Round($packages.Length / 1KB, 1)) KB" -ForegroundColor Green
