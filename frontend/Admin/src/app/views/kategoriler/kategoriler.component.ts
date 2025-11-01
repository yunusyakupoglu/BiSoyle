import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '@/app/services/auth.service';

@Component({
  selector: 'app-kategoriler',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './kategoriler.component.html'
})
export class KategorilerComponent implements OnInit {
  kategoriler: any[] = [];
  loading = false;
  error: string | null = null;
  
  // Modal state
  showModal = false;
  editingCategory: any = null;
  saving = false;
  
  // Form data
  formData = {
    kategoriAdi: '',
    aciklama: '',
    tenantId: null as number | null
  };

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.loadKategoriler();
  }

  loadKategoriler() {
    this.loading = true;
    this.error = null;

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.get<any[]>(`${environment.apiUrl}/categories`, { headers })
      .subscribe({
        next: (data) => {
          this.kategoriler = data;
          this.loading = false;
        },
        error: (err) => {
          console.error('Kategoriler yüklenemedi:', err);
          this.error = 'Kategoriler yüklenirken bir hata oluştu.';
          this.loading = false;
        }
      });
  }

  openCreateModal(): void {
    const currentUser = this.authService.getUser();
    this.editingCategory = null;
    this.formData = {
      kategoriAdi: '',
      aciklama: '',
      tenantId: currentUser?.tenantId || null
    };
    this.showModal = true;
  }

  openEditModal(category: any): void {
    this.editingCategory = category;
    this.formData = {
      kategoriAdi: category.kategoriAdi || '',
      aciklama: category.aciklama || '',
      tenantId: category.tenantId
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingCategory = null;
  }

  saveCategory(): void {
    if (!this.formData.kategoriAdi) {
      alert('Kategori adı zorunludur!');
      return;
    }

    this.saving = true;
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    const url = this.editingCategory 
      ? `${environment.apiUrl}/categories/${this.editingCategory.id}`
      : `${environment.apiUrl}/categories`;

    const method = this.editingCategory ? 'put' : 'post';

    this.http.request(method, url, { headers, body: this.formData })
      .subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadKategoriler();
        },
        error: (err) => {
          console.error('Kategori kaydedilemedi:', err);
          alert('Kategori kaydedilirken bir hata oluştu!');
          this.saving = false;
        }
      });
  }

  toggleActive(category: any): void {
    if (!confirm(`${category.kategoriAdi} kategorisini ${category.aktif ? 'pasif' : 'aktif'} etmek istediğinize emin misiniz?`)) {
      return;
    }

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.put(`${environment.apiUrl}/categories/${category.id}/toggle-active`, {}, { headers })
      .subscribe({
        next: () => {
          this.loadKategoriler();
        },
        error: (err) => {
          console.error('Durum değiştirilemedi:', err);
          alert('Durum değiştirilirken bir hata oluştu!');
        }
      });
  }

  deleteCategory(category: any): void {
    if (!confirm(`${category.kategoriAdi} kategorisini silmek istediğinize emin misiniz?`)) {
      return;
    }

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.delete(`${environment.apiUrl}/categories/${category.id}`, { headers })
      .subscribe({
        next: () => {
          this.loadKategoriler();
        },
        error: (err) => {
          console.error('Kategori silinemedi:', err);
          alert(err.error?.message || 'Kategori silinirken bir hata oluştu!');
        }
      });
  }
}
