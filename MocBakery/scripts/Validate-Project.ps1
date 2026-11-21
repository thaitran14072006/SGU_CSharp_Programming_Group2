param([switch]$IncludeGui, [switch]$IncludeCustomerSql)
$ErrorActionPreference='Stop'
if($IncludeCustomerSql -and [string]::IsNullOrWhiteSpace($env:MOCBAKERY_SQL_TEST_CONNECTION)){
    throw 'Can dat MOCBAKERY_SQL_TEST_CONNECTION cho server thu truoc khi chay -IncludeCustomerSql.'
}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
Push-Location $root
try {
    $evidence=Join-Path $root ('MocBakery\artifacts\validation-' + (Get-Date -Format yyyyMMdd-HHmmss))
    New-Item -ItemType Directory -Force -Path $evidence | Out-Null
    function Run-Logged([string]$Name,[string[]]$Arguments) {
        & dotnet @Arguments 2>&1 | Tee-Object -FilePath (Join-Path $evidence ($Name+'.txt'))
        if($LASTEXITCODE -ne 0){throw "Kiem tra that bai: $Name"}
    }
    Run-Logged 'build' @('build','MocBakery/MocBakery.sln','-c','Release','--nologo')
    Run-Logged 'accounts-customers' @('MocBakery/tests/Unit/bin/Release/net10.0/MocBakery.UnitChecks.dll')
    Run-Logged 'customer-business' @('MocBakery/tests/CustomerAccounts/bin/Release/net10.0/MocBakery.CustomerAccountChecks.dll')
    $webAccounts='MocBakery/tests/WebAccounts/bin/Release/net10.0/MocBakery.WebAccountChecks.dll'
    Run-Logged 'customer-http' @($webAccounts)
    $webCart='MocBakery/tests/WebCart/bin/Release/net10.0/MocBakery.WebCartChecks.dll'
    Run-Logged 'web-cart-http' @($webCart)
    # SQL cart checks resolve schema paths from the MocBakery working directory.
    Push-Location (Join-Path $root 'MocBakery')
    try {
        Run-Logged 'web-cart-sql' @('tests/WebCart/bin/Release/net10.0/MocBakery.WebCartChecks.dll','--sql')
    } finally { Pop-Location }
    Run-Logged 'public-catalog-web-orders' @('MocBakery/tests/PublicCatalog/bin/Release/net10.0/MocBakery.PublicCatalogChecks.dll')
    if($IncludeCustomerSql){
        Run-Logged 'customer-sql' @($webAccounts,'--sql')
    } else {
        'NOT RUN: Customer SQL integration. Use -IncludeCustomerSql with MOCBAKERY_SQL_TEST_CONNECTION.' |
            Set-Content -Encoding utf8 (Join-Path $evidence 'customer-sql.txt')
    }
    $checks='MocBakery/tests/Unit/CatalogInventory/bin/Release/net10.0/MocBakery.CatalogInventoryChecks.dll'
    Run-Logged 'catalog-inventory' @($checks)
    Run-Logged 'integration' @($checks,'--integration')
    Run-Logged 'excel-rules' @($checks,'--excel')
    Run-Logged 'full-system' @($checks,'--system')
    $env:MOCBAKERY_UI_OUTPUT=Join-Path $evidence 'layout'
    Run-Logged 'layout' @('MocBakery/src/MocBakery.GUI/bin/Release/net10.0-windows/MocBakery.GUI.dll','--ui-check')
    if($IncludeGui){
        # The GUI host only accepts an isolated fixture prepared by this repository.
        & '.\MocBakery\tests\System\PrepareGuiFixture.ps1'
        if($LASTEXITCODE -ne 0){throw 'Khong chuan bi duoc GUI fixture.'}
        $fixture=(Get-Content -Raw 'MocBakery/artifacts/gui-test/database.txt').Trim()
        if($fixture -notmatch '^MocBakery_NhuGuiTest_[a-zA-Z0-9_]+$'){throw 'Unexpected fixture name'}
        & sqlcmd -S '(localdb)\MSSQLLocalDB' -E -C -b -f 65001 -d $fixture -i 'MocBakery/database/schema/03_Upgrade_StaffAccounts.sql'
        if($LASTEXITCODE -ne 0){throw 'Khong nang cap duoc GUI fixture.'}
        $gui='MocBakery/tests/System/bin/Release/net10.0-windows/MocBakery.GuiChecks.dll'
        $env:MOCBAKERY_UI_OUTPUT=Join-Path $evidence 'nhu-gui'
        Run-Logged 'nhu-gui' @($gui,'--verify-design','--owner')
        $env:MOCBAKERY_UI_OUTPUT=Join-Path $evidence 'thai-gui'
        Run-Logged 'thai-gui' @($gui,'--verify-thai','--owner')
    }
    Write-Output "Hoan tat. Log: $evidence"
} finally { Remove-Item Env:MOCBAKERY_UI_OUTPUT -ErrorAction SilentlyContinue;Pop-Location }
