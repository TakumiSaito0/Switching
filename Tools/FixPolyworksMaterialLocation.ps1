$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectSettings = Join-Path $projectRoot "ProjectSettings\ProjectVersion.txt"
$polyworksRoot = Join-Path $projectRoot "Assets\PrivateFolder\Off Axis Studios\Polyworks"

if (-not (Test-Path -LiteralPath $projectSettings -PathType Leaf)) {
    throw "Unity project root could not be found: $projectRoot"
}

if (-not (Test-Path -LiteralPath $polyworksRoot -PathType Container)) {
    throw "Polyworks could not be found: $polyworksRoot"
}

$unityProcesses = @(Get-Process -Name "Unity" -ErrorAction SilentlyContinue)
if ($unityProcesses.Count -gt 0) {
    throw "Close Unity before running this tool."
}

$files = @(Get-ChildItem -LiteralPath $polyworksRoot -Filter "*.fbx.meta" -File -Recurse)
$changed = 0

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $updated = [System.Text.RegularExpressions.Regex]::Replace(
        $content,
        "(?m)^(\s*materialLocation:\s*)0(\s*)$",
        '${1}1${2}')

    if ($updated -eq $content) {
        continue
    }

    [System.IO.File]::WriteAllText(
        $file.FullName,
        $updated,
        [System.Text.UTF8Encoding]::new($false))
    $changed++
}

Write-Host "Scanned: $($files.Count) FBX meta files"
Write-Host "Changed: $changed legacy material settings"
Write-Host "Done. Open Unity and wait for the one-time reimport to finish."

