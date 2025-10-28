# 🚀 Bi'Soyle - Hızlı Başlangıç

## 📋 Gereksinimler

- .NET 8.0 SDK
- Python 3.11+
- Node.js 18+ (frontend için)

## 🏃 Hızlı Başlatma

### 1. Tüm Servisleri Başlat

```powershell
.\Start-All.ps1
```

Bu komut 5 ayrı PowerShell penceresinde servisleri başlatır:
- Gateway (Port 5000)
- Receipt Service (Port 5001)
- Product Service (Port 5002)
- Transaction Service (Port 5003)
- Voice Service (Port 8765)

### 2. Manuel Başlatma

Eğer PowerShell scripti çalışmazsa:

```powershell
# Terminal 1: Gateway
cd gateway
dotnet run

# Terminal 2: Receipt Service
cd services/receipt-service
dotnet run

# Terminal 3: Product Service
cd services/product-service
dotnet run

# Terminal 4: Transaction Service
cd services/transaction-service
dotnet run

# Terminal 5: Voice Service
cd services/voice-service
python main.py
```

### 3. Frontend

```powershell
cd frontend
npm install
npm start
```

## ✅ Test

### Endpoint'leri Test Et

```powershell
# Products
curl http://localhost:5002/api/products

# Gateway üzerinden
curl http://localhost:5000/api/v1/products
```

### Voice Service

```javascript
const ws = new WebSocket('ws://localhost:8765');
ws.onmessage = (event) => {
    console.log(JSON.parse(event.data));
};
```

## 🎯 Sesli Komutlar

1. Mikrofon butonuna tıkla
2. "fiş başlat" de
3. "3 adet çikolatalı kruvasan" de
4. "yarım kilo cevizli baklava" de
5. "fiş yazdır" de

**Sonuç**: PDF oluşturulur ve işlem kaydedilir (~500ms)

## 📊 Performans

- ✅ 6x daha hızlı (500ms vs 3 saniye)
- ✅ Paralel işleme
- ✅ Scalable yapı
- ✅ Fault tolerant

## 🐛 Sorun Giderme

### Port Kullanımda Hatası

```powershell
# Hangi process port kullanıyor?
netstat -ano | findstr :5000
```

### Servis Başlamıyor

```powershell
# .NET SDK kontrolü
dotnet --version

# Python kontrolü
python --version
```

## 📝 Sonraki Adımlar

1. ✅ Servisler çalışıyor
2. ⏳ Frontend'i güncelle
3. ⏳ RabbitMQ ekle
4. ⏳ SignalR hub'ları ekle
5. ⏳ Database (PostgreSQL)





