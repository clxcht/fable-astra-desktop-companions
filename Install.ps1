$ErrorActionPreference = 'Stop'
$companionSource = Join-Path $PSScriptRoot 'FableAstra'
$companionRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'FableAstra'
$companionDestination = Join-Path $companionRoot 'App'
$companionExe = Join-Path $companionDestination 'FableAstra.exe'
if (-not (Test-Path -LiteralPath (Join-Path $companionSource 'FableAstra.exe'))) { throw 'The FableAstra folder must be beside Install.ps1.' }
Get-Process -Name FableAstra -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $companionExe } | Stop-Process
New-Item -ItemType Directory -Path $companionDestination -Force | Out-Null
Get-ChildItem -LiteralPath $companionSource | Copy-Item -Destination $companionDestination -Recurse -Force
$companionRun = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
if (-not (Test-Path -LiteralPath $companionRun)) { New-Item -Path $companionRun | Out-Null }
New-ItemProperty -Path $companionRun -Name 'FableAstra' -Value ('"' + $companionExe + '" --startup') -PropertyType String -Force | Out-Null
$companionShell = New-Object -ComObject WScript.Shell
$companionPrograms = [Environment]::GetFolderPath('Programs')
$companionShortcut = $companionShell.CreateShortcut((Join-Path $companionPrograms 'Fable + Astra.lnk'))
$companionShortcut.TargetPath = $companionExe
$companionShortcut.WorkingDirectory = $companionDestination
$companionShortcut.Description = 'Fable chan and Astra chan desktop companions'
$companionShortcut.Save()
Start-Process -FilePath $companionExe
Write-Output ('Installed and launched: ' + $companionExe)
Write-Output 'Startup is enabled for your Windows account. Find Fable + Astra in the Start menu.'
