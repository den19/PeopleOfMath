# Restart local OPUS-MT translation until inventory is clean. No external APIs.
$ErrorActionPreference = "Continue"
$env:PYTHONIOENCODING = "utf-8"
Set-Location "c:\git\PeopleOfMath"

$Python = "C:\Users\den\AppData\Local\Programs\Python\Python314\python.exe"
if (-not (Test-Path $Python)) { $Python = "python" }

$busy = Get-CimInstance Win32_Process -Filter "Name='python.exe'" |
    Where-Object { $_.CommandLine -like '*translate_en_fields*' -or $_.CommandLine -like '*translate_en_local*' }
if ($busy) {
    Write-Host "Translation already running; exit."
    exit 0
}

$maxRounds = 120
for ($round = 1; $round -le $maxRounds; $round++) {
    Write-Host "==== ROUND $round (local OPUS-MT) ===="
    & $Python -u Tools\translate_en_fields.py --limit 8
    $code = $LASTEXITCODE
    Write-Host "exit=$code"

    & $Python Tools\_inventory_en_fields.py | Out-Null
    $report = Get-Content Tools\_en_inventory_report.json -Raw | ConvertFrom-Json
    $need = $report.assets_needing_en
    Write-Host "assets_needing_en=$need"
    if ($need -le 0) {
        Write-Host "COMPLETE"
        break
    }
    Start-Sleep -Seconds 1
}
