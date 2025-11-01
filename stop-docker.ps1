# BiSoyle - Stop Docker Compose
# Bu script Docker Compose ile başlatılan tüm servisleri durdurur

Write-Host "🛑 BiSoyle Docker servisleri durduruluyor..." -ForegroundColor Yellow
Write-Host ""

docker-compose down

Write-Host ""
Write-Host "✅ Tüm servisler durduruldu!" -ForegroundColor Green






