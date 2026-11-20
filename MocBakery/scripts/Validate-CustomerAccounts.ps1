param(
    [switch]$IncludeSql,
    [switch]$Offline,
    [switch]$NoBuild
)
$ErrorActionPreference = 'Stop'
if ($IncludeSql -and $Offline) {
    throw 'Khong the kiem thu SQL voi -Offline; can build voi SQL driver.'
}
if ($IncludeSql -and [string]::IsNullOrWhiteSpace($env:MOCBAKERY_SQL_TEST_CONNECTION)) {
    throw 'Can dat MOCBAKERY_SQL_TEST_CONNECTION cho server thu co quyen tao CSDL. Khong dung ket noi ung dung.'
}
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$evidence = Join-Path $root ('MocBakery/artifacts/customer-accounts-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
'NOT RUN: SQL integration.' | Set-Content -Encoding utf8 (Join-Path $evidence 'customer-sql.txt')
function Run-Logged([string]$Name, [string[]]$Arguments) {
    & dotnet @Arguments 2>&1 | Tee-Object -FilePath (Join-Path $evidence ($Name + '.txt'))
    if ($LASTEXITCODE -ne 0) { throw "Kiem tra that bai: $Name. Xem $evidence" }
}
Push-Location $root
try {
    @(
        'Date: ' + (Get-Date -Format o)
        'Base commit: ' + (git rev-parse HEAD)
        'SQL requested: ' + $IncludeSql.IsPresent
        'Offline build: ' + $Offline.IsPresent
        'Build skipped: ' + $NoBuild.IsPresent
    ) | Set-Content -Encoding utf8 (Join-Path $evidence 'context.txt')
    $projects = @(
        'MocBakery/tests/CustomerAccounts/MocBakery.CustomerAccountChecks.csproj'
        'MocBakery/tests/WebAccounts/MocBakery.WebAccountChecks.csproj'
    )
    if (!$NoBuild) {
        foreach ($project in $projects) {
            $arguments = @('build', $project, '-c', 'Release', '--nologo', '-m:1', '-p:UseSharedCompilation=false')
            if ($Offline) { $arguments += '-p:RestoreSqlClient=false' }
            Run-Logged ([IO.Path]::GetFileNameWithoutExtension($project) + '-build') $arguments
        }
    }
    Run-Logged 'customer-business' @('MocBakery/tests/CustomerAccounts/bin/Release/net10.0/MocBakery.CustomerAccountChecks.dll')
    $web = 'MocBakery/tests/WebAccounts/bin/Release/net10.0/MocBakery.WebAccountChecks.dll'
    Run-Logged 'customer-http' @($web)
    if ($IncludeSql) {
        Run-Logged 'customer-sql' @($web, '--sql')
    } else {
        'NOT RUN: SQL integration. Use -IncludeSql and MOCBAKERY_SQL_TEST_CONNECTION.' |
            Set-Content -Encoding utf8 (Join-Path $evidence 'customer-sql.txt')
    }
    'PASS: Requested checks completed. See customer-sql.txt for SQL status.' |
        Set-Content -Encoding utf8 (Join-Path $evidence 'status.txt')
    Write-Output "Hoan tat kiem tra tai khoan khach. Log: $evidence"
} catch {
    'FAILED: Requested checks did not complete successfully. See individual logs.' |
        Set-Content -Encoding utf8 (Join-Path $evidence 'status.txt')
    throw
} finally { Pop-Location }
