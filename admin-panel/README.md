# BiSoyle Service Manager

Windows Forms tabanlı servis yönetim arayüzü - WHM/cPanel benzeri kullanım kolaylığı.

## Özellikler

- ✅ **8 Servisi Tek Ekrandan Yönetme**
  - API Gateway (5000)
  - Receipt Service (5001)
  - Product Service (5002)
  - Transaction Service (5003)
  - User Service (5004)
  - Tenant Service (5005)
  - Voice Service (8765/8766)
  - Frontend (4200)

- ✅ **Gerçek Zamanlı Durum Takibi**
  - 5 saniyede bir otomatik kontrol
  - Running/Stopped durumu renkli göstergelerle

- ✅ **Kullanıcı Dostu Arayüz**
  - Modern Windows Forms tasarımı
  - Renkli durum göstergeleri
  - Detaylı servis logları

- ✅ **Tek Tık İşlemler**
  - Start All: Tüm servisleri başlatır
  - Stop All: Tüm servisleri durdurur
  - Refresh: Durumları yeniler
  - Open Frontend: Tarayıcıda frontend'i açar

## Kullanım

### Çalıştırma

```bash
cd admin-panel/BiSoyleAdminGUI
dotnet run
```

### Derleme

```bash
dotnet build
```

### Tek Dosya Olarak Yayınlama

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

Çıktı: `bin/Release/net8.0-windows/win-x64/publish/BiSoyleAdmin.exe`

## Servis Başlatma/Durdurma

### Otomatik (Start All)
1. **▶ Start All** butonuna tıklayın
2. Tüm servisler sırasıyla başlatılır
3. Her servisin durumu otomatik güncellenir

### Manuel
Her servis için:
- **▶ (Yeşil)** buton: Servisi başlat
- **■ (Kırmızı)** buton: Servisi durdur

## Durum Göstergeleri

| Durum | Gösterge | Anlamı |
|-------|----------|--------|
| Running | ● Running (Yeşil, Kalın) | Servis çalışıyor |
| Stopped | ○ Stopped (Kırmızı, Normal) | Servis durmuş |

## Log Sistemi

- Tüm servis başlatma/durdurma işlemleri loglanır
- Renkli log mesajları:
  - 🔵 Mavi: Başlatma işlemleri
  - 🟢 Yeşil: Başarılı işlemler
  - 🔴 Kırmızı: Hata mesajları
  - 🟠 Turuncu: Durdurma işlemleri

## Gereksinimler

- .NET 8.0 Runtime
- Windows 10/11
- PowerShell 5.0+
- Tüm backend servislerin yoluna erişim

## Teknik Detaylar

### Mimari
- **Framework**: Windows Forms (.NET 8.0)
- **UI**: Modern Windows Forms Design
- **Monitoring**: Timer-based port checking
- **Process Management**: PowerShell script execution

### Servis Başlatma
```csharp
StartService("SERVICE_NAME", port, @"relative\path", "command")
```

Örnek:
```csharp
StartService("GATEWAY", 5000, @"gateway", "dotnet run")
```

### Durum Kontrolü
```csharp
IsPortListening(port) // Returns bool
```

`netstat` komutunu kullanarak port dinleme durumunu kontrol eder.

## Sorun Giderme

### Servisler Başlatılamıyor
1. Gerekli .NET SDK'nın yüklü olduğunu kontrol edin
2. Servis dizinlerinin doğru olduğunu kontrol edin
3. Portların meşgul olmadığını kontrol edin

### UI Güncellenmiyor
1. **Refresh** butonuna tıklayın
2. Uygulamayı yeniden başlatın

### Frontend Açılmıyor
1. Frontend servisinin çalıştığından emin olun
2. Tarayıcıda `http://localhost:4200` adresini kontrol edin

## Lisans

Bu proje HostEagle Information Technologies tarafından geliştirilmiştir.

## Destek

Sorularınız için: info@hosteagle.com

