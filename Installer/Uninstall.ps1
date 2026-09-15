param([switch]$Quiet)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
if (-not $Quiet) {
    $answer = [System.Windows.Forms.MessageBox]::Show('Uninstall Fable + Astra? Your preferences will be kept for a future reinstall.', 'Fable + Astra', 'YesNo', 'Question')
    if ($answer -ne 'Yes') { exit 0 }
}
$companionRoot = [IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'FableAstra'))
$companionApp = [IO.Path]::GetFullPath((Join-Path $companionRoot 'App'))
$companionExe = Join-Path $companionApp 'FableAstra.exe'
if ($companionApp -ne ($companionRoot + '\App')) { throw 'Unexpected uninstall path.' }
foreach ($process in @(Get-Process -Name FableAstra -ErrorAction SilentlyContinue | Where-Object Path -EQ $companionExe)) {
    $null = $process.CloseMainWindow()
    if (-not $process.WaitForExit(5000)) { Stop-Process -InputObject $process; $null = $process.WaitForExit(5000) }
    if (-not $process.HasExited) { throw 'Close the companions from the tray and try again.' }
}
Remove-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name FableAstra -ErrorAction SilentlyContinue
$shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Fable + Astra.lnk'
if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut -Force }
if (Test-Path -LiteralPath $companionApp) { Remove-Item -LiteralPath $companionApp -Recurse -Force }
$registration = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FableAstra'
if (Test-Path -LiteralPath $registration) { Remove-Item -LiteralPath $registration -Force }
$metadata = Join-Path $companionRoot 'installation.json'
if (Test-Path -LiteralPath $metadata) { Remove-Item -LiteralPath $metadata -Force }
$uninstaller = Join-Path $companionRoot 'Uninstall.ps1'
if (Test-Path -LiteralPath $uninstaller) { Remove-Item -LiteralPath $uninstaller -Force }
if (-not $Quiet) { [System.Windows.Forms.MessageBox]::Show('Fable + Astra were uninstalled. Your saved preferences remain on this computer.', 'Fable + Astra') | Out-Null }
