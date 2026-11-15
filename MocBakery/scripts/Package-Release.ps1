param([string]$ReleaseName = ('MocBakery-Final-' + (Get-Date -Format yyyy-MM-dd)))
$ErrorActionPreference='Stop'
if($ReleaseName -notmatch '^MocBakery-[a-zA-Z0-9-]+$'){throw 'Invalid release name'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$repo=[IO.Path]::GetFullPath((Join-Path $root '..'))
$output=Join-Path $root ('artifacts\'+$ReleaseName)
New-Item -ItemType Directory -Force -Path $output | Out-Null
& dotnet publish (Join-Path $root 'src\MocBakery.GUI\MocBakery.GUI.csproj') -c Release -r win-x64 --self-contained true -o (Join-Path $output 'app') --nologo
if($LASTEXITCODE -ne 0){throw 'Publish failed'}
foreach($name in @('database','docs')){
    $destination=Join-Path $output $name
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $root $name) | Copy-Item -Destination $destination -Recurse -Force
}
New-Item -ItemType Directory -Force -Path (Join-Path $output 'scripts') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Initialize-Demo.ps1') -Destination (Join-Path $output 'scripts') -Force
Copy-Item -LiteralPath (Join-Path $root 'docs\deployment\CHOT_DO_AN.md') -Destination (Join-Path $output 'BAT_DAU_TAI_DAY.md') -Force
Copy-Item -LiteralPath (Join-Path $root 'docs\report\CHUONG_5_6_TONG_HOP.md') -Destination $output -Force
$safe='safe.directory='+$repo.Replace('\','/')
$commit=& git -c $safe -C $repo rev-parse HEAD
if($LASTEXITCODE -ne 0){throw 'Cannot identify source commit'}
$commit.Trim() | Set-Content -LiteralPath (Join-Path $output 'SOURCE_COMMIT.txt')
$sourceZip=Join-Path $output 'Source.zip'
& git -c $safe -C $repo archive --format=zip --output $sourceZip HEAD MocBakery README.md
if($LASTEXITCODE -ne 0){throw 'Source archive failed'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath=$output+'.zip'
$tempZip=$zipPath+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
$archive=[IO.Compression.ZipFile]::Open($tempZip,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach($file in Get-ChildItem -LiteralPath $output -Recurse -File){
        $relative=[IO.Path]::GetRelativePath($output,$file.FullName).Replace('\','/')
        if($relative -like '*.local.json*' -or $relative -like 'app/logs/*' -or $relative -like '*.bak' -or $relative -like '*.pdb'){continue}
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.FullName,$ReleaseName+'/'+$relative,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $archive.Dispose() }
Move-Item -LiteralPath $tempZip -Destination $zipPath -Force
$hash=Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
($hash.Hash+'  '+[IO.Path]::GetFileName($zipPath)) | Set-Content -LiteralPath ($zipPath+'.sha256.txt')
Write-Output "Source commit: $commit"
Write-Output "Release: $zipPath"
Write-Output "SHA256: $($hash.Hash)"
