import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';
import { DeviceFingerprintService } from '../../services/device-fingerprint.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-license-activation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './license-activation.component.html',
  styleUrls: ['./license-activation.component.scss']
})
export class LicenseActivationComponent implements OnInit {
  licenseJson: string = '';
  signature: string = '';
  deviceFingerprint: string = '';
  loading = false;
  error: string | null = null;
  success: boolean = false;
  activationResult: any = null;
  licenseDetails: any = null;
  
  licenseKey: string = '';

  constructor(
    private http: HttpClient,
    private router: Router,
    private authService: AuthService,
    private fingerprintService: DeviceFingerprintService
  ) {}

  async ngOnInit(): Promise<void> {
    // Check if already authenticated
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/auth/login'], { replaceUrl: true });
      return;
    }
    
    // Check if license already validated
    if (this.authService.isLicenseValidated()) {
      this.router.navigate(['/dashboard'], { replaceUrl: true });
      return;
    }
    
    // Compute device fingerprint
    await this.loadDeviceFingerprint();
  }

  async loadDeviceFingerprint(): Promise<void> {
    try {
      this.deviceFingerprint = await this.fingerprintService.computeFingerprint();
    } catch (error) {
      console.error('Error computing fingerprint:', error);
      this.error = 'Cihaz fingerprint hesaplanamadı!';
    }
  }

  async activateLicense(): Promise<void> {
    if (!this.licenseJson || !this.signature) {
      this.error = 'Lisans bilgileri alınamadı. Lütfen tekrar deneyin.';
      return;
    }

    if (!this.deviceFingerprint) {
      await this.loadDeviceFingerprint();
      if (!this.deviceFingerprint) {
        this.error = 'Cihaz fingerprint hesaplanamadı!';
        return;
      }
    }

    this.loading = true;
    this.error = null;
    this.success = false;

    try {
      const requestBody = {
        licenseJson: this.licenseJson,
        signature: this.signature,
        deviceFingerprint: this.deviceFingerprint,
        deviceName: navigator.userAgent,
        ipAddress: null // Will be captured server-side
      };

      let headers = new HttpHeaders({ 'Content-Type': 'application/json' });
      const token = this.authService.getToken();
      if (token) {
        headers = headers.set('Authorization', `Bearer ${token}`);
      }

      const response = await firstValueFrom(
        this.http.post(`${environment.apiUrl}/licenses/activate`, requestBody, { headers })
      );

      const tenantId = this.extractTenantId();
      this.loading = false;
      this.success = true;
      this.activationResult = response;
      this.licenseDetails = null;
      this.licenseJson = '';
      this.signature = '';
      this.licenseKey = '';
      if (tenantId !== null) {
        this.authService.markLicenseValidated(undefined, tenantId);
      } else {
        this.authService.markLicenseValidated();
      }

      // Sunucudan kullanıcıyı/tenantId'yi tazele ve bayrağı kalıcılaştır
      try {
        await this.authService.ensureLicenseFlagFromServer();
      } catch {}

      // Başarılı aktivasyon sonrası doğrudan dashboard'a yönlendir
      this.router.navigate(['/dashboard'], { replaceUrl: true });
    } catch (error: any) {
      this.loading = false;
      const detail = error?.error?.detail || error?.error?.message || error?.message;
      this.error = detail || 'Lisans aktivasyonu sırasında bir hata oluştu!';
      
      if (error?.error?.requiresRebind) {
        this.error += '\n\nMaksimum kurulum sayısına ulaşıldı. Lütfen yöneticinizle iletişime geçin.';
      }
    }
  }

  async fetchLicenseFromServer(): Promise<void> {
    if (!this.licenseKey) {
      this.error = 'Lisans anahtarı gerekli!';
      return;
    }

    this.licenseDetails = null;
    this.loading = true;
    this.error = null;

    try {
      const trimmedKey = this.licenseKey.trim();
      let headers = new HttpHeaders({ 'Content-Type': 'application/json' });
      const token = this.authService.getToken();
      if (token) {
        headers = headers.set('Authorization', `Bearer ${token}`);
      }

      // First validate license key
      const validateResponse: any = await firstValueFrom(
        this.http.post(`${environment.apiUrl}/tenants/validate-license`, {
          licenseKey: trimmedKey
        }, { headers })
      );

      if (!validateResponse?.valid) {
        this.error = validateResponse?.detail || 'Geçersiz lisans anahtarı!';
        this.loading = false;
        return;
      }

      // Fetch license payload from server
      const licensePayload: any = await firstValueFrom(
        this.http.post(`${environment.apiUrl}/licenses/by-key`, { licenseKey: trimmedKey }, { headers })
      );

      this.licenseJson = licensePayload?.licenseJson || '';
      this.signature = licensePayload?.signature || '';
      this.licenseDetails = licensePayload;
      this.loading = false;
      const currentUser = this.authService.getUser();
      // Eski katı kontrol kaldırıldı: Sunucu zaten lisansın geçerliliğini ve bağlamını doğruluyor.
      // Eğer farklı firmaya aitse, backend aktivasyon aşamasında hata dönecektir.
      if (!this.licenseJson || !this.signature) {
        this.error = 'Lisans bilgileri alınamadı. Lütfen yöneticinizle iletişime geçin.';
        return;
      }

      await this.activateLicense();
    } catch (error: any) {
      this.loading = false;
      const detail = error?.error?.detail || error?.error?.message || error?.message;
      this.error = detail || 'Lisans doğrulanırken bir hata oluştu!';
    }
  }

  private extractTenantId(): number | null {
    if (this.licenseDetails?.tenantId) {
      return this.licenseDetails.tenantId;
    }
    const user = this.authService.getUser();
    return user?.tenantId ?? null;
  }

  // Legacy method kept for compatibility with previous template bindings.
  toggleManualEntry(): void {
    // No-op: manual license upload flow has been removed.
  }

  formatFingerprint(fingerprint: string): string {
    return this.fingerprintService.formatFingerprint(fingerprint);
  }

  copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text).then(() => {
      alert('Kopyalandı: ' + text);
    }).catch(err => {
      console.error('Kopyalama hatası:', err);
    });
  }
  
  getDeviceInfo(): string {
    if (typeof navigator !== 'undefined' && navigator.userAgent) {
      return navigator.userAgent;
    }
    return 'Unknown Device';
  }
}

