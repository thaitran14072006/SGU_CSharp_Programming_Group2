#requires -Version 5.1
[CmdletBinding()]
param(
    [string]$DbHost = '127.0.0.1',
    [int]$DbPort = 14333,
    [string]$Database = 'QuanLyTiemBanh',
    [string]$DbUser = 'sa'
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$secret = Read-Host -Prompt "Nhap mat khau SQL Server cho $DbUser@$DbHost" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
try { $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
$builder = New-Object System.Data.Common.DbConnectionStringBuilder
$builder['Server'] = "tcp:$DbHost,$DbPort"
$builder['Database'] = $Database
$builder['User Id'] = $DbUser
$builder['Password'] = $password
$builder['Encrypt'] = 'True'
$builder['TrustServerCertificate'] = 'True' # Chi cho test voi chung chi tu ky
$builder['Connect Timeout'] = '10'
$previous = [Environment]::GetEnvironmentVariable('MOCBAKERY_CONNECTION_STRING', 'Process')
try {
    $env:MOCBAKERY_CONNECTION_STRING = $builder.ConnectionString
    Push-Location $root
    try {
        & dotnet run --project src/MocBakery.GUI/MocBakery.GUI.csproj
        if ($LASTEXITCODE -ne 0) { throw 'Khong chay duoc Desktop. Kiem tra .NET 10 / SQL connection.' }
    } finally { Pop-Location }
} finally {
    if ($null -eq $previous) { Remove-Item Env:MOCBAKERY_CONNECTION_STRING -ErrorAction SilentlyContinue }
    else { $env:MOCBAKERY_CONNECTION_STRING = $previous }
    $password = $null
}
