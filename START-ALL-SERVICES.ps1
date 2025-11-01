# BiSoyle - Tüm Servisleri Local Başlatma Script'i
# BY HOSTEAGLE INFORMATION TECHNOLOGIES

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "De' Bakiim - BiSoyle POS System" -ForegroundColor Yellow
Write-Host "Local Development Mode" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# PostgreSQL ve RabbitMQ'yu Docker'da çalıştır
Write-Host "[1/10] Starting PostgreSQL & RabbitMQ (Docker)..." -ForegroundColor Yellow
docker-compose up -d postgres rabbitmq
Start-Sleep -Seconds 5

# User Service
Write-Host "[2/10] Starting User Service (Port 5004)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/user-service; Write-Host 'USER SERVICE - Port 5004' -ForegroundColor Green; dotnet run"

# Tenant Service
Write-Host "[3/10] Starting Tenant Service (Port 5005)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/tenant-service; Write-Host 'TENANT SERVICE - Port 5005' -ForegroundColor Cyan; dotnet run"

# Product Service
Write-Host "[4/10] Starting Product Service (Port 5002)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/product-service; Write-Host 'PRODUCT SERVICE - Port 5002' -ForegroundColor Blue; dotnet run"

# Transaction Service
Write-Host "[5/10] Starting Transaction Service (Port 5003)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/transaction-service; Write-Host 'TRANSACTION SERVICE - Port 5003' -ForegroundColor Magenta; dotnet run"

# Receipt Service
Write-Host "[6/10] Starting Receipt Service (Port 5001)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/receipt-service; Write-Host 'RECEIPT SERVICE - Port 5001' -ForegroundColor Red; dotnet run"

Write-Host "[7/10] Waiting for services to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Gateway
Write-Host "[8/10] Starting API Gateway (Port 5000)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd gateway; Write-Host 'API GATEWAY - Port 5000' -ForegroundColor DarkYellow; dotnet run"

# Voice Service
Write-Host "[9/10] Starting Voice Service (Port 8765/8766)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/voice-service; Write-Host 'VOICE SERVICE - Port 8765/8766' -ForegroundColor DarkCyan; python main.py"

Start-Sleep -Seconds 5

# Frontend Admin Panel
Write-Host "[10/10] Starting Frontend Admin Panel (Port 4200)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd frontend/Admin; Write-Host 'FRONTEND ADMIN - Port 4200' -ForegroundColor Magenta; npm start"

Write-Host "All services starting..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "✅ ALL SERVICES STARTED!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📌 Services Running:" -ForegroundColor Yellow
Write-Host "   🐘 PostgreSQL       : localhost:5433" -ForegroundColor White
Write-Host "   🐰 RabbitMQ         : localhost:15672 (admin/admin123)" -ForegroundColor White
Write-Host "   🌐 API Gateway      : http://localhost:5000" -ForegroundColor White
Write-Host "   👤 User Service     : http://localhost:5004" -ForegroundColor White
Write-Host "   🏢 Tenant Service   : http://localhost:5005" -ForegroundColor White
Write-Host "   📦 Product Service  : http://localhost:5002" -ForegroundColor White
Write-Host "   💳 Transaction Svc  : http://localhost:5003" -ForegroundColor White
Write-Host "   🧾 Receipt Service  : http://localhost:5001" -ForegroundColor White
Write-Host "   🎤 Voice Service    : http://localhost:8765, ws://localhost:8766" -ForegroundColor White
Write-Host ""
Write-Host "🚀 Frontend Admin Panel: http://localhost:4200" -ForegroundColor Cyan
Write-Host ""
Write-Host "✨ Bluetooth Device Testing: NOW AVAILABLE!" -ForegroundColor Green
Write-Host "   Go to Cihazlar page and test your Bluetooth devices!" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop this window (services will continue)" -ForegroundColor DarkGray
Write-Host ""

# Keep script running
while ($true) {
    Start-Sleep -Seconds 60
}




