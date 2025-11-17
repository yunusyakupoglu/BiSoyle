# Docker Komutları - Voice Service

## Logları Görüntüleme

### Son 100 satır log
```bash
docker logs bisoyle-voice-service-1 --tail 100
```

### Canlı log takibi (Ctrl+C ile durdur)
```bash
docker logs bisoyle-voice-service-1 -f
```

### Enroll işlemi loglarını filtrele
```bash
docker logs bisoyle-voice-service-1 --tail 200 | grep -i "enroll\|file\|saved\|processing"
```

## Kaydedilen Dosyaları Kontrol Etme

### Container içindeki dosyaları listele
```bash
docker exec bisoyle-voice-service-1 ls -lah /app/models/speakers/
```

### Container içine gir
```bash
docker exec -it bisoyle-voice-service-1 /bin/bash
```

## Container'ı Yeniden Build Etme

### 1. Container'ı durdur
```bash
docker stop bisoyle-voice-service-1
```

### 2. Container'ı sil
```bash
docker rm bisoyle-voice-service-1
```

### 3. Image'ı yeniden build et
```bash
cd services/voice-service
docker build -t bisoyle-voice-service .
```

### 4. Container'ı başlat (docker-compose kullanıyorsanız)
```bash
# Ana dizinde
docker-compose up -d voice-service
```

VEYA manuel olarak:
```bash
docker run -d --name bisoyle-voice-service-1 -p 8765:8765 -p 8766:8766 bisoyle-voice-service
```

## Hızlı Yeniden Başlatma (Kod değişiklikleri için)

```bash
# Container'ı yeniden başlat (kod değişiklikleri için rebuild gerekir)
docker-compose restart voice-service

# Veya rebuild ile
docker-compose up -d --build voice-service
```

