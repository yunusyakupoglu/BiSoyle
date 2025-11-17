import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '@/app/services/auth.service';

@Component({
  selector: 'app-firmalar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
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
  
  // Subscription modal
  showSubscriptionModal = false;
  subscriptionPlans: any[] = [];
  selectedPlanId: number | null = null;
  savingSubscription = false;
  showPaymentModal = false;
  paymentProcessing = false;
  paymentReference: string | null = null;
  paymentError: string | null = null;
  paymentSummary: any = null;
  availableInstallments: number[] = [1, 2, 3, 6, 9, 12];
  selectedPlanForPayment: any = null;
  paymentForm = {
    cardHolder: '',
    cardNumber: '',
    expiryMonth: '',
    expiryYear: '',
    cvv: '',
    installment: 1
  };
  
  // Admin user credentials modal (yeni firma eklenince gösterilecek)
  showAdminCredentialsModal = false;
  adminCredentials: { username: string; email: string; password: string } | null = null;

  // SuperAdmin kontrolü
  get isSuperAdmin(): boolean {
    const user = this.authService.getUser();
    if (!user) return false;
    // SuperAdmin kontrolü: tenantId null/0 veya roles içinde SuperAdmin varsa
    return !user.tenantId || user.tenantId === 0 || user.roles?.includes('SuperAdmin') || false;
  }

  getSelectedPlan(): any {
    if (this.selectedPlanId === null || this.selectedPlanId === undefined || this.selectedPlanId === ('' as any)) {
      return null;
    }
    const selectedId = Number(this.selectedPlanId);
    return this.subscriptionPlans.find(plan => Number(plan.id ?? plan.Id) === selectedId) || null;
  }

  onPlanChange(): void {
    // Ensure selectedPlanId is numeric (select returns string)
    if (this.selectedPlanId !== null && this.selectedPlanId !== undefined) {
      this.selectedPlanId = Number(this.selectedPlanId) as any;
    }
    this.selectedPlanForPayment = this.getSelectedPlan();
    this.paymentReference = null;
    this.paymentSummary = null;
  }

  resetPaymentForm(): void {
    this.paymentForm = {
      cardHolder: '',
      cardNumber: '',
      expiryMonth: '',
      expiryYear: '',
      cvv: '',
      installment: this.availableInstallments[0] || 1
    };
    this.paymentError = null;
  }

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadFirmalar();
    this.loadSubscriptionPlans();
    this.loadInstallmentOptions();
  }
  
  loadSubscriptionPlans(): void {
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };
    
    this.http.get<any[]>(`${environment.apiUrl}/subscription-plans/all`, { headers })
      .subscribe({
        next: (data) => {
          this.subscriptionPlans = data;
        },
        error: (err) => {
          console.error('Abonelik planları yüklenemedi:', err);
        }
      });
  }

  loadInstallmentOptions(): void {
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.get<number[]>(`${environment.apiUrl}/virtual-pos/installments`, { headers })
      .subscribe({
        next: (data) => {
          if (Array.isArray(data) && data.length > 0) {
            this.availableInstallments = data;
          }
        },
        error: (err) => {
          console.error('Taksit seçenekleri alınamadı:', err);
        }
      });
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
    // Backend field adları: FirmaAdi, Email, Telefon, VergiNo, Adres
    this.formData = {
      firmaAdi: firma.firmaAdi || firma.FirmaAdi || '',
      email: firma.email || firma.Email || '',
      telefon: firma.telefon || firma.Telefon || '',
      vergiNo: firma.vergiNo || firma.VergiNo || '',
      adres: firma.adres || firma.Adres || ''
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

    // Backend ile uyumlu field adları
    const requestBody = {
      FirmaAdi: this.formData.firmaAdi,
      Email: this.formData.email,
      Telefon: this.formData.telefon || null,
      Adres: this.formData.adres || null,
      VergiNo: this.formData.vergiNo || null
    };

    const url = this.editingFirma 
      ? `${environment.apiUrl}/tenants/${this.editingFirma.id}`
      : `${environment.apiUrl}/tenants`;

    const method = this.editingFirma ? 'put' : 'post';

    this.http.request(method, url, { headers, body: requestBody })
      .subscribe({
        next: (response: any) => {
          this.saving = false;
          this.closeModal();
          
          // Eğer yeni firma oluşturulduysa ve adminUser bilgileri varsa göster
          if (!this.editingFirma && response?.adminUser) {
            this.adminCredentials = {
              username: response.adminUser.username,
              email: response.adminUser.email,
              password: response.adminUser.password
            };
            this.showAdminCredentialsModal = true;
          } else {
            this.loadFirmalar();
          }
        },
        error: (err) => {
          console.error('Firma kaydedilemedi:', err);
          const errorMessage = err.error?.detail || err.error?.message || 'Firma kaydedilirken bir hata oluştu!';
          alert(errorMessage);
          this.saving = false;
        }
      });
  }

  toggleActive(firma: any): void {
    const firmaAdi = firma.firmaAdi || firma.FirmaAdi || 'Firma';
    const aktif = firma.aktif !== undefined ? firma.aktif : firma.Aktif;
    
    if (!confirm(`${firmaAdi} firmasını ${aktif ? 'pasif' : 'aktif'} etmek istediğinize emin misiniz?`)) {
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
          const errorMessage = err.error?.detail || err.error?.message || 'Durum değiştirilirken bir hata oluştu!';
          alert(errorMessage);
        }
      });
  }

  deleteFirma(firma: any): void {
    const firmaAdi = firma.firmaAdi || firma.FirmaAdi || 'Firma';
    
    if (!confirm(`${firmaAdi} firmasını silmek istediğinize emin misiniz? Bu işlem geri alınamaz!`)) {
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
          const errorMessage = err.error?.detail || err.error?.message || 'Firma silinirken bir hata oluştu!';
          alert(errorMessage);
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

    // Get Admin role ID (SuperAdmin rolü hariç)
    this.http.get<any[]>(`${environment.apiUrl}/roles`, { headers })
      .subscribe({
        next: (roles) => {
          // SuperAdmin rolünü filtrele - Admin kullanıcılar SuperAdmin ekleyemez
          const availableRoles = roles.filter(r => r.roleAdi !== 'SuperAdmin');
          const adminRole = availableRoles.find(r => r.roleAdi === 'Admin');
          
          if (!adminRole) {
            alert('Admin rolü bulunamadı!');
            this.savingAdmin = false;
            return;
          }

          // Create user with Admin role (SuperAdmin hariç)
          const userData = {
            tenantId: this.selectedFirma.id,
            username: this.adminFormData.username,
            email: this.adminFormData.email,
            password: this.adminFormData.password,
            firstName: this.adminFormData.firstName,
            lastName: this.adminFormData.lastName,
            title: this.adminFormData.title,
            location: this.selectedFirma.adres || 'Turkey',
            roleIds: [adminRole.id] // Sadece Admin rolü atanır
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
  
  openSubscriptionModal(firma: any): void {
    this.selectedFirma = firma;
    this.selectedPlanId = null;
    this.selectedPlanForPayment = null;
    this.paymentReference = null;
    this.paymentSummary = null;
    this.paymentError = null;
    this.showPaymentModal = false;
    this.resetPaymentForm();
    this.showSubscriptionModal = true;
  }
  
  closeSubscriptionModal(): void {
    this.showSubscriptionModal = false;
    this.selectedFirma = null;
    this.selectedPlanId = null;
    this.selectedPlanForPayment = null;
    this.paymentReference = null;
    this.paymentSummary = null;
    this.paymentError = null;
    this.showPaymentModal = false;
    this.resetPaymentForm();
  }
  
  assignSubscription(skipPaymentCheck = false): void {
    if (!this.selectedFirma || !this.selectedPlanId) {
      alert('Lütfen bir abonelik planı seçin!');
      return;
    }

    const selectedPlan = this.getSelectedPlan();
    if (!selectedPlan) {
      alert('Seçilen plan bulunamadı!');
      return;
    }

    const planPrice = selectedPlan.aylikUcret ?? selectedPlan.AylikUcret ?? 0;

    if (!skipPaymentCheck && planPrice > 0 && !this.paymentReference) {
      this.selectedPlanForPayment = selectedPlan;
      this.resetPaymentForm();
      this.showPaymentModal = true;
      return;
    }
    
    this.savingSubscription = true;
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };
    
    const subscriptionData: any = {
      tenantId: this.selectedFirma.id,
      planId: this.selectedPlanId,
      baslangicTarihi: new Date().toISOString(),
      bitisTarihi: null
    };

    if (planPrice > 0 && this.paymentReference) {
      subscriptionData.paymentReference = this.paymentReference;
      subscriptionData.odenenTutar = this.paymentSummary?.chargedAmount ?? planPrice;
      subscriptionData.taksitSayisi = this.paymentSummary?.installment ?? this.paymentForm.installment;
      subscriptionData.paraBirimi = this.paymentSummary?.currency ?? 'TRY';
    }
    
    this.http.post(`${environment.apiUrl}/subscriptions`, subscriptionData, { headers })
      .subscribe({
        next: (response: any) => {
          this.savingSubscription = false;
          if (response.licenseKey) {
            alert(`✅ Abonelik atandı!\n\nLisans Anahtarı:\n${response.licenseKey}\n\nBu anahtarı kaydedin ve firmaya iletin!`);
          } else {
            alert('Abonelik atandı!');
          }
          this.closeSubscriptionModal();
          this.loadFirmalar();
        },
        error: (err) => {
          console.error('Abonelik atanamadı:', err);
          const errorMessage = err.error?.detail || err.error?.message || 'Abonelik atanırken bir hata oluştu!';
          alert(errorMessage);
          this.savingSubscription = false;
        }
      });
  }

  closePaymentModal(): void {
    this.showPaymentModal = false;
  }

  processPayment(): void {
    if (!this.selectedFirma || !this.selectedPlanForPayment) {
      this.paymentError = 'Lütfen firma ve abonelik planı seçin.';
      return;
    }

    const planPrice = this.selectedPlanForPayment.aylikUcret ?? this.selectedPlanForPayment.AylikUcret ?? 0;
    if (planPrice <= 0) {
      this.paymentError = 'Seçilen plan için ödeme gerekmiyor.';
      return;
    }

    if (!this.paymentForm.cardHolder || !this.paymentForm.cardNumber || !this.paymentForm.expiryMonth || !this.paymentForm.expiryYear || !this.paymentForm.cvv) {
      this.paymentError = 'Lütfen tüm kart bilgilerini eksiksiz doldurun.';
      return;
    }

    const sanitizedCardNumber = this.paymentForm.cardNumber.replace(/\s+/g, '');
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    const payload = {
      tenantId: this.selectedFirma.id,
      planId: this.selectedPlanForPayment.id || this.selectedPlanForPayment.Id,
      tutar: planPrice,
      paraBirimi: 'TRY',
      taksitSayisi: this.paymentForm.installment,
      kartNumarasi: sanitizedCardNumber,
      sonKullanmaAy: this.paymentForm.expiryMonth,
      sonKullanmaYil: this.paymentForm.expiryYear,
      cvv: this.paymentForm.cvv,
      kartSahibi: this.paymentForm.cardHolder,
      ucDSecure: true
    };

    this.paymentProcessing = true;
    this.paymentError = null;

    this.http.post(`${environment.apiUrl}/virtual-pos/charge`, payload, { headers })
      .subscribe({
        next: (response: any) => {
          this.paymentProcessing = false;
          this.paymentReference = response?.reference;
          this.paymentSummary = response;
          this.showPaymentModal = false;

          if (response?.message) {
            alert(`💳 ${response.message}\n\nReferans: ${response.reference}\nBanka: ${response.bankName || 'Bilinmiyor'}\nTutar: ${response.chargedAmount} ${response.currency}`);
          } else {
            alert('Ödeme başarılı!');
          }

          this.assignSubscription(true);
        },
        error: (err) => {
          this.paymentProcessing = false;
          const errorMessage = err.error?.message || err.error?.detail || err.error?.Message || 'Ödeme işlenirken bir hata oluştu.';
          this.paymentError = errorMessage;
          console.error('Ödeme başarısız:', err);
        }
      });
  }
  
  closeAdminCredentialsModal(): void {
    this.showAdminCredentialsModal = false;
    this.adminCredentials = null;
    this.loadFirmalar(); // Firmaları yeniden yükle
  }
  
  copyToClipboard(text: string, type: string): void {
    if (!text) return;
    
    navigator.clipboard.writeText(text).then(() => {
      alert(`${type} kopyalandı: ${text}`);
    }).catch(err => {
      console.error('Kopyalama hatası:', err);
      alert('Kopyalama başarısız!');
    });
  }
  
  generatePasswordForFirma(firma: any): void {
    const firmaAdi = firma.firmaAdi || firma.FirmaAdi || 'Firma';
    
    if (!confirm(`${firmaAdi} için admin kullanıcısının şifresini yeniden üretmek istediğinize emin misiniz?`)) {
      return;
    }
    
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };
    
    this.http.post(`${environment.apiUrl}/tenants/${firma.id}/generate-admin-password`, {}, { headers })
      .subscribe({
        next: (response: any) => {
          if (response?.adminUser) {
            this.adminCredentials = {
              username: response.adminUser.username,
              email: response.adminUser.email,
              password: response.adminUser.password
            };
            this.showAdminCredentialsModal = true;
            // Parolayı otomatik olarak panoya kopyala
            setTimeout(() => {
              this.copyPasswordToClipboard();
            }, 500);
          } else {
            alert('Parola üretilemedi!');
          }
        },
        error: (err) => {
          console.error('Parola üretilemedi:', err);
          const errorMessage = err.error?.detail || err.error?.message || 'Parola üretilirken bir hata oluştu!';
          alert(errorMessage);
        }
      });
  }
  
  copyPasswordToClipboard(): void {
    if (!this.adminCredentials?.password) return;
    
    navigator.clipboard.writeText(this.adminCredentials.password).then(() => {
      // Başarı mesajı göster (toast veya bildirim)
      const toast = document.createElement('div');
      toast.className = 'position-fixed top-0 start-50 translate-middle-x mt-3 alert alert-success alert-dismissible fade show';
      toast.style.zIndex = '9999';
      toast.innerHTML = `
        <strong>✓ Parola kopyalandı!</strong> ${this.adminCredentials?.password}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
      `;
      document.body.appendChild(toast);
      setTimeout(() => {
        toast.remove();
      }, 3000);
    }).catch(err => {
      console.error('Parola kopyalama hatası:', err);
      alert('Parola kopyalanamadı! Lütfen manuel olarak kopyalayın.');
    });
  }
  
  copyAllCredentials(): void {
    if (!this.adminCredentials) return;
    
    const credentialsText = `Kullanıcı Adı: ${this.adminCredentials.username}\nE-posta: ${this.adminCredentials.email}\nŞifre: ${this.adminCredentials.password}`;
    
    navigator.clipboard.writeText(credentialsText).then(() => {
      const toast = document.createElement('div');
      toast.className = 'position-fixed top-0 start-50 translate-middle-x mt-3 alert alert-success alert-dismissible fade show';
      toast.style.zIndex = '9999';
      toast.innerHTML = `
        <strong>✓ Tüm bilgiler kopyalandı!</strong>
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
      `;
      document.body.appendChild(toast);
      setTimeout(() => {
        toast.remove();
      }, 3000);
    }).catch(err => {
      console.error('Kopyalama hatası:', err);
      alert('Bilgiler kopyalanamadı!');
    });
  }

  sendingEmail: { [key: number]: boolean } = {};

  resendLicenseEmail(firma: any): void {
    const firmaAdi = firma.firmaAdi || firma.FirmaAdi || 'Firma';
    const firmaId = firma.id;
    
    if (!firma.email && !firma.Email) {
      alert(`${firmaAdi} için e-posta adresi bulunamadı!`);
      return;
    }

    if (!confirm(`${firmaAdi} firmasına lisans anahtarı email'i gönderilsin mi?`)) {
      return;
    }

    this.sendingEmail[firmaId] = true;
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    this.http.post(`${environment.apiUrl}/tenants/${firmaId}/resend-license-email`, {}, { headers })
      .subscribe({
        next: (response: any) => {
          this.sendingEmail[firmaId] = false;
          alert(`✅ Lisans anahtarı email'i başarıyla gönderildi!\n\nAlıcı: ${firma.email || firma.Email}`);
        },
        error: (err) => {
          this.sendingEmail[firmaId] = false;
          console.error('Email gönderilemedi:', err);
          const errorMessage = err.error?.detail || err.error?.message || 'Email gönderilirken bir hata oluştu!';
          alert(`❌ ${errorMessage}`);
        }
      });
  }
}

