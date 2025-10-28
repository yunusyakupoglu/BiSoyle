# Frontend Güncelleme Rehberi

## 🔄 Mevcut Frontend'i Bi'Soyle'a Uyarla

### 1. Frontend'i Kopyala

```powershell
# Mevcut frontend'i yeni projeye kopyala
xcopy /E /I "C:\Users\Lenovo\Desktop\VoiceAI\src\Reback-Angular_v1.0\Admin" "C:\Users\Lenovo\Desktop\BiSoyle\frontend"
```

### 2. Environment Dosyasını Güncelle

`frontend/src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  gatewayUrl: 'http://localhost:5000',        // Gateway
  receiptUrl: 'http://localhost:5001',          // Receipt Service
  productUrl: 'http://localhost:5002',          // Product Service
  transactionUrl: 'http://localhost:5003',      // Transaction Service
  voiceUrl: 'ws://localhost:8765'               // Voice Service (WebSocket)
};

// SignalR Hub
export const signalRConfig = {
  receiptHub: 'http://localhost:5001/hub/receipt',
  autoReconnect: true
};
```

### 3. Topbar Component'i Güncelle

`frontend/src/app/layouts/topbar/topbar.component.ts`:

```typescript
// Gateway üzerinden istek gönder
async printReceipt(items: any[]): Promise<void> {
  try {
    // Ürün fiyatları - Gateway üzerinden
    const urunler = await fetch(`${environment.gatewayUrl}/api/v1/products`, {
      headers: { 'Authorization': `Bearer ${this.authService.getToken()}` }
    }).then(r => r.json());
    
    // Enrich items
    const enrichedItems = items.map((item: any) => {
      const matchedUrun = urunler.find((u: any) => 
        u.urun_adi.toLowerCase().includes(item.product.toLowerCase())
      );
      return {
        quantity: item.quantity,
        unit: item.unit,
        product: item.product,
        price: matchedUrun ? matchedUrun.birim_fiyat : 0,
        urun_id: matchedUrun ? matchedUrun.id : 0
      };
    });

    // Fiş yazdır - Gateway üzerinden
    const response = await fetch(`${environment.gatewayUrl}/api/v1/receipt/print`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.authService.getToken()}`
      },
      body: JSON.stringify({ items: enrichedItems })
    });

    const data = await response.json();
    
    if (data.ok && data.type === 'pdf') {
      // PDF'i aç
      window.open(`${environment.gatewayUrl}/${data.path}`, '_blank');
    }
  } catch (err) {
    console.error('Yazdırma hatası:', err);
  }
}
```

### 4. SignalR Entegrasyonu

`frontend/src/app/services/receipt-hub.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ReceiptHubService {
  private hubConnection: HubConnection | null = null;

  constructor() {}

  startConnection(): void {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.receiptUrl}/hub/receipt`)
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('SignalR: Connected'))
      .catch(err => console.error('SignalR: Connection error', err));

    // Listen for receipt completion
    this.hubConnection.on('receiptComplete', (islemKodu, pdfPath) => {
      console.log(`Receipt complete: ${islemKodu}`);
      // Auto-open PDF
      window.open(`${environment.receiptUrl}/${pdfPath}`, '_blank');
    });
  }

  stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      console.log('SignalR: Disconnected');
    }
  }
}
```

### 5. Proxy Config (Opsiyonel)

`frontend/proxy.conf.json`:

```json
{
  "/api/*": {
    "target": "http://localhost:5000",
    "secure": false,
    "changeOrigin": true
  }
}
```

### 6. Angular.json Güncelle

```json
{
  "serve": {
    "proxyConfig": "proxy.conf.json"
  }
}
```

## ✅ Test

```powershell
cd frontend
npm install
npm start
```

http://localhost:4200 adresinde test edin.

## 🎯 Özellikler

- ✅ Gateway üzerinden tüm API istekleri
- ✅ SignalR ile real-time bildirimler
- ✅ WebSocket ile speaker ID
- ✅ Hızlı response (<500ms)
- ✅ Paralel işleme





