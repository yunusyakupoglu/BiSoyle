import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from 'src/app/services/auth.service';
import { LogoBoxComponent } from 'src/app/components/logo-box.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LogoBoxComponent],
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
    // Eğer zaten giriş yapmışsa ana sayfaya yönlendir
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/speakerid']);
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
      next: (response) => {
        this.yukleniyor = false;
        // Login başarılı, Speaker ID sayfasına yönlendir
        this.router.navigate(['/speakerid']);
      },
      error: (err) => {
        this.yukleniyor = false;
        this.hataMesaji = err.error?.detail || 'Giriş başarısız. Kullanıcı adı veya şifre hatalı.';
      }
    });
  }
}

