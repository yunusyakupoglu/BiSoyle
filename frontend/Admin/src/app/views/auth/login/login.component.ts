import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  kullaniciAdi: string = '';
  parola: string = '';
  hataMesaji: string = '';
  yukleniyor: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    // Eğer zaten giriş yapmışsa kontrol et
    if (this.authService.isAuthenticated()) {
      this.handleAlreadyLoggedIn();
    }
  }

  async handleAlreadyLoggedIn(): Promise<void> {
    const user = this.authService.getUser();
    const isSuperAdmin = this.authService.isSuperAdmin();
    await this.authService.ensureLicenseFlagFromServer();
    const licenseValidated = this.authService.isLicenseValidated();

    if (isSuperAdmin || licenseValidated) {
      this.router.navigate(['/dashboard']);
    } else {
      this.router.navigate(['/license-activation']);
    }
  }

  girisYap() {
    if (!this.kullaniciAdi || !this.parola) {
      this.hataMesaji = 'Kullanıcı adı ve şifre gerekli';
      return;
    }

    this.yukleniyor = true;
    this.hataMesaji = '';

    this.authService.login({
      email: this.kullaniciAdi,
      password: this.parola
    }).subscribe({
      next: async (response) => {
        this.yukleniyor = false;
        
        // Kullanıcı bilgilerini kontrol et
        const user = response.user;
        const isSuperAdmin = !user.tenantId || user.tenantId === 0 || 
                           (user.roles && user.roles.includes('SuperAdmin'));
        await this.authService.ensureLicenseFlagFromServer();
        const licenseValidated = this.authService.isLicenseValidated();
        
        // SuperAdmin değilse ve lisans doğrulanmamışsa license activation'a yönlendir
        if (!isSuperAdmin && !licenseValidated) {
          // License activation sayfasına yönlendir (device binding ile tam aktivasyon)
          this.router.navigate(['/license-activation']);
          return;
        }
        
        // SuperAdmin veya lisans doğrulanmışsa dashboard'a yönlendir
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.yukleniyor = false;
        this.hataMesaji = err.error?.detail || 'Giriş başarısız. Kullanıcı adı veya şifre hatalı.';
      }
    });
  }
}

