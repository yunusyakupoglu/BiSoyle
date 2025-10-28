# Bi'Soyle - Tüm Servisleri Başlat
# Bu script tüm mikroservisleri ve frontend'i başlatır

Write-Host "================================" -ForegroundColor Cyan
Write-Host "  Bi'Soyle - Tüm Servisleri Başlat" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# Port kontrolü
function Test-Port {
    param([int]$Port)
    $connection = Test-NetConnection -ComputerName localhost -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue
    return $connection
}

Write-Host "1. Port kontrolü yapılıyor..." -ForegroundColor Yellow

$ports = @(5000, 5001, 5002, 5003, 4200)
foreach ($port in $ports) {
    if (Test-Port -Port $port) {
        Write-Host "  ⚠️  Port $port kullanımda!" -ForegroundColor Red
        Write-Host "  Lütfen bu port'taki servisi durdurun ve tekrar deneyin." -ForegroundColor Yellow
        Read-Host "Devam etmek için Enter'a bas"
    } else {
        Write-Host "  ✓ Port $port müsait" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "2. Servisler başlatılıyor..." -ForegroundColor Yellow
Write-Host ""

# Gateway
Write-Host "Gateway başlatılıyor (Port 5000)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\Users\Lenovo\Desktop\BiSoyle\gateway; dotnet run" -WindowStyle Minimized
Start-Sleep -Seconds 3

# Receipt Service
Write-Host "Receipt Service başlatılıyor (Port 5001)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\Users\Lenovo\Desktop\BiSoyle\services\receipt-service; dotnet run" -WindowStyle Minimized
Start-Sleep -Seconds 2

# Product Service
Write-Host "Product Service başlatılıyor (Port 5002)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\Users\Lenovo\Desktop\BiSoyle\services\product-service; dotnet run" -WindowStyle Minimized
Start-Sleep -Seconds 2

# Transaction Service
Write-Host "Transaction Service başlatılıyor (Port 5003)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\Users\Lenovo\Desktop\BiSoyle\services\transaction-service; dotnet run" -WindowStyle Minimized
Start-Sleep -Seconds 2

# Frontend
Write-Host "Frontend başlatılıyor (Port 4200)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\Users\Lenovo\Desktop\BiSoyle\frontend\Admin; npm start" -WindowStyle Minimized

Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "  Tüm Servisler Başlatıldı!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""
Write-Host "API Gateway:  http://localhost:5000" -ForegroundColor Cyan
Write-Host "Receipt API:  http://localhost:5001" -ForegroundColor Cyan
Write-Host "Product API:  http://localhost:5002" -ForegroundColor Cyan
Write-Host "Transaction:  http://localhost:5003" -ForegroundColor Cyan
Write-Host "Frontend:     http://localhost:4200" -ForegroundColor Cyan
Write-Host ""
Write-Host "Swagger:      http://localhost:5000/swagger" -ForegroundColor Yellow
Write-Host ""
Write-Host "Servisleri durdurmak için pencereleri kapatın." -ForegroundColor Yellow
Write-Host ""
