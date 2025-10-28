# BiSoyle Servis Test Script
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BiSoyle Servisleri Test Ediliyor" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"

# Test Results
$testResults = @{
    Gateway = $false
    ReceiptService = $false
    ProductService = $false
    TransactionService = $false
}

# Test URLs
$gatewayUrl = "http://localhost:5000"
$receiptUrl = "http://localhost:5001"
$productUrl = "http://localhost:5002"
$transactionUrl = "http://localhost:5003"

Write-Host "1. Testing Product Service..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$productUrl/api/products" -Method GET -TimeoutSec 5 -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "   ✅ Product Service: OK" -ForegroundColor Green
        $testResults.ProductService = $true
        $products = $response.Content | ConvertFrom-Json
        Write-Host "   📦 Products Found: $($products.Count)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ Product Service: FAILED" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "2. Testing Transaction Service..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$transactionUrl/api/transactions" -Method GET -TimeoutSec 5 -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "   ✅ Transaction Service: OK" -ForegroundColor Green
        $testResults.TransactionService = $true
        $transactions = $response.Content | ConvertFrom-Json
        Write-Host "   💰 Transactions Found: $($transactions.Count)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ Transaction Service: FAILED" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "3. Testing Receipt Service..." -ForegroundColor Yellow
try {
    $receiptData = @{
        Items = @(
            @{
                urun_id = 1
                Product = "Test Product"
                Quantity = 2
                Price = 10.0
                Unit = "adet"
            }
        )
    } | ConvertTo-Json

    $response = Invoke-WebRequest -Uri "$receiptUrl/api/receipt/print" -Method POST -Body $receiptData -ContentType "application/json" -TimeoutSec 10 -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "   ✅ Receipt Service: OK" -ForegroundColor Green
        $testResults.ReceiptService = $true
        $result = $response.Content | ConvertFrom-Json
        Write-Host "   🧾 Receipt Created: $($result.islem_kodu)" -ForegroundColor Cyan
        Write-Host "   💵 Total Amount: $($result.toplam_tutar)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ Receipt Service: FAILED" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "4. Testing Gateway..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$gatewayUrl/api/v1/products" -Method GET -TimeoutSec 5 -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "   ✅ Gateway: OK" -ForegroundColor Green
        $testResults.Gateway = $true
    }
} catch {
    Write-Host "   ❌ Gateway: FAILED" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Test Sonuçları" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$totalTests = $testResults.Count
$passedTests = ($testResults.Values | Where-Object { $_ -eq $true }).Count
$successRate = [math]::Round(($passedTests / $totalTests) * 100)

Write-Host "Product Service:    $(if ($testResults.ProductService) { '✅ PASS' } else { '❌ FAIL' })" -ForegroundColor $(if ($testResults.ProductService) { 'Green' } else { 'Red' })
Write-Host "Transaction Service: $(if ($testResults.TransactionService) { '✅ PASS' } else { '❌ FAIL' })" -ForegroundColor $(if ($testResults.TransactionService) { 'Green' } else { 'Red' })
Write-Host "Receipt Service:   $(if ($testResults.ReceiptService) { '✅ PASS' } else { '❌ FAIL' })" -ForegroundColor $(if ($testResults.ReceiptService) { 'Green' } else { 'Red' })
Write-Host "Gateway:            $(if ($testResults.Gateway) { '✅ PASS' } else { '❌ FAIL' })" -ForegroundColor $(if ($testResults.Gateway) { 'Green' } else { 'Red' })

Write-Host ""
Write-Host "Başarı Oranı: $successRate%" -ForegroundColor $(if ($successRate -eq 100) { 'Green' } else { 'Yellow' })
Write-Host ""

if ($successRate -eq 100) {
    Write-Host "🎉 Tüm servisler çalışıyor!" -ForegroundColor Green
} else {
    Write-Host "⚠️ Bazı servisler çalışmıyor. Lütfen servisleri başlatın." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Servis başlatma komutları:" -ForegroundColor Cyan
    Write-Host "  cd gateway; dotnet run" -ForegroundColor White
    Write-Host "  cd services\receipt-service; dotnet run" -ForegroundColor White
    Write-Host "  cd services\product-service; dotnet run" -ForegroundColor White
    Write-Host "  cd services/transaction-service; dotnet run" -ForegroundColor White
}

Write-Host ""
