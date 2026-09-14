$ErrorActionPreference = 'Stop'
$companionLocal = [Environment]::GetFolderPath('LocalApplicationData')
$companionExpected = [IO.Path]::GetFullPath((Join-Path $companionLocal 'FableAstra\App'))
$companionExe = Join-Path $companionExpected 'FableAstra.exe'
Get-Process -Name FableAstra -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $companionExe } | Stop-Process
Remove-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'FableAstra' -ErrorAction SilentlyContinue
$companionShortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Fable + Astra.lnk'
if (Test-Path -LiteralPath $companionShortcut) { Remove-Item -LiteralPath $companionShortcut }
if (Test-Path -LiteralPath $companionExpected) {
    $companionResolved = (Resolve-Path -LiteralPath $companionExpected).Path
    if ($companionResolved -ne $companionExpected -or -not $companionResolved.StartsWith([IO.Path]::GetFullPath($companionLocal) + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected installation path; no files were deleted.' }
    Remove-Item -LiteralPath $companionResolved -Recurse -Force
}
Write-Output 'Companions removed. Your settings are kept in LocalAppData\FableAstra.'
