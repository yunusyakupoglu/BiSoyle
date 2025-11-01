# BiSoyle POS - Desktop Application

Bu proje Angular + Electron kullanarak masaüstü uygulaması olarak çalışır.

## Gereksinimler

- Node.js (v18 veya üstü)
- npm veya yarn

## Kurulum

1. Bağımlılıkları yükleyin:
```bash
npm install
```

## Development Modunda Çalıştırma

Electron uygulamasını development modunda çalıştırmak için:

```bash
npm run electron:dev
```

Bu komut:
- Angular dev server'ı başlatır (http://localhost:4200)
- Electron penceresini açar ve localhost'tan yükler
- Hot reload desteği sunar

## Production Build

Windows için installer oluşturmak:

```bash
npm run electron:build:win
```

Bu komut:
- Angular uygulamasını production modda derler
- Electron Builder ile Windows installer (.exe) oluşturur
- `release/` klasöründe installer'ı bulabilirsiniz

## Klasör Yapısı

```
frontend/Admin/
├── electron/           # Electron dosyaları
│   ├── main.js        # Ana Electron process
│   └── preload.js     # Preload script
├── src/               # Angular kaynak dosyaları
├── angular.json       # Angular config
├── electron-builder.yml # Electron Builder config
└── package.json       # Proje bağımlılıkları
```

## Özellikler

- ✅ Masaüstü uygulama olarak çalışır
- ✅ Windows, Linux, macOS desteği
- ✅ Auto-updater hazır
- ✅ Offline destek
- ✅ Native menüler ve kısayollar
- ✅ System tray desteği (istediğinizde eklenebilir)

## Notlar

- Development modunda `http://localhost:4200` bekler, bu yüzden backend servisleri çalışıyor olmalı
- Production build'de `dist/reback/` klasöründen statik dosyaları yükler
- Electron uygulaması backend servisleriyle normal HTTP istekleri yapar

## Sorun Giderme

**Electron penceresi açılmıyor:**
- Önce `npm run start` ile Angular dev server'ın çalıştığından emin olun

**Build hatası alıyorsanız:**
```bash
npm install --save-dev electron electron-builder
```

## Lisans

BiSoyle POS © 2024

