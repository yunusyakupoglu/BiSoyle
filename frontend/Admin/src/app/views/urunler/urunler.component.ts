import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from 'src/app/services/auth.service';

interface Urun {
  id: number;
  urun_adi: string;
  birim_fiyat: number;
  kategori_id: number;
  olcu_birimi_id: number;
}

interface Kategori {
  id: number;
  kategori_adi: string;
}

interface OlcuBirimi {
  id: number;
  olcu_birimi_adi: string;
}

@Component({
  selector: 'app-urunler',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './urunler.component.html'
})
export class UrunlerComponent implements OnInit {
  urunler: Urun[] = [];
  kategoriler: Kategori[] = [];
  olcuBirimleri: OlcuBirimi[] = [];
  yukleniyor = false;
  
  // Form alanları
  urunAdi: string = '';
  birimFiyat: number = 0;
  kategoriId: number = 0;
  olcuBirimiId: number = 0;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.kategorileriGetir();
    this.olcuBirimleriniGetir();
    this.urunleriGetir();
  }

  get headers() {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  kategorileriGetir() {
    this.http.get<Kategori[]>('http://localhost:8000/kategoriler', { headers: this.headers }).subscribe({
      next: (data) => this.kategoriler = data,
      error: (err) => console.error('Kategoriler yüklenemedi:', err)
    });
  }

  olcuBirimleriniGetir() {
    this.http.get<OlcuBirimi[]>('http://localhost:8000/olcu-birimleri', { headers: this.headers }).subscribe({
      next: (data) => this.olcuBirimleri = data,
      error: (err) => console.error('Olcu birimleri yüklenemedi:', err)
    });
  }

  urunleriGetir() {
    this.yukleniyor = true;
    this.http.get<Urun[]>('http://localhost:8000/urunler', { headers: this.headers }).subscribe({
      next: (data) => {
        this.urunler = data;
        this.yukleniyor = false;
      },
      error: (err) => {
        console.error('Ürünler yüklenemedi:', err);
        this.yukleniyor = false;
      }
    });
  }

  urunEkle() {
    if (!this.urunAdi || !this.birimFiyat || !this.kategoriId || !this.olcuBirimiId) {
      alert('Tüm alanları doldurun');
      return;
    }

    const yeniUrun = {
      urun_adi: this.urunAdi,
      birim_fiyat: this.birimFiyat,
      kategori_id: this.kategoriId,
      olcu_birimi_id: this.olcuBirimiId
    };

    this.http.post<Urun>('http://localhost:8000/urunler', yeniUrun, { headers: this.headers }).subscribe({
      next: () => {
        alert('Ürün eklendi');
        this.urunleriGetir();
        this.urunAdi = '';
        this.birimFiyat = 0;
        this.kategoriId = 0;
        this.olcuBirimiId = 0;
      },
      error: (err) => {
        alert('Ürün eklenemedi: ' + (err.error?.detail || err.message));
        console.error('Ürün eklenemedi:', err);
      }
    });
  }

  urunSil(id: number) {
    if (confirm('Bu ürünü silmek istediğinizden emin misiniz?')) {
      this.http.delete(`http://localhost:8000/urunler/${id}`, { headers: this.headers }).subscribe({
        next: () => {
          alert('Ürün silindi');
          this.urunleriGetir();
        },
        error: (err) => {
          alert('Ürün silinemedi: ' + (err.error?.detail || err.message));
        }
      });
    }
  }

  getKategoriAdi(id: number): string {
    const kategori = this.kategoriler.find(k => k.id === id);
    return kategori ? kategori.kategori_adi : 'Bulunamadı';
  }

  getOlcuBirimiAdi(id: number): string {
    const olcu = this.olcuBirimleri.find(o => o.id === id);
    return olcu ? olcu.olcu_birimi_adi : 'Bulunamadı';
  }
}
