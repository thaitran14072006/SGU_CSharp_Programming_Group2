param([switch]$Staff, [switch]$NewFixture)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$fixtureRoot = Join-Path $projectRoot 'artifacts\gui-test'
$fixtureFile = Join-Path $fixtureRoot 'database.txt'
$testProject = Join-Path $PSScriptRoot 'MocBakery.GuiChecks.csproj'
dotnet build $testProject -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'GUI test host build failed.' }
New-Item -ItemType Directory -Force -Path $fixtureRoot | Out-Null
if ($NewFixture -or !(Test-Path -LiteralPath $fixtureFile)) {
    $fixtureName = 'MocBakery_NhuGuiTest_' + (Get-Date -Format yyyyMMdd) + '_' + [Guid]::NewGuid().ToString('N').Substring(0,6)
    $schema = (Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'database\schema\01_CreateDatabase.sql')).Replace('QuanLyTiemBanh',$fixtureName)
    $seed = (Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'database\seeds\03_CatalogInventory_Demo.sql')).Replace('QuanLyTiemBanh',$fixtureName)
    $scriptFile = Join-Path $fixtureRoot 'setup.sql'
    Set-Content -LiteralPath $scriptFile -Value ($schema + "`nGO`n" + $seed) -Encoding utf8
    sqlcmd -S '(localdb)\MSSQLLocalDB' -E -C -b -f 65001 -i $scriptFile
    if ($LASTEXITCODE -ne 0) { throw 'Isolated SQL fixture creation failed.' }
    Set-Content -LiteralPath $fixtureFile -Value $fixtureName
}
$fixtureName = (Get-Content -Raw -LiteralPath $fixtureFile).Trim()
if ($fixtureName -notmatch '^MocBakery_NhuGuiTest_[a-zA-Z0-9_]+$') { throw 'Unexpected fixture database name.' }
$outputFolder = Join-Path $PSScriptRoot 'bin\Release\net10.0-windows'
@{Database=$fixtureName;Staff=[bool]$Staff} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputFolder 'fixture.local.json') -Encoding utf8
Write-Output "Prepared isolated GUI fixture: $fixtureName"
Write-Output ('Open test host: ' + (Join-Path $outputFolder 'MocBakery.GuiChecks.exe'))
