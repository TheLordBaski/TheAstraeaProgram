# Builds the Windows player with the Unity editor on this machine (the nightly job's first step).
# The editor is found from $env:UNITY_PATH, else the version in ProjectSettings/ProjectVersion.txt under the usual folders.
param([string]$Log = "build-windows.log")

$project = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$version = ((Get-Content (Join-Path $project "ProjectSettings/ProjectVersion.txt"))[0] -split ": ")[1].Trim()
$candidates = @($env:UNITY_PATH,
    "D:\Programs\Unity\$version\Editor\Unity.exe",
    "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe") | Where-Object { $_ -and (Test-Path $_) }
if (-not $candidates) { Write-Error "Unity $version was not found; set UNITY_PATH to its Unity.exe."; exit 2 }
$unity = $candidates | Select-Object -First 1

Write-Host "Building with $unity"
$p = Start-Process -FilePath $unity -PassThru -Wait -ArgumentList @(
    "-batchmode", "-quit", "-projectPath", $project, "-logFile", (Join-Path $project $Log),
    "-executeMethod", "TAP.EditorTools.BuildScript.BuildWindowsBatch")
if ($p.ExitCode -ne 0) { Get-Content (Join-Path $project $Log) -Tail 40; exit $p.ExitCode }
Get-Content (Join-Path $project $Log) | Select-String "\[Build\]" | ForEach-Object { Write-Host $_.Line }
