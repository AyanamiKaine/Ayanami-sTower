<#
Demo script for native function hot-reload workflow.

This script demonstrates:
1. Building the initial math_ops.dll
2. Running the VM with the native function (add operation)
3. Modifying math_ops.c to multiply instead
4. Recompiling the DLL
5. Hot-reloading the function in the running VM
6. Showing the changed behavior

Run from SFPM-C/examples directory.
#>

$scriptDir = $PSScriptRoot
$projectRoot = Resolve-Path (Join-Path $scriptDir '..')
$buildReleasePath = Join-Path $projectRoot 'build\Release'
$buildRelease = Resolve-Path $buildReleasePath -ErrorAction SilentlyContinue

if (-not $buildRelease) {
    Write-Error "Release build directory not found at: $buildReleasePath"
    Write-Host "Please build the project first:"
    Write-Host "  cd ..\build"
    Write-Host "  cmake --build . --config Release"
    exit 1
}

$nativeReloadExe = Join-Path $buildRelease 'sfpm_native_reload.exe'
$mathOpsDll = Join-Path $buildRelease 'math_ops.dll'

if (-not (Test-Path $nativeReloadExe)) {
    Write-Error "Executable not found: $nativeReloadExe"
    exit 1
}

if (-not (Test-Path $mathOpsDll)) {
    Write-Error "DLL not found: $mathOpsDll"
    Write-Host "Build the math_ops library first:"
    Write-Host "  cmake --build build --config Release --target math_ops"
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Native Function Hot-Reload Demo" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Show initial DLL content
Write-Host "[1] Initial math_ops.dll content (should return a + b):" -ForegroundColor Yellow
Get-Content (Join-Path $scriptDir 'math_ops.c') | Select-String -Pattern "return.*;" -Context 0,0 | Select-Object -First 1
Write-Host ""

# First run with ADD operation
Write-Host "[2] Running VM with initial library (10 + 5 = 15):" -ForegroundColor Yellow
$firstInput = @"
1
6
"@
$firstInput | & $nativeReloadExe
Write-Host ""

# Backup original file
$mathOpsSource = Join-Path $scriptDir 'math_ops.c'
$mathOpsBackup = Join-Path $scriptDir 'math_ops.c.bak'
Copy-Item $mathOpsSource $mathOpsBackup -Force

Write-Host "[3] Modifying math_ops.c to multiply instead of add..." -ForegroundColor Yellow

# Modify the source to multiply
$content = Get-Content $mathOpsSource -Raw
$content = $content -replace 'return a \+ b;.*', 'return a * b;  /* HOT-RELOADED: Now multiplies! */'
$content = $content -replace 'return 1;.*get_version', 'return 2;  /* Version incremented after hot-reload */'
Set-Content $mathOpsSource $content -NoNewline

Write-Host "Modified line:" -ForegroundColor Green
Get-Content $mathOpsSource | Select-String -Pattern "return.*\*.*;" -Context 0,0 | Select-Object -First 1
Write-Host ""

# Recompile the DLL
Write-Host "[4] Recompiling math_ops.dll..." -ForegroundColor Yellow
Push-Location $buildRelease

# Use the compiler from the build environment
$vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vsWhere) {
    $vsPath = & $vsWhere -latest -property installationPath
    $vcvarsPath = Join-Path $vsPath 'VC\Auxiliary\Build\vcvars64.bat'
    
    if (Test-Path $vcvarsPath) {
        # Compile using MSVC
        $compileCmd = "cmd /c `"$vcvarsPath`" && cl /LD /nologo /O2 /DNDEBUG `"$mathOpsSource`" /Fe:math_ops.dll 2>&1"
        Invoke-Expression $compileCmd | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Recompilation successful!" -ForegroundColor Green
        } else {
            Write-Error "Compilation failed"
            Pop-Location
            # Restore backup
            Move-Item $mathOpsBackup $mathOpsSource -Force
            exit 1
        }
    }
}

Pop-Location
Write-Host ""

# Interactive session: load library, run, quit
Write-Host "[5] Running VM and hot-reloading the modified library:" -ForegroundColor Yellow
Write-Host "    This will reload math_ops.dll and run again (10 * 5 = 50):" -ForegroundColor Yellow

$secondInput = @"
2
math_ops.dll
math_add
0
1
6
"@

$secondInput | & $nativeReloadExe

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Demo Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "What happened:" -ForegroundColor Green
Write-Host "  1. Initial run: math_add returned 10 + 5 = 15" -ForegroundColor White
Write-Host "  2. Modified source: changed 'a + b' to 'a * b'" -ForegroundColor White
Write-Host "  3. Recompiled: math_ops.dll updated while VM running" -ForegroundColor White
Write-Host "  4. Hot-reload: VM reloaded DLL without restart" -ForegroundColor White
Write-Host "  5. Second run: same call now returns 10 * 5 = 50" -ForegroundColor White
Write-Host ""

# Restore original file
Write-Host "Restoring original math_ops.c..." -ForegroundColor Yellow
Move-Item $mathOpsBackup $mathOpsSource -Force

# Rebuild original DLL
Write-Host "Rebuilding original math_ops.dll..." -ForegroundColor Yellow
Push-Location $buildRelease
if (Test-Path $vcvarsPath) {
    $compileCmd = "cmd /c `"$vcvarsPath`" && cl /LD /nologo /O2 /DNDEBUG `"$mathOpsSource`" /Fe:math_ops.dll 2>&1"
    Invoke-Expression $compileCmd | Out-Null
}
Pop-Location

Write-Host "Original files restored!" -ForegroundColor Green
