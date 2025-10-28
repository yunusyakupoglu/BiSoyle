# Bi'Soyle Frontend

Angular 18 frontend for Bi'Soyle mikroservis sistemi.

## 🚀 Quick Start

```bash
cd frontend
npm install
npm start
```

## 🔧 Configuration

### API Endpoints

Backend services configuration in `src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  gatewayUrl: 'http://localhost:5000',
  receiptUrl: 'http://localhost:5001',
  productUrl: 'http://localhost:5002',
  transactionUrl: 'http://localhost:5003',
  voiceUrl: 'ws://localhost:8765'
};
```

### SignalR Connection

Real-time updates for receipt processing:

```typescript
import { HubConnectionBuilder } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('http://localhost:5001/hub/receipt')
  .build();

connection.on('receiptComplete', (islemKodu, pdfPath) => {
  // PDF hazır, göster
});
```

### Voice Commands

Sesli komutlar Angular topbar component'inde:

```typescript
// Komutlar:
- "fiş başlat" - Yeni fiş
- "3 adet çikolatalı kruvasan" - Ürün ekle
- "yarım kilo cevizli baklava" - Ağırlıklı ürün
- "fiş yazdır" - Fişi yazdır ve kaydet
```

## 📝 Notlar

- Voice Service için WebSocket kullanılır
- SignalR ile real-time bildirimler
- Gateway üzerinden tüm API istekleri
- Fast & responsive (<500ms)





