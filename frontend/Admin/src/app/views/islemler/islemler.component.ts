import { Component, OnInit, OnDestroy, CUSTOM_ELEMENTS_SCHEMA, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from 'src/app/services/auth.service';
import { TransactionRefreshService } from 'src/app/services/transaction-refresh.service';
import { Subscription } from 'rxjs';
import { environment } from 'src/environments/environment';

interface Islem {
  id: number;
  islemKodu?: string;
  islem_kodu?: string; // Backward compatibility
  islemTipi?: string;
  urun_detaylari?: string;
  items?: any[];
  toplamTutar?: number;
  toplam_tutar?: number; // Backward compatibility
  olusturmaTarihi?: string;
  created_at?: string; // Backward compatibility
}

interface Urun {
  id: number;
  urunAdi: string;
  urun_adi?: string; // Backward compatibility
  birimFiyat: number;
  birim_fiyat?: number; // Backward compatibility
  kategoriId?: number;
  kategori_id?: number; // Backward compatibility
  olcuBirimi?: string;
  olcu_birimi?: string; // Backward compatibility
}

interface OlcuBirimi {
  id: number;
  birimAdi: string;
  birim_adi?: string; // Backward compatibility
  kisaltma?: string;
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
export class IslemlerComponent implements OnInit, OnDestroy {
  islemler: Islem[] = [];
  urunler: Urun[] = [];
  olcuBirimleri: OlcuBirimi[] = [];
  yukleniyor = false;
  
  // Form alanları
  islemKodu: string = '';
  islemTipi: string = 'SATIS'; // Varsayılan: SATIS
  seciliUrunler: UrunDetayItem[] = [];
  toplamTutar: number = 0;
  
  private refreshSubscription?: Subscription;

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private transactionRefreshService: TransactionRefreshService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.islemleriGetir();
    this.urunleriGetir();
    this.olcuBirimleriniGetir();
    
    // Transaction refresh event'ini dinle
    this.refreshSubscription = this.transactionRefreshService.refresh$.subscribe(() => {
      console.log('Transaction refresh event received, refreshing list...');
      this.islemleriGetir();
    });
  }
  
  ngOnDestroy() {
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
    }
  }

  olcuBirimleriniGetir() {
    const tenantId = this.authService.getUser()?.tenantId;
    const qs = tenantId ? `?tenantId=${tenantId}` : '';
    this.http.get<OlcuBirimi[]>(`${environment.apiUrl}/unit-of-measures${qs}`, { headers: this.headers }).subscribe({
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
    const user = this.authService.getUser();
    const tenantId = user?.tenantId;
    const userRoles = user?.roles || [];
    const isUser = userRoles.includes('User') && !userRoles.includes('Admin') && !userRoles.includes('SuperAdmin');
    
    // Query string oluştur
    const params = new URLSearchParams();
    if (tenantId) {
      params.append('tenantId', tenantId.toString());
    }
    // User rolü ise sadece kendi işlemlerini göster
    if (isUser && user?.id) {
      params.append('userId', user.id.toString());
    }
    
    const qs = params.toString() ? `?${params.toString()}` : '';
    this.http.get<Islem[]>(`${environment.apiUrl}/transactions${qs}`, { headers: this.headers }).subscribe({
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
    const tenantId = this.authService.getUser()?.tenantId;
    const qs = tenantId ? `?tenantId=${tenantId}` : '';
    this.http.get<Urun[]>(`${environment.apiUrl}/products${qs}`, { headers: this.headers }).subscribe({
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
      const miktar = Number(item.miktar) || 0;
      return total + (birimFiyat * miktar);
    }, 0);
  }

  getUrunAdi(urunId: number): string {
    const urun = this.urunler.find(u => u.id === urunId);
    return urun ? (urun.urunAdi || urun.urun_adi || '-') : '-';
  }

  getUrunBirimFiyat(urunId: number): number {
    const urun = this.urunler.find(u => u.id === urunId);
    return urun ? (urun.birimFiyat || urun.birim_fiyat || 0) : 0;
  }

  getUrunOlcuBirimi(urunId: number): string {
    const urun = this.urunler.find(u => u.id === urunId);
    if (!urun) return '';
    // Product carries unit name directly
    return (urun.olcuBirimi as any) || (urun as any).olcu_birimi || '';
  }

  onUrunSelected(index: number, selectedUrunId: any) {
    const selectedUrun = this.seciliUrunler[index];
    if (!selectedUrun) return;
    
    // ngModelChange event'i direkt değeri döner (number veya string)
    // [(ngModel)] zaten item.urun_id'yi set ediyor, bu yüzden tekrar set etmiyoruz
    const urunId = typeof selectedUrunId === 'number' 
      ? selectedUrunId 
      : parseInt(selectedUrunId || '0');
    
    if (urunId && urunId !== 0) {
      // Veritabanından güncel ürün/birim fiyatını çek
      const headers = this.headers;
      this.http.get<any>(`${environment.apiUrl}/products/${urunId}`, { headers }).subscribe({
        next: (urun) => {
          // Cache'i güncelle
          const idx = this.urunler.findIndex(u => u.id === urun.id);
          if (idx > -1) {
            this.urunler[idx] = { ...this.urunler[idx], ...urun };
          } else {
            this.urunler.push(urun);
          }
          // Birim fiyatı modele yaz
          const birimFiyat = Number(urun?.birimFiyat ?? urun?.birim_fiyat ?? 0);
          selectedUrun.birim_fiyat = birimFiyat;
          this.hesaplaToplamTutar();
          // Change detection'ı tetikle
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Ürün bilgisi alınamadı:', err);
          // Fallback: listeden oku
          const birimFiyat = this.getUrunBirimFiyat(urunId);
          selectedUrun.birim_fiyat = birimFiyat;
          this.hesaplaToplamTutar();
          this.cdr.detectChanges();
        }
      });
    } else {
      // Ürün seçimi temizlendi
      selectedUrun.birim_fiyat = 0;
      this.hesaplaToplamTutar();
      this.cdr.detectChanges();
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

    // Send Items with actual birimFiyat from products
    const items = this.seciliUrunler.map(item => {
      const urun = this.urunler.find(u => u.id === item.urun_id);
      return {
        urunId: item.urun_id,
        urunAdi: urun ? (urun.urunAdi || urun.urun_adi || '') : '',
        miktar: item.miktar,
        birimFiyat: this.getUrunBirimFiyat(item.urun_id),
        subtotal: this.getUrunBirimFiyat(item.urun_id) * item.miktar
      };
    });

    // Get user info for TenantId and UserId
    const user = this.authService.getUser();
    const yeniIslem = {
      tenantId: user?.tenantId || 1,
      userId: user?.id || 1,
      islemKodu: this.islemKodu,
      islemTipi: this.islemTipi, // Kullanıcının seçtiği işlem tipi
      odemeTipi: 'NAKIT',
      items: items,
      toplamTutar: this.toplamTutar
    };

    this.http.post<Islem>(`${environment.apiUrl}/transactions`, yeniIslem, { headers: this.headers }).subscribe({
      next: () => {
        alert('İşlem eklendi');
        this.islemleriGetir();
        this.islemKodu = '';
        this.islemTipi = 'SATIS'; // Varsayılan değere dön
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
      this.http.delete(`${environment.apiUrl}/transactions/${id}`, { headers: this.headers }).subscribe({
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
