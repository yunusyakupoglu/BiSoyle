import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '@/app/services/auth.service';

@Component({
  selector: 'app-abonelikler',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './abonelikler.component.html',
  styleUrls: ['./abonelikler.component.scss']
})
export class AboneliklerComponent implements OnInit {
  abonelikler: any[] = [];
  loading = false;
  error: string | null = null;
  
  // Modal state
  showModal = false;
  editingPlan: any = null;
  saving = false;
  
  // Form data
  formData = {
    planAdi: '',
    maxKullaniciSayisi: 5,
    aylikUcret: 0
  };

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadAbonelikler();
  }

  loadAbonelikler(): void {
    this.loading = true;
    this.error = null;

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.get<any[]>(`${environment.apiUrl}/subscription-plans/all`, { headers })
      .subscribe({
        next: (data) => {
          this.abonelikler = data;
          this.loading = false;
        },
        error: (err) => {
          console.error('Abonelikler yüklenemedi:', err);
          this.error = 'Abonelikler yüklenirken bir hata oluştu.';
          this.loading = false;
        }
      });
  }

  openCreateModal(): void {
    this.editingPlan = null;
    this.formData = {
      planAdi: '',
      maxKullaniciSayisi: 5,
      aylikUcret: 0
    };
    this.showModal = true;
  }

  openEditModal(plan: any): void {
    this.editingPlan = plan;
    this.formData = {
      planAdi: plan.planAdi || '',
      maxKullaniciSayisi: plan.maxKullaniciSayisi || 5,
      aylikUcret: plan.aylikUcret || 0
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingPlan = null;
  }

  savePlan(): void {
    if (!this.formData.planAdi || this.formData.maxKullaniciSayisi < 1) {
      alert('Lütfen tüm gerekli alanları doldurun!');
      return;
    }

    this.saving = true;
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    const url = this.editingPlan 
      ? `${environment.apiUrl}/subscription-plans/${this.editingPlan.id}`
      : `${environment.apiUrl}/subscription-plans`;

    const method = this.editingPlan ? 'put' : 'post';

    this.http.request(method, url, { headers, body: this.formData })
      .subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadAbonelikler();
        },
        error: (err) => {
          console.error('Plan kaydedilemedi:', err);
          alert('Plan kaydedilirken bir hata oluştu!');
          this.saving = false;
        }
      });
  }

  toggleActive(plan: any): void {
    if (!confirm(`${plan.planAdi} planını ${plan.aktif ? 'pasif' : 'aktif'} etmek istediğinize emin misiniz?`)) {
      return;
    }

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.put(`${environment.apiUrl}/subscription-plans/${plan.id}/toggle-active`, {}, { headers })
      .subscribe({
        next: () => {
          this.loadAbonelikler();
        },
        error: (err) => {
          console.error('Durum değiştirilemedi:', err);
          alert('Durum değiştirilirken bir hata oluştu!');
        }
      });
  }

  deletePlan(plan: any): void {
    if (!confirm(`${plan.planAdi} planını silmek istediğinize emin misiniz? Bu işlem geri alınamaz!`)) {
      return;
    }

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.delete(`${environment.apiUrl}/subscription-plans/${plan.id}`, { headers })
      .subscribe({
        next: () => {
          this.loadAbonelikler();
        },
        error: (err) => {
          console.error('Plan silinemedi:', err);
          alert(err.error?.message || 'Plan silinirken bir hata oluştu!');
        }
      });
  }
}

