#requires -Version 5.1
[CmdletBinding()]
param([switch]$SeedDemo)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    if (-not (Test-Path '.env')) { throw 'Thieu .env. Chay Copy-Item .env.example .env va dat mat khau manh.' }
    if (-not (Test-Path 'compose.yaml')) { throw 'Khong tim thay compose.yaml cua nhom.' }
    & docker compose config --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Cau hinh Docker Compose khong hop le.' }
    & docker compose up -d
    if ($LASTEXITCODE -ne 0) { throw 'Khong khoi dong duoc Docker Compose.' }
    $container = & docker compose ps -q sqlserver
    $composeExitCode = $LASTEXITCODE

    if ($composeExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($container)) {
        throw 'Khong tim thay container sqlserver.'
    }

    $container = $container.Trim()
    Write-Host "Da tim thay SQL Server container: $container"

    $sqlcmd = '/opt/mssql-tools18/bin/sqlcmd'
    $ping = 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -Q "SET NOCOUNT ON; SELECT 1"'
    $ready = $false
    for ($i = 0; $i -lt 45; $i++) {
        & docker exec $container bash -lc $ping *> $null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 2
    }
    if (-not $ready) { throw 'SQL Server chua san sang. Xem docker compose logs sqlserver.' }

    function Invoke-SqlFile([string]$localFile, [string]$remoteFile) {
        if (-not (Test-Path $localFile)) { throw "Khong tim thay $localFile" }
        & docker cp $localFile "${container}:$remoteFile"
        if ($LASTEXITCODE -ne 0) { throw "Khong copy duoc $localFile" }
        $run = 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -i ' + $remoteFile
        & docker exec $container bash -lc $run
        if ($LASTEXITCODE -ne 0) { throw "SQL script that bai: $localFile" }
    }

    $check = 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N''QuanLyTiemBanh'') IS NULL THEN 0 ELSE 1 END"'
    $databaseExistsOutput = (& docker exec $container bash -lc $check | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Khong kiem tra duoc database.' }
    if ($databaseExistsOutput -match '(?m)^0\s*$') {
        Write-Host 'Tao database va schema tu script goc cua nhom...'
        Invoke-SqlFile 'database/schema/01_CreateDatabase.sql' '/tmp/mocbakery-schema.sql'
    } elseif ($databaseExistsOutput -match '(?m)^1\s*$') {
        Write-Host 'Database da ton tai: GIU NGUYEN, khong chay lai schema.'
    } else { throw "Ket qua kiem tra database khong xac dinh: $databaseExistsOutput" }

    $coreCount = 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -h -1 -W -d QuanLyTiemBanh -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.tables WHERE name IN (N''TaiKhoan'',N''LoaiBanh'',N''SanPham'',N''LoBanh'',N''KhachHang'',N''HoaDon'',N''ChiTietHoaDon'',N''DonDatBanh'',N''ChiTietDonDat'')"'
    $tables = (& docker exec $container bash -lc $coreCount | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $tables -notmatch '(?m)^9\s*$') { throw "Khong du 9 bang cot loi (tim thay: $tables). Dung lai de tranh ghi de." }
    Write-Host 'Da xac nhan du 9 bang cot loi.'

    if ($SeedDemo) {
        Invoke-SqlFile 'database/seeds/02_Customers.sql' '/tmp/mocbakery-customers.sql'
        Invoke-SqlFile 'database/seeds/03_DemoCatalogInventory.sql' '/tmp/mocbakery-demo.sql'
        Write-Host 'Da nap seed khach hang va catalog/ton kho DEMO (khong tao tai khoan mac dinh).'
    }
    Invoke-SqlFile 'database/queries/VerifySharedDatabase.sql' '/tmp/mocbakery-verify.sql'
    Write-Host 'HOAN TAT. Ket noi Desktop: localhost,14333 / QuanLyTiemBanh.'
} finally { Pop-Location }
