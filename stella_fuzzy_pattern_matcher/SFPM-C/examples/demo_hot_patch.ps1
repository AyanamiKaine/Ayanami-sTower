<#
Demo script for automating the interactive hot-patch flow.

Flow:
 1) View program
 2) Patch offset 4 (OP_ADD -> OP_MUL)
 3) View program
 4) Run program
 5) Save snapshot
 6) Quit
 7) Restart example to auto-load snapshot and exit

Run this script from the repository root (SFPM-C folder) or it will try to resolve paths relative to this file.
#>

$scriptDir = $PSScriptRoot
$projectRoot = Resolve-Path (Join-Path $scriptDir '..')
# Build Release folder is located at <repo>/stella_fuzzy_pattern_matcher/SFPM-C/build/Release
$buildReleasePath = Join-Path $projectRoot 'build\Release'
$buildRelease = Resolve-Path $buildReleasePath -ErrorAction SilentlyContinue
if (-not $buildRelease) {
    Write-Error "Release build directory not found. Build the project first. Expected: ..\build\Release"
    exit 1
}

$hotReloadExe = Join-Path $buildRelease 'sfpm_hot_reload.exe'
if (-not (Test-Path $hotReloadExe)) {
    Write-Error "Executable not found: $hotReloadExe. Build the examples first (cmake --build . --config Release --target sfpm_hot_reload)"
    exit 1
}

Write-Host "Using exe: $hotReloadExe`n"

# First interaction: patch -> save -> quit
$firstInput = @"
5
2
4
3
5
1
3
7
n
"@

Write-Host "Running interactive demo (patch -> save -> quit)..."
$firstInput | & $hotReloadExe

Start-Sleep -Seconds 1

# Second run: let it auto-load, then quit immediately
$secondInput = @"
7
n
"@
Write-Host "Restarting to show snapshot load and then auto-quit..."
$secondInput | & $hotReloadExe

Write-Host "Demo complete. The snapshot file 'interpreter.img' should be in the Release folder: $buildRelease\interpreter.img"