# Script to kill all Chrome and ChromeDriver processes
# Run this if you have orphaned processes in Task Manager

Write-Host "Killing all Chrome and ChromeDriver processes..." -ForegroundColor Yellow

# Kill all Chrome processes
$chromeProcesses = Get-Process chrome -ErrorAction SilentlyContinue
if ($chromeProcesses) {
    $chromeProcesses | Stop-Process -Force
    Write-Host "Killed $($chromeProcesses.Count) Chrome processes" -ForegroundColor Green
} else {
    Write-Host "No Chrome processes found" -ForegroundColor Gray
}

# Kill all ChromeDriver processes
$chromedriverProcesses = Get-Process chromedriver -ErrorAction SilentlyContinue
if ($chromedriverProcesses) {
    $chromedriverProcesses | Stop-Process -Force
    Write-Host "Killed $($chromedriverProcesses.Count) ChromeDriver processes" -ForegroundColor Green
} else {
    Write-Host "No ChromeDriver processes found" -ForegroundColor Gray
}

Write-Host "Cleanup complete!" -ForegroundColor Green
