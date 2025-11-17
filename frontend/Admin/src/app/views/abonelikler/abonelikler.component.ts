import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
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
  savingPaymentSettings = false;
  savingMailSettings = false;
  
  // Form data
  formData = {
    planAdi: '',
    maxKullaniciSayisi: 5,
    maxBayiSayisi: 1,
    aylikUcret: 0
  };

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  paymentSettings = {
    iban: '',
    bankName: '',
    accountHolder: '',
    updatedAt: ''
  };

  paymentSettingsError: string | null = null;
  paymentSettingsSuccess: string | null = null;
  mailSettingsError: string | null = null;
  mailSettingsSuccess: string | null = null;

  mailSettings = {
    host: '',
    port: 587,
    security: 'STARTTLS',
    username: '',
    password: '',
    fromEmail: '',
    fromName: '',
    replyTo: '',
    passwordSet: false
  };

  readonly mailSecurityOptions = [
    { value: 'STARTTLS', label: 'STARTTLS (Önerilen)' },
    { value: 'SSL', label: 'SSL/TLS' },
    { value: 'NONE', label: 'Güvenli Bağlantı Yok' }
  ];

  ngOnInit(): void {
    this.loadAbonelikler();
    this.loadPaymentSettings();
    this.loadMailSettings();
  }

  loadMailSettings(): void {
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.get<any>(`${environment.apiUrl}/platform-settings/mail`, { headers })
      .subscribe({
        next: (data) => {
          this.mailSettings = {
            host: data?.host || '',
            port: data?.port || 587,
            security: data?.security || 'STARTTLS',
            username: data?.username || '',
            password: '',
            fromEmail: data?.fromEmail || '',
            fromName: data?.fromName || '',
            replyTo: data?.replyTo || '',
            passwordSet: data?.passwordSet || false
          };
        },
        error: (err) => {
          console.error('Mail ayarları yüklenemedi:', err);
          this.mailSettingsError = err.error?.detail || 'Mail ayarları yüklenirken bir hata oluştu.';
        }
      });
  }

  loadPaymentSettings(): void {
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.get<any>(`${environment.apiUrl}/platform-settings/payment`, { headers })
      .subscribe({
        next: (data) => {
          this.paymentSettings = {
            iban: data?.iban || '',
            bankName: data?.bankName || '',
            accountHolder: data?.accountHolder || '',
            updatedAt: data?.updatedAt || ''
          };
        },
        error: (err) => {
          console.error('Ödeme ayarları yüklenemedi:', err);
          this.paymentSettingsError = err.error?.detail || 'Ödeme ayarları yüklenirken bir hata oluştu.';
        }
      });
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

  savePaymentSettings(form: NgForm): void {
    if (!this.paymentSettings.iban) {
      this.paymentSettingsError = 'Lütfen geçerli bir IBAN girin.';
      return;
    }

    this.paymentSettingsError = null;
    this.paymentSettingsSuccess = null;
    this.savingPaymentSettings = true;

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    const payload = {
      iban: this.paymentSettings.iban,
      bankName: this.paymentSettings.bankName,
      accountHolder: this.paymentSettings.accountHolder
    };

    this.http.put(`${environment.apiUrl}/platform-settings/payment`, payload, { headers })
      .subscribe({
        next: (response: any) => {
          this.savingPaymentSettings = false;
          this.paymentSettingsSuccess = 'Ödeme ayarları güncellendi.';
          this.paymentSettings.updatedAt = response?.updatedAt || new Date().toISOString();
          form.form.markAsPristine();
        },
        error: (err) => {
          console.error('Ödeme ayarları kaydedilemedi:', err);
          this.paymentSettingsError = err.error?.detail || err.error?.message || 'Ödeme ayarları kaydedilirken bir hata oluştu!';
          this.savingPaymentSettings = false;
        }
      });
  }

  saveMailSettings(form: NgForm): void {
    if (!this.mailSettings.host || !this.mailSettings.port || !this.mailSettings.username || !this.mailSettings.fromEmail) {
      this.mailSettingsError = 'Lütfen sunucu, port, kullanıcı adı ve gönderici e-posta alanlarını doldurun.';
      return;
    }

    if (!this.mailSettings.password && !this.mailSettings.passwordSet) {
      this.mailSettingsError = 'İlk kurulum için SMTP parolasını girmeniz gerekir.';
      return;
    }

    this.mailSettingsError = null;
    this.mailSettingsSuccess = null;
    this.savingMailSettings = true;

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    const payload: any = {
      host: this.mailSettings.host,
      port: Number(this.mailSettings.port) || 587,
      security: this.mailSettings.security || 'STARTTLS',
      username: this.mailSettings.username,
      fromEmail: this.mailSettings.fromEmail,
      fromName: this.mailSettings.fromName,
      replyTo: this.mailSettings.replyTo || null
    };

    if (this.mailSettings.password) {
      payload.password = this.mailSettings.password;
    }

    this.http.put(`${environment.apiUrl}/platform-settings/mail`, payload, { headers })
      .subscribe({
        next: (response: any) => {
          this.savingMailSettings = false;
          this.mailSettingsSuccess = 'Mail ayarları güncellendi.';
          this.mailSettings.passwordSet = true;
          this.mailSettings.password = '';
          form.form.markAsPristine();
        },
        error: (err) => {
          console.error('Mail ayarları kaydedilemedi:', err);
          this.mailSettingsError = err.error?.detail || err.error?.message || 'Mail ayarları kaydedilirken bir hata oluştu!';
          this.savingMailSettings = false;
        }
      });
  }

  openCreateModal(): void {
    this.editingPlan = null;
    this.formData = {
      planAdi: '',
      maxKullaniciSayisi: 5,
      maxBayiSayisi: 1,
      aylikUcret: 0
    };
    this.showModal = true;
  }

  openEditModal(plan: any): void {
    this.editingPlan = plan;
    // Backend field adları: PlanAdi, MaxKullaniciSayisi, AylikUcret
    this.formData = {
      planAdi: plan.planAdi || plan.PlanAdi || '',
      maxKullaniciSayisi: plan.maxKullaniciSayisi || plan.MaxKullaniciSayisi || 5,
      maxBayiSayisi: plan.maxBayiSayisi || plan.MaxBayiSayisi || 1,
      aylikUcret: plan.aylikUcret || plan.AylikUcret || 0
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingPlan = null;
  }

  savePlan(): void {
    if (!this.formData.planAdi || this.formData.maxKullaniciSayisi < 1 || this.formData.maxBayiSayisi < 1) {
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

    // Backend ile uyumlu field adları
    const requestBody = {
      PlanAdi: this.formData.planAdi,
      MaxKullaniciSayisi: this.formData.maxKullaniciSayisi,
      MaxBayiSayisi: this.formData.maxBayiSayisi,
      AylikUcret: this.formData.aylikUcret
    };

    this.http.request(method, url, { headers, body: requestBody })
      .subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadAbonelikler();
        },
        error: (err) => {
          console.error('Plan kaydedilemedi:', err);
          const errorMessage = err.error?.detail || err.error?.message || 'Plan kaydedilirken bir hata oluştu!';
          alert(errorMessage);
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

