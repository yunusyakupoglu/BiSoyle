# BiSoyle - Tüm Servisleri Local Başlatma Script'i

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "BiSoyle POS System" -ForegroundColor Yellow
Write-Host "Starting all services..." -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# PostgreSQL ve RabbitMQ
Write-Host "[1/10] Starting PostgreSQL & RabbitMQ..." -ForegroundColor Yellow
docker-compose up -d postgres rabbitmq
Start-Sleep -Seconds 5

# User Service
Write-Host "[2/10] Starting User Service (Port 5004)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd c:\Users\Lenovo\Desktop\BiSoyle\services\user-service; Write-Host 'USER SERVICE - Port 5004' -ForegroundColor Green; dotnet run"

# Tenant Service
Write-Host "[3/10] Starting Tenant Service (Port 5005)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd c:\Users\Lenovo\Desktop\BiSoyle\services\tenant-service; Write-Host 'TENANT SERVICE - Port 5005' -ForegroundColor Cyan; dotnet run"

# Product Service
Write-Host "[4/10] Starting Product Service (Port 5002)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd c:\Users\Lenovo\Desktop\BiSoyle\services\product-service; Write-Host 'PRODUCT SERVICE - Port 5002' -ForegroundColor Blue; dotnet run"

# Transaction Service
Write-Host "[5/10] Starting Transaction Service (Port 5003)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd c:\Users\Lenovo\Desktop\BiSoyle\services\transaction-service; Write-Host 'TRANSACTION SERVICE - Port 5003' -ForegroundColor Magenta; dotnet run"

# Receipt Service
Write-Host "[6/10] Starting Receipt Service (Port 5001)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd c:\Users\Lenovo\Desktop\BiSoyle\services\receipt-service; Write-Host 'RECEIPT SERVICE - Port 5001' -ForegroundColor Red; dotnet run"

Write-Host "[7/10] Waiting for backend services..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Gateway
Write-Host "[8/10] Starting API Gateway (Port 5000)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd c:\Users\Lenovo\Desktop\BiSoyle\gateway; Write-Host 'API GATEWAY - Port 5000' -ForegroundColor DarkYellow; dotnet run"

# Voice Service
Write-Host "[9/10] Starting Voice Service (Port 8765/8766)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd c:\Users\Lenovo\Desktop\BiSoyle\services\voice-service; Write-Host 'VOICE SERVICE - Port 8765/8766' -ForegroundColor DarkCyan; python main.py"

Start-Sleep -Seconds 5

# Frontend Angular Dev Server
Write-Host "[10/12] Starting Angular Dev Server (Port 4200)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd c:\Users\Lenovo\Desktop\BiSoyle\frontend\Admin; Write-Host 'ANGULAR DEV SERVER - Port 4200' -ForegroundColor Magenta; npx ng serve"

Start-Sleep -Seconds 10

# Electron Desktop App
Write-Host "[11/12] Starting Electron Desktop App..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd c:\Users\Lenovo\Desktop\BiSoyle\frontend\Admin; Write-Host 'ELECTRON DESKTOP APP' -ForegroundColor Green; Start-Sleep -Seconds 10; npx electron ."

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "ALL SERVICES STARTED!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Services:" -ForegroundColor Yellow
Write-Host "  PostgreSQL    : localhost:5433" -ForegroundColor White
Write-Host "  RabbitMQ      : localhost:15672" -ForegroundColor White
Write-Host "  API Gateway   : http://localhost:5000" -ForegroundColor White
Write-Host "  User Service  : http://localhost:5004" -ForegroundColor White
Write-Host "  Tenant Svc    : http://localhost:5005" -ForegroundColor White
Write-Host "  Product Svc   : http://localhost:5002" -ForegroundColor White
Write-Host "  Transaction   : http://localhost:5003" -ForegroundColor White
Write-Host "  Receipt Svc   : http://localhost:5001" -ForegroundColor White
Write-Host "  Voice Service : http://localhost:8765" -ForegroundColor White
Write-Host "  Angular Dev   : http://localhost:4200" -ForegroundColor White
Write-Host ""
Write-Host "Electron Desktop App: Başlatılıyor..." -ForegroundColor Green
Write-Host ""
