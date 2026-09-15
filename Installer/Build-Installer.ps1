param([string]$BuildDirectory = (Join-Path $env:TEMP 'FableAstra-InstallerBuild'))
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$projectRoot = Split-Path $PSScriptRoot -Parent
$buildRoot = [IO.Path]::GetFullPath($BuildDirectory)
$buildRun = Join-Path $buildRoot ([Guid]::NewGuid().ToString('N'))
$payload = Join-Path $buildRun 'App'
dotnet publish (Join-Path $projectRoot 'Source\FableAstra.csproj') -c Release -r win-arm64 --self-contained true -p:PublishReadyToRun=false -o $payload
if ($LASTEXITCODE -ne 0) { throw 'App publish failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'FableAstra\Assets') -Destination $payload -Recurse
$manifest = [ordered]@{}
Get-ChildItem -LiteralPath $payload -Recurse -File | Sort-Object FullName | ForEach-Object {
    $relative = $_.FullName.Substring($payload.Length + 1).Replace('\', '/')
    $manifest[$relative] = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $payload 'manifest.json') -Encoding UTF8
Add-Type -AssemblyName System.IO.Compression.FileSystem
$payloadZip = Join-Path $buildRun 'payload.zip'
[IO.Compression.ZipFile]::CreateFromDirectory($payload, $payloadZip, 'Optimal', $false)
$published = Join-Path $buildRun 'Setup'
dotnet publish (Join-Path $PSScriptRoot 'FableAstra.Setup.csproj') -c Release -r win-arm64 --self-contained true ('-p:PayloadPath=' + $payloadZip) -o $published
if ($LASTEXITCODE -ne 0) { throw 'Installer publish failed.' }
$release = Join-Path $projectRoot 'Release'
New-Item -ItemType Directory -Path $release -Force | Out-Null
$installer = Join-Path $release 'Fable-Astra-Setup-v1.2.1-win-arm64.exe'
Copy-Item -LiteralPath (Join-Path $published 'Fable-Astra-Setup.exe') -Destination $installer -Force
$digest = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
($digest + '  ' + [IO.Path]::GetFileName($installer)) | Set-Content -LiteralPath (Join-Path $release 'SHA256SUMS.txt') -Encoding ASCII
Write-Output $installer
