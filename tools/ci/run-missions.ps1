# Runs mission autotests in the Windows player, in batch mode, and writes the result of each as a table.
# Usage: ./tools/ci/run-missions.ps1 -Exe Build/Windows/TAP.exe -Out results [-Missions "orbit lunar"] [-TimeoutMinutes 120]
# In a GitHub Actions job the table goes to the job summary. Exit code 1 when a mission did not pass.
param(
    [string]$Exe = "Build/Windows/TAP.exe",
    [string]$Out = "results",
    [string]$Missions = "",
    [int]$TimeoutMinutes = 120
)

$all = @("flow", "orbit", "suborbital", "persistence", "docking", "failures", "escape", "lunar")
$list = if ($Missions.Trim()) { @($Missions -split '[ ,]+' | Where-Object { $_ }) } else { $all }
New-Item -ItemType Directory -Force $Out | Out-Null
$exe = (Resolve-Path $Exe).Path

$rows = @()
$failed = 0
foreach ($m in $list) {
    $report = Join-Path (Resolve-Path $Out).Path "$m.md"
    $log = Join-Path (Resolve-Path $Out).Path "$m.log"
    Remove-Item $report, $log -ErrorAction SilentlyContinue
    Write-Host "== $m"
    $sw = [Diagnostics.Stopwatch]::StartNew()
    # -batchmode: without it a window opens, and keys pressed into it would fly the rocket.
    $p = Start-Process -FilePath $exe -ArgumentList @("-batchmode", "-autotest", $m, "-report", $report, "-logFile", $log) -PassThru
    $result = "FAILED"
    $checks = "-"
    if (-not $p.WaitForExit($TimeoutMinutes * 60 * 1000)) {
        $p.Kill()
        $result = "TIMEOUT"
    }
    elseif (Test-Path $report) {
        $text = Get-Content $report -Raw
        if ($text -match "Result: (\w+)") { $result = $Matches[1] }
        if ($text -match "Checks: (\d+/\d+) passed") { $checks = $Matches[1] }
    }
    else { $result = "NO REPORT" }
    $sw.Stop()
    if ($result -ne "PASSED") { $failed++ }
    $mark = if ($result -eq "PASSED") { "pass" } else { "FAIL" }
    $rows += "| $m | $mark ($result) | $checks | $([int]$sw.Elapsed.TotalMinutes) min |"
    Write-Host "   $result $checks"
}

$table = @("## Missions", "", "| Mission | Result | Checks | Time |", "|---|---|---|---|") + $rows
$table | ForEach-Object { Write-Host $_ }
if ($env:GITHUB_STEP_SUMMARY) { $table | Add-Content -Path $env:GITHUB_STEP_SUMMARY -Encoding utf8 }
$table | Set-Content -Path (Join-Path $Out "summary.md") -Encoding utf8
exit $(if ($failed -gt 0) { 1 } else { 0 })
