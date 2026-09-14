param([ValidateSet('win-arm64','win-x64','win-x86')][string]$Runtime = 'win-arm64')
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$companionProject = Join-Path $PSScriptRoot 'Source\FableAstra.csproj'
$companionOutput = Join-Path $PSScriptRoot 'FableAstra'
dotnet publish $companionProject -c Release -r $Runtime --self-contained false -o $companionOutput
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
