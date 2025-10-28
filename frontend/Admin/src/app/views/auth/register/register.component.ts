import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent {
  ad: string = '';
  soyad: string = '';
  kullaniciAdi: string = '';
  parola: string = '';
  parolaTekrar: string = '';
  
  hataMesaji: string = '';
  basariMesaji: string = '';
  yukleniyor: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  kayitOl() {
    // Validation
    if (!this.ad || !this.soyad || !this.kullaniciAdi || !this.parola) {
      this.hataMesaji = 'Tüm alanları doldurun';
      return;
    }

    if (this.parola !== this.parolaTekrar) {
      this.hataMesaji = 'Şifreler eşleşmiyor';
      return;
    }

    if (this.parola.length < 6) {
      this.hataMesaji = 'Şifre en az 6 karakter olmalı';
      return;
    }

    this.yukleniyor = true;
    this.hataMesaji = '';
    this.basariMesaji = '';

    this.authService.register({
      ad: this.ad,
      soyad: this.soyad,
      kullanici_adi: this.kullaniciAdi,
      parola: this.parola,
      rol_id: 2  // Normal kullanıcı
    }).subscribe({
      next: (response) => {
        this.yukleniyor = false;
        this.basariMesaji = 'Kayıt başarılı! Giriş sayfasına yönlendiriliyorsunuz...';
        setTimeout(() => {
          this.router.navigate(['/auth/login']);
        }, 2000);
      },
      error: (err) => {
        this.yukleniyor = false;
        this.hataMesaji = err.error?.detail || 'Kayıt başarısız. Kullanıcı adı zaten mevcut olabilir.';
      }
    });
  }
}

