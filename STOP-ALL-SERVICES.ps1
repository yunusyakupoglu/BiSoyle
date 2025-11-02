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

# Stop Node.js processes (Angular dev server, Electron)
Write-Host "Stopping Node.js processes (Angular, Electron)..." -ForegroundColor Yellow
Get-Process -Name "node" -ErrorAction SilentlyContinue | Stop-Process -Force

# Stop Electron processes
Write-Host "Stopping Electron processes..." -ForegroundColor Yellow
Get-Process -Name "electron" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "BiSoyle.Receipt.Service" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "BiSoyle.Transaction.Service" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "BiSoyle.Product.Service" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "BiSoyle.User.Service" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "BiSoyle.Tenant.Service" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "BiSoyle.Gateway" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host ""
Write-Host "✅ All services stopped!" -ForegroundColor Green
Write-Host ""





