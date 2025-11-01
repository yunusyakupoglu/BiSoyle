# BiSoyle - Start with Docker Compose
# Bu script tüm servisleri Docker ile başlatır

Write-Host "🐳 BiSoyle Docker Compose ile başlatılıyor..." -ForegroundColor Green
Write-Host ""

# Check if Docker is running
$dockerRunning = docker info 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker çalışmıyor! Lütfen Docker Desktop'ı başlatın." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Docker aktif" -ForegroundColor Green
Write-Host ""

# Build and start services
Write-Host "🏗️  Servisler build ediliyor ve başlatılıyor..." -ForegroundColor Cyan
docker-compose up --build

Write-Host ""
Write-Host "⏹️  Servisleri durdurmak için Ctrl+C'ye basın" -ForegroundColor Yellow






