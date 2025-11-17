# Docker Açıldığında Otomatik Servis Başlatma

## 🚀 Kullanım

### Yöntem 1: Manuel Başlatma (Önerilen)
Docker Desktop'ı başlattıktan sonra, proje klasöründe `start-all-services.bat` dosyasını çift tıklayarak çalıştırın.

### Yöntem 2: PowerShell ile
```powershell
.\start-when-docker-ready.ps1
```

### Yöntem 3: Otomatik Başlatma (Windows Startup)

#### Seçenek A: Startup Klasörüne Kısayol Ekleme
1. `Win + R` tuşlarına basın
2. `shell:startup` yazın ve Enter'a basın
3. `start-all-services.bat` dosyasına sağ tıklayın → "Kısayol oluştur" seçin
4. Oluşan kısayolu Startup klasörüne kopyalayın

#### Seçenek B: Task Scheduler ile Otomatik Başlatma
1. Task Scheduler'ı açın (`Win + R` → `taskschd.msc`)
2. "Create Basic Task" seçin
3. İsim: "BiSoyle Auto Start"
4. Trigger: "When the computer starts"
5. Action: "Start a program"
6. Program: `powershell.exe`
7. Arguments: `-ExecutionPolicy Bypass -File "C:\Users\Lenovo\Desktop\BiSoyle\start-when-docker-ready.ps1"`
8. "Run whether user is logged on or not" seçeneğini işaretleyin
9. Finish

### Yöntem 4: Docker Desktop Startup Script
Docker Desktop'ın başlangıç scripti özelliğini kullanarak:
1. Docker Desktop'ı açın
2. Settings → General → "Use WSL 2 based engine" (isteğe bağlı)
3. Settings → Resources → Advanced → Startup script bölümüne ekleyin:
   ```powershell
   powershell -ExecutionPolicy Bypass -File "C:\Users\Lenovo\Desktop\BiSoyle\start-when-docker-ready.ps1"
   ```

## 📋 Script Özellikleri

- ✅ Docker'ın hazır olmasını otomatik bekler (60 saniye)
- ✅ PostgreSQL ve RabbitMQ'yu Docker Compose ile başlatır
- ✅ Tüm .NET servislerini ayrı pencerelerde başlatır
- ✅ Voice Service'i Python ile başlatır
- ✅ Frontend'i Angular ile başlatır
- ✅ Servislerin başlamasını sırayla kontrol eder

## 🔧 Sorun Giderme

### Docker hazır olmuyor
- Docker Desktop'ın tamamen başlamasını bekleyin
- `docker info` komutu ile Docker'ın çalıştığını kontrol edin

### Port çakışması
- İlgili portu kullanan başka bir uygulama olup olmadığını kontrol edin
- Script eski süreçleri temizlemeye çalışır, ama manuel kontrol gerekebilir

### Servisler başlamıyor
- PowerShell pencerelerini kontrol edin (hata mesajları görünecektir)
- Her servisin kendi klasöründe `dotnet run` komutunu manuel çalıştırarak test edin

## 📝 Notlar

- Script tüm servisleri **minimize edilmiş** pencerelerde açar
- Servislerin tam olarak başlaması 10-15 saniye sürebilir
- Frontend otomatik olarak tarayıcıda açılacaktır




