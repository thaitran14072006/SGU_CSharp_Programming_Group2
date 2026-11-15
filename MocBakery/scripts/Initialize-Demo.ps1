param(
    [string]$Server = '(localdb)\MSSQLLocalDB',
    [string]$AppFolder = ''
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!$AppFolder) {
    $AppFolder = Join-Path $root 'app'
    if (!(Test-Path -LiteralPath $AppFolder)) { $AppFolder = Join-Path $root 'artifacts\publish' }
}
$AppFolder = [IO.Path]::GetFullPath($AppFolder)
if (!(Test-Path -LiteralPath (Join-Path $AppFolder 'MocBakery.GUI.exe'))) {
    throw 'Khong tim thay MocBakery.GUI.exe. Publish ung dung hoac chon -AppFolder.'
}
if (!(Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw 'Can cai SQL Server command line tools (sqlcmd).' }
$database = 'MocBakery_Demo_' + [Guid]::NewGuid().ToString('N').Substring(0,12)
$schema = Get-Content -Raw -LiteralPath (Join-Path $root 'database\schema\01_CreateDatabase.sql')
$seed = Get-Content -Raw -LiteralPath (Join-Path $root 'database\seeds\04_ExcelDesign_Demo.sql')
$script = ($schema + "`nGO`n" + $seed).Replace('QuanLyTiemBanh',$database)
$tempFile = [IO.Path]::GetTempFileName()
try {
    [IO.File]::WriteAllText($tempFile,$script,[Text.UTF8Encoding]::new($true))
    & sqlcmd -S $Server -E -C -b -f 65001 -i $tempFile
    if ($LASTEXITCODE -ne 0) { throw 'Tao CSDL demo khong thanh cong; chua doi cau hinh ung dung.' }
} finally { Remove-Item -LiteralPath $tempFile -ErrorAction SilentlyContinue }
$connection = [System.Data.Common.DbConnectionStringBuilder]::new()
$connection['Server']=$Server;$connection['Database']=$database
$connection['Integrated Security']='True';$connection['Encrypt']='True'
$connection['TrustServerCertificate']='True';$connection['Connect Timeout']='5'
$configPath = Join-Path $AppFolder 'appsettings.local.json'
if (Test-Path -LiteralPath $configPath) {
    Copy-Item -LiteralPath $configPath -Destination ($configPath + '.previous-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
@{ConnectionString=$connection.ConnectionString} | ConvertTo-Json | Set-Content -LiteralPath $configPath -Encoding utf8
Write-Output "CSDL demo moi: $database"
Write-Output 'Mo ung dung, chon Thiet lap lan dau de tao quan ly cua ban. Khong co mat khau mac dinh.'
Write-Output (Join-Path $AppFolder 'MocBakery.GUI.exe')
