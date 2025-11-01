import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '@/app/services/auth.service';

@Component({
  selector: 'app-urunler',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './urunler.component.html'
})
export class UrunlerComponent implements OnInit {
  urunler: any[] = [];
  kategoriler: any[] = [];
  olcuBirimleri: any[] = [];
  loading = false;
  error: string | null = null;
  
  // Modal state
  showModal = false;
  editingProduct: any = null;
  saving = false;
  
  // Form data
  formData = {
    urunAdi: '',
    birimFiyat: 0,
    kategoriId: null as number | null,
    olcuBirimi: 'Adet',
    stokMiktari: 0,
    tenantId: null as number | null
  };

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.loadUrunler();
    this.loadKategoriler();
    this.loadOlcuBirimleri();
  }

  get headers() {
    return {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };
  }

  loadUrunler(): void {
    this.loading = true;
    this.error = null;

    this.http.get<any[]>(`${environment.apiUrl}/products`, { headers: this.headers })
      .subscribe({
        next: (data) => {
          this.urunler = data;
          this.loading = false;
        },
        error: (err) => {
          console.error('Ürünler yüklenemedi:', err);
          this.error = 'Ürünler yüklenirken bir hata oluştu.';
          this.loading = false;
        }
      });
  }

  loadKategoriler(): void {
    this.http.get<any[]>(`${environment.apiUrl}/categories`, { headers: this.headers })
      .subscribe({
        next: (data) => {
          this.kategoriler = data;
        },
        error: (err) => console.error('Kategoriler yüklenemedi:', err)
      });
  }

  loadOlcuBirimleri(): void {
    this.http.get<any[]>(`${environment.apiUrl}/unit-of-measures`, { headers: this.headers })
      .subscribe({
        next: (data) => {
          this.olcuBirimleri = data;
        },
        error: (err) => console.error('Ölçü birimleri yüklenemedi:', err)
      });
  }

  openCreateModal(): void {
    const currentUser = this.authService.getUser();
    this.editingProduct = null;
    this.formData = {
      urunAdi: '',
      birimFiyat: 0,
      kategoriId: null,
      olcuBirimi: 'Adet',
      stokMiktari: 0,
      tenantId: currentUser?.tenantId || null
    };
    this.showModal = true;
  }

  openEditModal(product: any): void {
    this.editingProduct = product;
    this.formData = {
      urunAdi: product.urunAdi || '',
      birimFiyat: product.birimFiyat || 0,
      kategoriId: product.kategoriId,
      olcuBirimi: product.olcuBirimi || 'Adet',
      stokMiktari: product.stokMiktari || 0,
      tenantId: product.tenantId
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingProduct = null;
  }

  saveProduct(): void {
    if (!this.formData.urunAdi || this.formData.birimFiyat <= 0) {
      alert('Ürün adı ve fiyat zorunludur!');
      return;
    }

    this.saving = true;
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    const url = this.editingProduct 
      ? `${environment.apiUrl}/products/${this.editingProduct.id}`
      : `${environment.apiUrl}/products`;

    const method = this.editingProduct ? 'put' : 'post';

    this.http.request(method, url, { headers, body: this.formData })
      .subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadUrunler();
        },
        error: (err) => {
          console.error('Ürün kaydedilemedi:', err);
          alert('Ürün kaydedilirken bir hata oluştu!');
          this.saving = false;
        }
      });
  }

  toggleActive(product: any): void {
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.put(`${environment.apiUrl}/products/${product.id}/toggle-active`, {}, { headers })
      .subscribe({
        next: () => {
          this.loadUrunler();
        },
        error: (err) => {
          console.error('Durum değiştirilemedi:', err);
          alert('Durum değiştirilirken bir hata oluştu!');
        }
      });
  }

  deleteProduct(product: any): void {
    if (!confirm(`${product.urunAdi} ürününü silmek istediğinize emin misiniz?`)) {
      return;
    }

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.delete(`${environment.apiUrl}/products/${product.id}`, { headers })
      .subscribe({
        next: () => {
          this.loadUrunler();
        },
        error: (err) => {
          console.error('Ürün silinemedi:', err);
          alert(err.error?.message || 'Ürün silinirken bir hata oluştu!');
        }
      });
  }

  getKategoriAdi(kategoriId: number | null): string {
    if (!kategoriId) return '-';
    const kategori = this.kategoriler.find(k => k.id === kategoriId);
    return kategori?.kategoriAdi || '-';
  }
}
