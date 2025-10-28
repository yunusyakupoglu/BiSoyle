import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from 'src/app/services/auth.service';

interface Islem {
  id: number;
  islem_kodu: string;
  urun_detaylari: string;
  toplam_tutar: number;
  created_at: string;
}

interface Urun {
  id: number;
  urun_adi: string;
  birim_fiyat: number;
  kategori_id: number;
  olcu_birimi_id: number;
}

interface OlcuBirimi {
  id: number;
  olcu_birimi_adi: string;
}

interface UrunDetayItem {
  urun_id: number;
  miktar: number;
  birim_fiyat: number;
}

@Component({
  selector: 'app-islemler',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './islemler.component.html'
})
export class IslemlerComponent implements OnInit {
  islemler: Islem[] = [];
  urunler: Urun[] = [];
  olcuBirimleri: OlcuBirimi[] = [];
  yukleniyor = false;
  
  // Form alanları
  islemKodu: string = '';
  seciliUrunler: UrunDetayItem[] = [];
  toplamTutar: number = 0;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.islemleriGetir();
    this.urunleriGetir();
    this.olcuBirimleriniGetir();
  }

  olcuBirimleriniGetir() {
    this.http.get<OlcuBirimi[]>('http://localhost:8000/olcu-birimleri', { headers: this.headers }).subscribe({
      next: (data) => this.olcuBirimleri = data,
      error: (err) => console.error('Olcu birimleri yüklenemedi:', err)
    });
  }

  get headers() {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  islemleriGetir() {
    this.yukleniyor = true;
    this.http.get<Islem[]>('http://localhost:8000/islemler', { headers: this.headers }).subscribe({
      next: (data) => {
        this.islemler = data;
        this.yukleniyor = false;
      },
      error: (err) => {
        console.error('İşlemler yüklenemedi:', err);
        this.yukleniyor = false;
      }
    });
  }

  urunleriGetir() {
    this.http.get<Urun[]>('http://localhost:8000/urunler', { headers: this.headers }).subscribe({
      next: (data) => {
        this.urunler = data;
      },
      error: (err) => console.error('Ürünler yüklenemedi:', err)
    });
  }

  urunEkle() {
    if (!this.seciliUrunler.length) {
      this.seciliUrunler.push({
        urun_id: 0,
        miktar: 1,
        birim_fiyat: 0
      });
    } else {
      this.seciliUrunler.push({
        urun_id: 0,
        miktar: 1,
        birim_fiyat: 0
      });
    }
    this.hesaplaToplamTutar();
  }

  generateIslemKodu(): string {
    const now = new Date();
    const random = Math.floor(Math.random() * 10000).toString().padStart(4, '0');
    const dateStr = now.getFullYear().toString() +
                     (now.getMonth() + 1).toString().padStart(2, '0') +
                     now.getDate().toString().padStart(2, '0');
    const timeStr = now.getHours().toString().padStart(2, '0') +
                    now.getMinutes().toString().padStart(2, '0') +
                    now.getSeconds().toString().padStart(2, '0');
    
    return `ISLEM-${dateStr}-${timeStr}-${random}`;
  }

  urunCikar(index: number) {
    this.seciliUrunler.splice(index, 1);
    this.hesaplaToplamTutar();
  }

  hesaplaToplamTutar() {
    this.toplamTutar = this.seciliUrunler.reduce((total, item) => {
      const birimFiyat = this.getUrunBirimFiyat(item.urun_id);
      return total + (birimFiyat * item.miktar);
    }, 0);
  }

  getUrunAdi(urunId: number): string {
    const urun = this.urunler.find(u => u.id === urunId);
    return urun ? urun.urun_adi : '-';
  }

  getUrunBirimFiyat(urunId: number): number {
    const urun = this.urunler.find(u => u.id === urunId);
    return urun ? urun.birim_fiyat : 0;
  }

  getUrunOlcuBirimi(urunId: number): string {
    const urun = this.urunler.find(u => u.id === urunId);
    if (!urun) return '';
    
    const olcu = this.olcuBirimleri.find(ob => ob.id === urun.olcu_birimi_id);
    return olcu ? olcu.olcu_birimi_adi : '';
  }

  onUrunSelected(index: number, event: any) {
    // Force change detection
    const selectedUrun = this.seciliUrunler[index];
    const selectedUrunId = parseInt(event.target.value);
    
    if (selectedUrun && selectedUrunId && selectedUrunId !== 0) {
      selectedUrun.urun_id = selectedUrunId;
      
      // Auto-fill birim fiyat from product
      const birimFiyat = this.getUrunBirimFiyat(selectedUrunId);
      selectedUrun.birim_fiyat = birimFiyat;
      
      this.hesaplaToplamTutar();
    }
  }

  islemEkle() {
    // Auto-generate islem kodu if empty
    if (!this.islemKodu) {
      this.islemKodu = this.generateIslemKodu();
    }

    if (!this.seciliUrunler.length) {
      alert('En az bir ürün gerekli');
      return;
    }

    // Kontrol: Tüm ürünlerde ürün seçilmeli ve miktar girilmeli
    const eksikUrunler = this.seciliUrunler.filter(u => !u.urun_id || !u.miktar);
    if (eksikUrunler.length > 0) {
      alert('Lütfen tüm ürünleri seçin ve miktar girin');
      return;
    }

    // Send urun_detaylari with actual birim_fiyat from products
    const urunDetaylari = this.seciliUrunler.map(item => ({
      urun_id: item.urun_id,
      miktar: item.miktar,
      birim_fiyat: this.getUrunBirimFiyat(item.urun_id)
    }));

    const yeniIslem = {
      islem_kodu: this.islemKodu,
      urun_detaylari: urunDetaylari,
      toplam_tutar: this.toplamTutar
    };

    this.http.post<Islem>('http://localhost:8000/islemler', yeniIslem, { headers: this.headers }).subscribe({
      next: () => {
        alert('İşlem eklendi');
        this.islemleriGetir();
        this.islemKodu = '';
        this.seciliUrunler = [];
        this.toplamTutar = 0;
      },
      error: (err) => {
        alert('İşlem eklenemedi: ' + (err.error?.detail || err.message));
        console.error('İşlem eklenemedi:', err);
      }
    });
  }

  islemSil(id: number) {
    if (confirm('Bu işlemi silmek istediğinizden emin misiniz?')) {
      this.http.delete(`http://localhost:8000/islemler/${id}`, { headers: this.headers }).subscribe({
        next: () => {
          alert('İşlem silindi');
          this.islemleriGetir();
        },
        error: (err) => {
          alert('İşlem silinemedi: ' + (err.error?.detail || err.message));
        }
      });
    }
  }

  parseUrunDetaylari(urunDetaylariStr: string): any[] {
    try {
      const parsed = JSON.parse(urunDetaylariStr);
      return Array.isArray(parsed) ? parsed : [];
    } catch (e) {
      return [];
    }
  }
}
