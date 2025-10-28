import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface Kategori {
  id: number;
  kategori_adi: string;
  kullanici_id: number;
}

@Component({
  selector: 'app-kategoriler',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './kategoriler.component.html'
})
export class KategorilerComponent implements OnInit {
  private apiUrl = 'http://localhost:8000';
  
  kategoriler: Kategori[] = [];
  yeniKategori: string = '';
  duzenleKategori: Kategori | null = null;
  yukleniyorData: boolean = false;
  mesaj: string = '';
  mesajTip: 'success' | 'danger' | 'warning' | 'info' = 'info';

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadKategoriler();
  }

  loadKategoriler() {
    this.yukleniyorData = true;
    this.http.get<Kategori[]>(`${this.apiUrl}/kategoriler`).subscribe({
      next: (data) => {
        this.kategoriler = data;
        this.yukleniyorData = false;
      },
      error: (err) => {
        this.mesaj = 'Kategoriler yüklenemedi: ' + err.message;
        this.mesajTip = 'danger';
        this.yukleniyorData = false;
      }
    });
  }

  ekle() {
    if (!this.yeniKategori.trim()) {
      this.mesaj = '⚠️ Kategori adı gerekli';
      this.mesajTip = 'warning';
      return;
    }

    this.http.post<Kategori>(`${this.apiUrl}/kategoriler`, {
      kategori_adi: this.yeniKategori
    }).subscribe({
      next: (data) => {
        this.mesaj = '✅ Kategori eklendi';
        this.mesajTip = 'success';
        this.yeniKategori = '';
        this.loadKategoriler();
      },
      error: (err) => {
        this.mesaj = '❌ Hata: ' + err.message;
        this.mesajTip = 'danger';
      }
    });
  }

  duzenle(kategori: Kategori) {
    this.duzenleKategori = { ...kategori };
  }

  guncelle() {
    if (!this.duzenleKategori) return;

    this.http.put<Kategori>(`${this.apiUrl}/kategoriler/${this.duzenleKategori.id}`, {
      kategori_adi: this.duzenleKategori.kategori_adi
    }).subscribe({
      next: (data) => {
        this.mesaj = '✅ Kategori güncellendi';
        this.mesajTip = 'success';
        this.duzenleKategori = null;
        this.loadKategoriler();
      },
      error: (err) => {
        this.mesaj = '❌ Hata: ' + err.message;
        this.mesajTip = 'danger';
      }
    });
  }

  sil(id: number) {
    if (!confirm('Bu kategoriyi silmek istediğinize emin misiniz?')) return;

    this.http.delete(`${this.apiUrl}/kategoriler/${id}`).subscribe({
      next: () => {
        this.mesaj = '✅ Kategori silindi';
        this.mesajTip = 'success';
        this.loadKategoriler();
      },
      error: (err) => {
        this.mesaj = '❌ Hata: ' + err.message;
        this.mesajTip = 'danger';
      }
    });
  }

  iptal() {
    this.duzenleKategori = null;
  }
}



