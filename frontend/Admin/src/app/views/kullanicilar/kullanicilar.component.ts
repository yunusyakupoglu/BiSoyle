import { Component, CUSTOM_ELEMENTS_SCHEMA, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from 'src/app/services/auth.service';
import { NgbModal, NgbModalConfig } from '@ng-bootstrap/ng-bootstrap';

interface Kullanici {
  id: number;
  ad: string;
  soyad: string;
  kullanici_adi: string;
  rol_id: number;
}

interface Rol {
  id: number;
  rol_adi: string;
}

@Component({
  selector: 'app-kullanicilar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './kullanicilar.component.html',
  styleUrls: ['./kullanicilar.component.scss'],
  providers: [NgbModalConfig, NgbModal]
})
export class KullanicilarComponent implements OnInit {
  kullanicilar: Kullanici[] = [];
  roller: Rol[] = [];
  filteredKullanicilar: Kullanici[] = [];
  searchTerm: string = '';
  
  // Modal için
  currentKullanici: any = null;
  isEditMode: boolean = false;
  
  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private modalService: NgbModal,
    config: NgbModalConfig
  ) {
    config.backdrop = 'static';
    config.keyboard = false;
  }

  ngOnInit(): void {
    this.yukleKullanicilar();
    this.yukleRoller();
  }

  get apiUrl() {
    return 'http://localhost:8000';
  }

  get headers() {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  yukleKullanicilar(): void {
    this.http.get<Kullanici[]>(`${this.apiUrl}/kullanicilar`, { headers: this.headers })
      .subscribe({
        next: (data) => {
          this.kullanicilar = data;
          this.filteredKullanicilar = data;
        },
        error: (err) => console.error('Kullanıcılar yüklenemedi:', err)
      });
  }

  yukleRoller(): void {
    this.http.get<Rol[]>(`${this.apiUrl}/roller`, { headers: this.headers })
      .subscribe({
        next: (data) => {
          this.roller = data;
        },
        error: (err) => console.error('Roller yüklenemedi:', err)
      });
  }

  search(): void {
    if (!this.searchTerm) {
      this.filteredKullanicilar = this.kullanicilar;
      return;
    }
    
    const term = this.searchTerm.toLowerCase();
    this.filteredKullanicilar = this.kullanicilar.filter(k => 
      k.kullanici_adi.toLowerCase().includes(term) ||
      k.ad.toLowerCase().includes(term) ||
      k.soyad.toLowerCase().includes(term)
    );
  }

  getRolAdi(rolId: number): string {
    const rol = this.roller.find(r => r.id === rolId);
    return rol ? rol.rol_adi : 'Bilinmiyor';
  }

  edit(kullanici: Kullanici, content: any): void {
    this.currentKullanici = { ...kullanici };
    this.isEditMode = true;
    this.modalService.open(content, { size: 'lg' });
  }

  newUser(content: any): void {
    this.currentKullanici = {
      ad: '',
      soyad: '',
      kullanici_adi: '',
      parola: '',
      rol_id: 2
    };
    this.isEditMode = false;
    this.modalService.open(content, { size: 'lg' });
  }

  save(content: any): void {
    if (!this.currentKullanici.kullanici_adi || !this.currentKullanici.ad) {
      alert('Kullanıcı adı ve ad zorunludur');
      return;
    }

    if (!this.isEditMode && !this.currentKullanici.parola) {
      alert('Şifre zorunludur');
      return;
    }

    if (this.isEditMode) {
      // Update existing user
      const url = `${this.apiUrl}/kullanicilar/${this.currentKullanici.id}`;
      const body = {
        ad: this.currentKullanici.ad,
        soyad: this.currentKullanici.soyad,
        kullanici_adi: this.currentKullanici.kullanici_adi,
        parola: this.currentKullanici.parola || '',
        rol_id: this.currentKullanici.rol_id
      };

      this.http.put<Kullanici>(url, body, { headers: this.headers })
        .subscribe({
          next: (data) => {
            alert('Kullanıcı güncellendi');
            this.modalService.dismissAll();
            this.yukleKullanicilar();
          },
          error: (err) => {
            const errorMsg = err.error?.detail || err.message || 'Bilinmeyen hata';
            alert('Hata: ' + errorMsg);
            console.error('Update error:', err);
          }
        });
    } else {
      // Create new user
      const url = `${this.apiUrl}/auth/register`;
      const body = {
        ad: this.currentKullanici.ad,
        soyad: this.currentKullanici.soyad,
        kullanici_adi: this.currentKullanici.kullanici_adi,
        parola: this.currentKullanici.parola,
        rol_id: this.currentKullanici.rol_id
      };

      this.http.post<Kullanici>(url, body, { headers: this.headers })
        .subscribe({
          next: (data) => {
            alert('Kullanıcı oluşturuldu');
            this.modalService.dismissAll();
            this.yukleKullanicilar();
          },
          error: (err) => {
            const errorMsg = err.error?.detail || err.message || 'Bilinmeyen hata';
            alert('Hata: ' + errorMsg);
            console.error('Create error:', err);
          }
        });
    }
  }

  delete(id: number): void {
    if (!confirm('Bu kullanıcıyı silmek istediğinizden emin misiniz?')) {
      return;
    }

    this.http.delete(`${this.apiUrl}/kullanicilar/${id}`, { headers: this.headers })
      .subscribe({
        next: () => {
          alert('Kullanıcı silindi');
          this.yukleKullanicilar();
        },
        error: (err) => {
          alert('Hata: ' + (err.error?.detail || 'Bilinmeyen hata'));
        }
      });
  }
}

