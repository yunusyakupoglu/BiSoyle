import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-license-key',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './license-key.component.html',
  styleUrls: ['./license-key.component.scss']
})
export class LicenseKeyComponent {
  licenseKey: string = '';
  hataMesaji: string = '';
  yukleniyor: boolean = false;

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    // LocalStorage'da license key validation yapılmış mı kontrol et
    const licenseValidated = localStorage.getItem('_BISOYLE_LICENSE_VALIDATED_');
    if (licenseValidated === 'true') {
      // Zaten validate edilmişse login sayfasına yönlendir
      this.router.navigate(['/auth/login']);
    }
  }

  validateLicense() {
    if (!this.licenseKey || this.licenseKey.trim().length === 0) {
      this.hataMesaji = 'Lisans anahtarı gerekli';
      return;
    }

    // Format kontrolü: XXXX-XXXX-XXXX-XXXX
    const licensePattern = /^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$/;
    const formattedKey = this.licenseKey.toUpperCase().trim();
    
    if (!licensePattern.test(formattedKey)) {
      this.hataMesaji = 'Lisans anahtarı formatı hatalı (Örnek: XXXX-XXXX-XXXX-XXXX)';
      return;
    }

    this.yukleniyor = true;
    this.hataMesaji = '';

    this.http.post<{
      valid: boolean;
      tenantId: number;
      tenantKey: string;
      firmaAdi: string;
      maxBayiSayisi: number;
      message: string;
    }>(`${environment.apiUrl}/tenants/validate-license`, {
      licenseKey: formattedKey
    }).subscribe({
      next: (response) => {
        this.yukleniyor = false;
        if (response.valid) {
          // License key geçerli, localStorage'a kaydet
          localStorage.setItem('_BISOYLE_LICENSE_VALIDATED_', 'true');
          localStorage.setItem('_BISOYLE_TENANT_ID_', response.tenantId.toString());
          localStorage.setItem('_BISOYLE_TENANT_KEY_', response.tenantKey);
          localStorage.setItem('_BISOYLE_FIRMA_ADI_', response.firmaAdi);
          localStorage.setItem('_BISOYLE_MAX_BAYI_', response.maxBayiSayisi.toString());
          
          // Login sayfasına yönlendir
          this.router.navigate(['/auth/login']);
        } else {
          this.hataMesaji = response.message || 'Geçersiz lisans anahtarı';
        }
      },
      error: (err) => {
        this.yukleniyor = false;
        this.hataMesaji = err.error?.detail || 'Lisans anahtarı doğrulanamadı. Lütfen tekrar deneyin.';
      }
    });
  }

  formatLicenseKey(event: any) {
    let value = event.target.value.replace(/[^A-Z0-9]/gi, '').toUpperCase();
    
    // Format: XXXX-XXXX-XXXX-XXXX
    if (value.length > 16) {
      value = value.substring(0, 16);
    }
    
    // Tire ekle
    let formatted = '';
    for (let i = 0; i < value.length; i++) {
      if (i > 0 && i % 4 === 0) {
        formatted += '-';
      }
      formatted += value[i];
    }
    
    this.licenseKey = formatted;
  }
}

