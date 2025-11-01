# BiSoyle - Local PostgreSQL Setup Script
# Bu script local PostgreSQL için veritabanlarını hazırlar

Write-Host "🗄️  BiSoyle - Local PostgreSQL Kurulum" -ForegroundColor Cyan
Write-Host ""

# PostgreSQL kurulu mu kontrol et
$pgVersion = psql --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ PostgreSQL kurulu değil!" -ForegroundColor Red
    Write-Host ""
    Write-Host "PostgreSQL'i indirmek için:" -ForegroundColor Yellow
    Write-Host "  https://www.postgresql.org/download/windows/" -ForegroundColor White
    Write-Host ""
    Write-Host "Veya winget ile:" -ForegroundColor Yellow
    Write-Host "  winget install PostgreSQL.PostgreSQL" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "✅ PostgreSQL kurulu: $pgVersion" -ForegroundColor Green
Write-Host ""

# Veritabanlarını oluştur
Write-Host "📊 Veritabanları oluşturuluyor..." -ForegroundColor Cyan

# PostgreSQL şifresini sor
$env:PGPASSWORD = Read-Host "PostgreSQL postgres kullanıcısının şifresini girin" -AsSecureString
$env:PGPASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($env:PGPASSWORD))

# Init script'i çalıştır
Write-Host "Veritabanları ve tablolar oluşturuluyor..." -ForegroundColor Yellow
psql -U postgres -f scripts/init-db.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Veritabanları başarıyla oluşturuldu!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Oluşturulan veritabanları:" -ForegroundColor Cyan
    Write-Host "  - bisoyle_receipt" -ForegroundColor White
    Write-Host "  - bisoyle_product (8 örnek ürün)" -ForegroundColor White
    Write-Host "  - bisoyle_transaction (3 örnek işlem)" -ForegroundColor White
    Write-Host ""
    Write-Host "🚀 Artık servisleri başlatabilirsiniz:" -ForegroundColor Green
    Write-Host "  .\start-local.ps1" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "❌ Hata! Veritabanları oluşturulamadı." -ForegroundColor Red
    Write-Host "Lütfen PostgreSQL şifrenizin doğru olduğundan emin olun." -ForegroundColor Yellow
}

# Şifreyi temizle
Remove-Item Env:\PGPASSWORD






