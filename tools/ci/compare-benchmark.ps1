# Compares a benchmark CSV (TAP.exe -benchmark) with a baseline and writes a table: what got slower by more than the threshold,
# what got faster, and the budgets of FND-13. In a GitHub Actions job the table also goes to the job summary.
# Usage: ./tools/ci/compare-benchmark.ps1 -Current results/benchmark.csv -Baseline Docs/benchmarks/windows-batch.csv [-Threshold 10] [-Fail]
# Works with Windows PowerShell and pwsh. Without a baseline file it only prints the numbers and the budgets.
param(
    [Parameter(Mandatory = $true)][string]$Current,
    [string]$Baseline = "",
    [double]$Threshold = 10,
    [switch]$Fail
)

function Read-Benchmark([string]$path) {
    $lines = Get-Content $path | Where-Object { $_ -and -not $_.StartsWith("#") }
    $rows = @{}
    foreach ($r in ($lines | ConvertFrom-Csv)) { $rows[$r.scenario] = $r }
    return $rows
}
function Num($v) { [double]::Parse([string]$v, [Globalization.CultureInfo]::InvariantCulture) }

$cur = Read-Benchmark $Current
$base = if ($Baseline -and (Test-Path $Baseline)) { Read-Benchmark $Baseline } else { @{} }
$out = New-Object System.Collections.Generic.List[string]
$regressions = 0

$out.Add("## Benchmark")
$out.Add("")
if ($base.Count -eq 0) { $out.Add("No baseline to compare with: the numbers are listed on their own.") ; $out.Add("") }
$out.Add("| Scenario | Metric | Baseline | Now | Change |")
$out.Add("|---|---|---|---|---|")
foreach ($name in $cur.Keys | Sort-Object) {
    $c = $cur[$name]
    $metrics = if ($name -eq "boot" -or $name -eq "startup") { @("seconds") } else { @("avg_ms", "p99_ms", "working_set_mb") }
    foreach ($m in $metrics) {
        $now = Num $c.$m
        if ($base.ContainsKey($name) -and $null -ne $base[$name].$m) {
            $was = Num $base[$name].$m
            $pct = if ($was -gt 0) { ($now - $was) / $was * 100 } else { 0 }
            $mark = if ($pct -gt $Threshold) { "**slower**"; $regressions++ } elseif ($pct -lt -$Threshold) { "faster" } else { "" }
            $out.Add(("| {0} | {1} | {2:N3} | {3:N3} | {4:+0.0;-0.0}% {5} |" -f $name, $m, $was, $now, $pct, $mark))
        }
        else { $out.Add(("| {0} | {1} | | {2:N3} | |" -f $name, $m, $now)) }
    }
}

# Budgets (FND-13). The frame budgets are for a GTX 1660 class PC at 1080p; a run without a window can only show that the CPU's share fits.
$out.Add("")
$out.Add("### Budgets")
$out.Add("")
$out.Add("| Budget | Limit | Now | |")
$out.Add("|---|---|---|---|")
function Budget($label, $limit, $now, $unit) {
    $ok = if ($null -eq $now) { "n/a" } elseif ($now -le $limit) { "ok" } else { "**over**" }
    $out.Add(("| {0} | {1} {2} | {3} | {4} |" -f $label, $limit, $unit, $(if ($null -eq $now) { "-" } else { "{0:N1} {1}" -f $now, $unit }), $ok))
}
Budget "launch of 150 parts: CPU time per frame (60 fps)" 16.7 $(if ($cur.ContainsKey("launch-150")) { Num $cur["launch-150"].avg_ms }) "ms"
Budget "launch of 300 parts: CPU time per frame (30 fps)" 33.3 $(if ($cur.ContainsKey("launch-300")) { Num $cur["launch-300"].avg_ms }) "ms"
$boot = if ($cur.ContainsKey("boot") -and $cur.ContainsKey("startup")) { (Num $cur["boot"].seconds) + (Num $cur["startup"].seconds) }
Budget "start to the space center" 20 $boot "s"
$memory = ($cur.Values | Where-Object { $_.working_set_mb } | ForEach-Object { Num $_.working_set_mb } | Measure-Object -Maximum).Maximum
Budget "memory" 4096 $memory "MB"

$out.Add("")
$out.Add("$regressions metric(s) slower than the baseline by more than $Threshold%.")
$out | ForEach-Object { Write-Host $_ }
if ($env:GITHUB_STEP_SUMMARY) { $out | Add-Content -Path $env:GITHUB_STEP_SUMMARY -Encoding utf8 }
if ($Fail -and $regressions -gt 0) { exit 1 }
exit 0
