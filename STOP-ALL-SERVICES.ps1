# BiSoyle - Tüm Servisleri Durdurma Script'i

Write-Host "=====================================" -ForegroundColor Red
Write-Host "Stopping All Services..." -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Red
Write-Host ""

# Stop Docker containers
Write-Host "Stopping Docker containers..." -ForegroundColor Yellow
docker-compose down

# Stop all dotnet processes
Write-Host "Stopping .NET services..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

# Stop Python processes
Write-Host "Stopping Python services..." -ForegroundColor Yellow
Get-Process -Name "python" -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*voice-service*" } | Stop-Process -Force

Write-Host ""
Write-Host "✅ All services stopped!" -ForegroundColor Green
Write-Host ""





