# BiSoyle - Start All Services Locally
# Bu script tüm servisleri local ortamda ayrı terminal penceresinde başlatır

Write-Host "🚀 BiSoyle servislerini başlatıyorum..." -ForegroundColor Green

# Gateway
Write-Host "Gateway başlatılıyor (Port 5000)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd gateway; dotnet run"

Start-Sleep -Seconds 2

# Receipt Service
Write-Host "Receipt Service başlatılıyor (Port 5001)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/receipt-service; dotnet run"

Start-Sleep -Seconds 2

# Product Service
Write-Host "Product Service başlatılıyor (Port 5002)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/product-service; dotnet run"

Start-Sleep -Seconds 2

# Transaction Service
Write-Host "Transaction Service başlatılıyor (Port 5003)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/transaction-service; dotnet run"

Start-Sleep -Seconds 2

# User Service
Write-Host "User Service başlatılıyor (Port 5004)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/user-service; dotnet run"

Start-Sleep -Seconds 2

# Frontend
Write-Host "Frontend başlatılıyor (Port 4200)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd frontend/Admin; npm start"

Write-Host ""
Write-Host "✅ Tüm servisler başlatıldı!" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Servis Portları:" -ForegroundColor Yellow
Write-Host "  - Gateway:             http://localhost:5000" -ForegroundColor White
Write-Host "  - Receipt Service:     http://localhost:5001" -ForegroundColor White
Write-Host "  - Product Service:     http://localhost:5002" -ForegroundColor White
Write-Host "  - Transaction Service: http://localhost:5003" -ForegroundColor White
Write-Host "  - User Service (JWT):  http://localhost:5004" -ForegroundColor Green
Write-Host "  - Frontend:            http://localhost:4200" -ForegroundColor White
Write-Host ""
Write-Host "📝 Swagger UI:" -ForegroundColor Yellow
Write-Host "  - Gateway:             http://localhost:5000/swagger" -ForegroundColor White
Write-Host "  - Receipt Service:     http://localhost:5001/swagger" -ForegroundColor White
Write-Host "  - Product Service:     http://localhost:5002/swagger" -ForegroundColor White
Write-Host "  - Transaction Service: http://localhost:5003/swagger" -ForegroundColor White
Write-Host "  - User Service:        http://localhost:5004/swagger" -ForegroundColor Green
Write-Host ""
Write-Host "👤 Test Kullanıcısı:" -ForegroundColor Yellow
Write-Host "  - Email:    admin@bisoyle.com" -ForegroundColor White
Write-Host "  - Password: admin123" -ForegroundColor White
Write-Host "  - Role:     Admin" -ForegroundColor White
Write-Host ""
Write-Host "⏹️  Servisleri durdurmak için ilgili terminal pencerelerini kapatın." -ForegroundColor Red

