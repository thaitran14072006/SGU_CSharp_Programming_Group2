#requires -Version 5.1
[CmdletBinding()]
param([switch]$BuildWeb)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Can cai .NET SDK 10.' }
    & dotnet run --project tests/WebOrdersNam/MocBakery.WebOrdersNamChecks.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Nam Web Orders BLL checks failed.' }
    if ($BuildWeb) {
        & dotnet build src/MocBakery.Web/MocBakery.Web.csproj -c Release
        if ($LASTEXITCODE -ne 0) { throw 'Web Release build failed.' }
    }
    Write-Host 'Nam module checks completed. SQL/HTTP/GUI integration is NOT included.' -ForegroundColor Green
} finally { Pop-Location }
