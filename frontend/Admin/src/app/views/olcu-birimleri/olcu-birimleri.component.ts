import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '@/app/services/auth.service';

@Component({
  selector: 'app-olcu-birimleri',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './olcu-birimleri.component.html'
})
export class OlcuBirimleriComponent implements OnInit {
  olcuBirimleri: any[] = [];
  loading = false;
  error: string | null = null;
  
  // Modal state
  showModal = false;
  editingUnit: any = null;
  saving = false;
  
  // Form data
  formData = {
    birimAdi: '',
    kisaltma: ''
  };

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.loadOlcuBirimleri();
  }

  loadOlcuBirimleri(): void {
    this.loading = true;
    this.error = null;

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.get<any[]>(`${environment.apiUrl}/unit-of-measures`, { headers })
      .subscribe({
        next: (data) => {
          this.olcuBirimleri = data;
          this.loading = false;
        },
        error: (err) => {
          console.error('Ölçü birimleri yüklenemedi:', err);
          this.error = 'Ölçü birimleri yüklenirken bir hata oluştu.';
          this.loading = false;
        }
      });
  }

  openCreateModal(): void {
    this.editingUnit = null;
    this.formData = {
      birimAdi: '',
      kisaltma: ''
    };
    this.showModal = true;
  }

  openEditModal(unit: any): void {
    this.editingUnit = unit;
    this.formData = {
      birimAdi: unit.birimAdi || '',
      kisaltma: unit.kisaltma || ''
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingUnit = null;
  }

  saveUnit(): void {
    if (!this.formData.birimAdi || !this.formData.kisaltma) {
      alert('Birim adı ve kısaltma zorunludur!');
      return;
    }

    this.saving = true;
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    const url = this.editingUnit 
      ? `${environment.apiUrl}/unit-of-measures/${this.editingUnit.id}`
      : `${environment.apiUrl}/unit-of-measures`;

    const method = this.editingUnit ? 'put' : 'post';

    this.http.request(method, url, { headers, body: this.formData })
      .subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadOlcuBirimleri();
        },
        error: (err) => {
          console.error('Ölçü birimi kaydedilemedi:', err);
          alert('Ölçü birimi kaydedilirken bir hata oluştu!');
          this.saving = false;
        }
      });
  }

  toggleActive(unit: any): void {
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.put(`${environment.apiUrl}/unit-of-measures/${unit.id}/toggle-active`, {}, { headers })
      .subscribe({
        next: () => {
          this.loadOlcuBirimleri();
        },
        error: (err) => {
          console.error('Durum değiştirilemedi:', err);
          alert('Durum değiştirilirken bir hata oluştu!');
        }
      });
  }

  deleteUnit(unit: any): void {
    if (!confirm(`${unit.birimAdi} ölçü birimini silmek istediğinize emin misiniz?`)) {
      return;
    }

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.delete(`${environment.apiUrl}/unit-of-measures/${unit.id}`, { headers })
      .subscribe({
        next: () => {
          this.loadOlcuBirimleri();
        },
        error: (err) => {
          console.error('Ölçü birimi silinemedi:', err);
          alert(err.error?.message || 'Ölçü birimi silinirken bir hata oluştu!');
        }
      });
  }
}
