import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { AuthService } from 'src/app/services/auth.service';

interface LicenseKeyInfo {
  id: number;
  firmaAdi: string;
  tenantKey: string;
  email: string;
  telefon?: string;
  licenseKey?: string;
  licenseKeyGirisTarihi?: string;
  aktif: boolean;
  aktifAbonelikId?: number;
  subscriptions?: Array<{
    id: number;
    planId: number;
    plan?: {
      id: number;
      planAdi: string;
      maxKullaniciSayisi: number;
      maxBayiSayisi: number;
      aylikUcret: number;
    };
    baslangicTarihi: string;
    bitisTarihi?: string;
    aktif: boolean;
  }>;
}

@Component({
  selector: 'app-lisans-anahtarlari',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './lisans-anahtarlari.component.html',
  styleUrls: ['./lisans-anahtarlari.component.scss']
})
export class LisansAnahtarlariComponent implements OnInit {
  licenseKeys: LicenseKeyInfo[] = [];
  loading = false;
  error: string | null = null;
  searchTerm: string = '';
  
  // SuperAdmin kontrolü için property (getter yerine, template'de kullanılacak)
  isSuperAdmin: boolean = false;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {
    // Constructor'da SuperAdmin kontrolü yap
    this.isSuperAdmin = this.checkSuperAdmin();
  }
  
  private checkSuperAdmin(): boolean {
    const user = this.authService.getUser();
    if (!user) return false;
    const hasSuperAdminRole = user.roles && user.roles.includes('SuperAdmin');
    const isSuperAdminTenant = !user.tenantId || user.tenantId === 0;
    return isSuperAdminTenant || hasSuperAdminRole || false;
  }

  ngOnInit(): void {
    if (this.isSuperAdmin) {
      this.loadLicenseKeys();
    } else {
      this.error = 'Bu sayfaya erişim yetkiniz yok.';
    }
  }

  loadLicenseKeys(): void {
    this.loading = true;
    this.error = null;
    
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.get<LicenseKeyInfo[]>(`${environment.apiUrl}/tenants`, { headers })
      .subscribe({
        next: (data) => {
          this.licenseKeys = data;
          this.loading = false;
        },
        error: (err) => {
          console.error('Lisans anahtarları yüklenemedi:', err);
          this.error = err.error?.detail || err.error?.message || 'Lisans anahtarları yüklenirken bir hata oluştu!';
          this.loading = false;
        }
      });
  }

  getFilteredLicenseKeys(): LicenseKeyInfo[] {
    if (!this.searchTerm) {
      return this.licenseKeys;
    }
    
    const search = this.searchTerm.toLowerCase();
    return this.licenseKeys.filter(key => 
      key.firmaAdi?.toLowerCase().includes(search) ||
      key.email?.toLowerCase().includes(search) ||
      key.licenseKey?.toLowerCase().includes(search) ||
      key.tenantKey?.toLowerCase().includes(search)
    );
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

  getActiveSubscription(licenseKey: LicenseKeyInfo): any {
    if (!licenseKey.subscriptions || licenseKey.subscriptions.length === 0) {
      return null;
    }
    
    return licenseKey.subscriptions.find(s => s.aktif) || licenseKey.subscriptions[0];
  }

  formatDate(dateString?: string): string {
    if (!dateString) return '-';
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('tr-TR', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return dateString;
    }
  }

  // İstatistikler için computed properties (template'de arrow function kullanmamak için)
  get totalFirmalarCount(): number {
    return this.licenseKeys.length;
  }

  get lisansliFirmaCount(): number {
    return this.licenseKeys.filter(key => key.licenseKey && key.licenseKey.length > 0).length;
  }

  get ilkGirisYapanCount(): number {
    return this.licenseKeys.filter(key => key.licenseKeyGirisTarihi != null).length;
  }

  get aktifFirmaCount(): number {
    return this.licenseKeys.filter(key => key.aktif === true).length;
  }
}

