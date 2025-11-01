import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '@/app/services/auth.service';

@Component({
  selector: 'app-firmalar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './firmalar.component.html',
  styleUrls: ['./firmalar.component.scss']
})
export class FirmalarComponent implements OnInit {
  firmalar: any[] = [];
  loading = false;
  error: string | null = null;
  
  // Modal state
  showModal = false;
  editingFirma: any = null;
  saving = false;
  
  // Admin user modal
  showAdminModal = false;
  selectedFirma: any = null;
  savingAdmin = false;
  
  // Form data
  formData = {
    firmaAdi: '',
    email: '',
    telefon: '',
    vergiNo: '',
    adres: ''
  };
  
  // Admin user form data
  adminFormData = {
    firstName: '',
    lastName: '',
    username: '',
    email: '',
    password: '',
    title: 'Firma Yöneticisi'
  };

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadFirmalar();
  }

  loadFirmalar(): void {
    this.loading = true;
    this.error = null;

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.get<any[]>(`${environment.apiUrl}/tenants`, { headers })
      .subscribe({
        next: (data) => {
          this.firmalar = data;
          this.loading = false;
        },
        error: (err) => {
          console.error('Firmalar yüklenemedi:', err);
          this.error = 'Firmalar yüklenirken bir hata oluştu.';
          this.loading = false;
        }
      });
  }

  openCreateModal(): void {
    this.editingFirma = null;
    this.formData = {
      firmaAdi: '',
      email: '',
      telefon: '',
      vergiNo: '',
      adres: ''
    };
    this.showModal = true;
  }

  openEditModal(firma: any): void {
    this.editingFirma = firma;
    this.formData = {
      firmaAdi: firma.firmaAdi || '',
      email: firma.email || '',
      telefon: firma.telefon || '',
      vergiNo: firma.vergiNo || '',
      adres: firma.adres || ''
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingFirma = null;
  }

  saveFirma(): void {
    if (!this.formData.firmaAdi || !this.formData.email) {
      alert('Firma adı ve e-posta zorunludur!');
      return;
    }

    this.saving = true;
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    const url = this.editingFirma 
      ? `${environment.apiUrl}/tenants/${this.editingFirma.id}`
      : `${environment.apiUrl}/tenants`;

    const method = this.editingFirma ? 'put' : 'post';

    this.http.request(method, url, { headers, body: this.formData })
      .subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadFirmalar();
        },
        error: (err) => {
          console.error('Firma kaydedilemedi:', err);
          alert('Firma kaydedilirken bir hata oluştu!');
          this.saving = false;
        }
      });
  }

  toggleActive(firma: any): void {
    if (!confirm(`${firma.firmaAdi} firmasını ${firma.aktif ? 'pasif' : 'aktif'} etmek istediğinize emin misiniz?`)) {
      return;
    }

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.put(`${environment.apiUrl}/tenants/${firma.id}/toggle-active`, {}, { headers })
      .subscribe({
        next: () => {
          this.loadFirmalar();
        },
        error: (err) => {
          console.error('Durum değiştirilemedi:', err);
          alert('Durum değiştirilirken bir hata oluştu!');
        }
      });
  }

  deleteFirma(firma: any): void {
    if (!confirm(`${firma.firmaAdi} firmasını silmek istediğinize emin misiniz? Bu işlem geri alınamaz!`)) {
      return;
    }

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.delete(`${environment.apiUrl}/tenants/${firma.id}`, { headers })
      .subscribe({
        next: () => {
          this.loadFirmalar();
        },
        error: (err) => {
          console.error('Firma silinemedi:', err);
          alert('Firma silinirken bir hata oluştu!');
        }
      });
  }

  openCreateAdminModal(firma: any): void {
    this.selectedFirma = firma;
    this.adminFormData = {
      firstName: '',
      lastName: '',
      username: '',
      email: '',
      password: '',
      title: 'Firma Yöneticisi'
    };
    this.showAdminModal = true;
  }

  closeAdminModal(): void {
    this.showAdminModal = false;
    this.selectedFirma = null;
  }

  createAdminUser(): void {
    if (!this.adminFormData.firstName || !this.adminFormData.username || 
        !this.adminFormData.email || !this.adminFormData.password) {
      alert('Tüm zorunlu alanları doldurun!');
      return;
    }

    if (!this.selectedFirma) {
      alert('Firma seçilmedi!');
      return;
    }

    this.savingAdmin = true;
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    // Get Admin role ID
    this.http.get<any[]>(`${environment.apiUrl}/roles`, { headers })
      .subscribe({
        next: (roles) => {
          const adminRole = roles.find(r => r.roleAdi === 'Admin');
          
          if (!adminRole) {
            alert('Admin rolü bulunamadı!');
            this.savingAdmin = false;
            return;
          }

          // Create user with Admin role
          const userData = {
            tenantId: this.selectedFirma.id,
            username: this.adminFormData.username,
            email: this.adminFormData.email,
            password: this.adminFormData.password,
            firstName: this.adminFormData.firstName,
            lastName: this.adminFormData.lastName,
            title: this.adminFormData.title,
            location: this.selectedFirma.adres || 'Turkey',
            roleIds: [adminRole.id]
          };

          this.http.post(`${environment.apiUrl}/users`, userData, { headers })
            .subscribe({
              next: () => {
                alert(`✅ Admin kullanıcısı oluşturuldu!\n\nGiriş Bilgileri:\nE-posta: ${this.adminFormData.email}\nŞifre: ${this.adminFormData.password}\n\nBu bilgileri kaydedin!`);
                this.savingAdmin = false;
                this.closeAdminModal();
              },
              error: (err) => {
                console.error('Admin kullanıcı oluşturulamadı:', err);
                alert(err.error?.message || 'Kullanıcı oluşturulurken bir hata oluştu!');
                this.savingAdmin = false;
              }
            });
        },
        error: (err) => {
          console.error('Roller yüklenemedi:', err);
          alert('Roller yüklenirken bir hata oluştu!');
          this.savingAdmin = false;
        }
      });
  }
}

