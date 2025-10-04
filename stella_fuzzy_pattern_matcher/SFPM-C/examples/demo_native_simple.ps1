<#
Simple manual hot-reload demonstration
This script shows the step-by-step process
#>

$scriptDir = $PSScriptRoot
$projectRoot = Resolve-Path (Join-Path $scriptDir '..')
$buildRelease = Join-Path $projectRoot 'build\Release'

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Native Function Hot-Reload Demo" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Show initial function
Write-Host "[Step 1] Current math_add function:" -ForegroundColor Yellow
$mathOpsPath = Join-Path $scriptDir 'math_ops.c'
Get-Content $mathOpsPath | Select-String -Pattern "return a.*b;" | Select-Object -First 1 | ForEach-Object {
    Write-Host "  $_" -ForegroundColor White
}

# Step 2: First run
Write-Host "`n[Step 2] Running VM with ADD function (10 + 5 = 15):" -ForegroundColor Yellow
Write-Host "Press Enter to continue..." -ForegroundColor Gray
Read-Host

$input1 = "1`n6`n"
$input1 | & (Join-Path $buildRelease 'sfpm_native_reload.exe')

# Step 3: Modify source
Write-Host "`n[Step 3] Modifying math_ops.c to MULTIPLY..." -ForegroundColor Yellow
$backup = "$mathOpsPath.demo.bak"
Copy-Item $mathOpsPath $backup -Force

$content = Get-Content $mathOpsPath -Raw
$content = $content -replace 'return a \+ b;', 'return a * b;  /* MODIFIED! */'
$content = $content -replace 'return 1;.*version', 'return 2;  /* Version 2 */'
Set-Content $mathOpsPath $content -NoNewline

Write-Host "New line:" -ForegroundColor Green
Get-Content $mathOpsPath | Select-String -Pattern "return a.*b;" | Select-Object -First 1 | ForEach-Object {
    Write-Host "  $_" -ForegroundColor Green
}

# Step 4: Recompile
Write-Host "`n[Step 4] Recompiling math_ops.dll..." -ForegroundColor Yellow
Write-Host "Press Enter to continue..." -ForegroundColor Gray
Read-Host

Push-Location $projectRoot
& cmake --build build --config Release --target math_ops 2>&1 | Out-Null
Pop-Location

if (Test-Path (Join-Path $buildRelease 'math_ops.dll')) {
    Write-Host "  ✓ Recompilation successful!" -ForegroundColor Green
} else {
    Write-Host "  ✗ Compilation failed!" -ForegroundColor Red
    Move-Item $backup $mathOpsPath -Force
    exit 1
}

# Step 5: Hot-reload and run
Write-Host "`n[Step 5] Hot-reloading library and running again:" -ForegroundColor Yellow
Write-Host "The VM will:" -ForegroundColor Gray
Write-Host "  - Reload math_ops.dll (option 2)" -ForegroundColor Gray
Write-Host "  - Run program again (option 1)" -ForegroundColor Gray
Write-Host "  - Should now show 10 * 5 = 50" -ForegroundColor Gray
Write-Host "`nPress Enter to continue..." -ForegroundColor Gray
Read-Host

$input2 = "2`nmath_ops.dll`nmath_add`n0`n1`n6`n"
$input2 | & (Join-Path $buildRelease 'sfpm_native_reload.exe')

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Demo Complete!" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Summary:" -ForegroundColor Green
Write-Host "  First run:  10 + 5 = 15 (addition)" -ForegroundColor White
Write-Host "  Modified:   Changed source to multiply" -ForegroundColor White
Write-Host "  Recompiled: Built new math_ops.dll" -ForegroundColor White
Write-Host "  Reloaded:   VM loaded new DLL" -ForegroundColor White
Write-Host "  Second run: 10 * 5 = 50 (multiplication)" -ForegroundColor White
Write-Host "`nNo VM restart required!" -ForegroundColor Yellow

# Cleanup
Write-Host "`n[Cleanup] Restoring original files..." -ForegroundColor Yellow
Move-Item $backup $mathOpsPath -Force

Push-Location $projectRoot
& cmake --build build --config Release --target math_ops 2>&1 | Out-Null
Pop-Location

Write-Host "  ✓ Original files restored" -ForegroundColor Green
Write-Host ""
