import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from 'src/app/services/auth.service';
import { NgbModal, NgbModalConfig } from '@ng-bootstrap/ng-bootstrap';

interface Cihaz {
  id: number;
  cihaz_adi: string;
  cihaz_tipi: 'yazici' | 'mikrofon';
  marka: string;
  model: string;
  baglant_tipi: 'usb' | 'bluetooth' | 'wifi';
  durum: 'aktif' | 'pasif';
  created_at: string;
}

@Component({
  selector: 'app-cihazlar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './cihazlar.component.html',
  styleUrls: ['./cihazlar.component.scss'],
  providers: [NgbModalConfig, NgbModal]
})
export class CihazlarComponent implements OnInit {
  cihazlar: Cihaz[] = [];
  filteredCihazlar: Cihaz[] = [];
  searchTerm: string = '';
  tipFilter: string = 'tumu';
  
  // Modal için
  currentCihaz: any = null;
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
    this.cihazlariYukle();
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

  cihazlariYukle(): void {
    this.http.get<Cihaz[]>(`${this.apiUrl}/cihazlar`, { headers: this.headers })
      .subscribe({
        next: (data) => {
          this.cihazlar = data;
          this.filteredCihazlar = data;
        },
        error: (err) => {
          console.error('Cihazlar yüklenemedi:', err);
          this.cihazlar = [];
          this.filteredCihazlar = [];
        }
      });
  }

  search(): void {
    let filtered = this.cihazlar;

    // Tip filtresi
    if (this.tipFilter !== 'tumu') {
      filtered = filtered.filter(c => c.cihaz_tipi === this.tipFilter);
    }

    // Arama
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(c => 
        c.cihaz_adi.toLowerCase().includes(term) ||
        c.marka.toLowerCase().includes(term) ||
        c.model.toLowerCase().includes(term) ||
        c.baglant_tipi.toLowerCase().includes(term)
      );
    }

    this.filteredCihazlar = filtered;
  }

  getDurumBadgeClass(durum: string): string {
    return durum === 'aktif' ? 'success' : 'secondary';
  }

  getDurumLabel(durum: string): string {
    return durum === 'aktif' ? 'Aktif' : 'Pasif';
  }

  getTipLabel(tip: string): string {
    return tip === 'yazici' ? 'Yazıcı' : 'Mikrofon';
  }

  getBaglantiLabel(tip: string): string {
    const labels: any = {
      'usb': 'USB',
      'bluetooth': 'Bluetooth',
      'wifi': 'WiFi'
    };
    return labels[tip] || tip;
  }

  yeniCihaz(content: any): void {
    this.currentCihaz = {
      cihaz_adi: '',
      cihaz_tipi: 'yazici',
      marka: '',
      model: '',
      baglant_tipi: 'usb',
      durum: 'aktif'
    };
    this.isEditMode = false;
    this.modalService.open(content, { size: 'lg' });
  }

  duzenle(cihaz: Cihaz, content: any): void {
    this.currentCihaz = { ...cihaz };
    this.isEditMode = true;
    this.modalService.open(content, { size: 'lg' });
  }

  kaydet(content: any): void {
    if (!this.currentCihaz.cihaz_adi || !this.currentCihaz.marka || !this.currentCihaz.model) {
      alert('Cihaz adı, marka ve model zorunludur');
      return;
    }

    if (this.isEditMode) {
      const url = `${this.apiUrl}/cihazlar/${this.currentCihaz.id}`;
      this.http.put<Cihaz>(url, this.currentCihaz, { headers: this.headers })
        .subscribe({
          next: (data) => {
            alert('Cihaz kaydedildi');
            this.modalService.dismissAll();
            this.cihazlariYukle();
          },
          error: (err) => {
            const errorMsg = err.error?.detail || err.message || 'Bilinmeyen hata';
            alert('Hata: ' + errorMsg);
            console.error('Cihaz kaydedilemedi:', err);
          }
        });
    } else {
      const url = `${this.apiUrl}/cihazlar`;
      this.http.post<Cihaz>(url, this.currentCihaz, { headers: this.headers })
        .subscribe({
          next: (data) => {
            alert('Cihaz kaydedildi');
            this.modalService.dismissAll();
            this.cihazlariYukle();
          },
          error: (err) => {
            const errorMsg = err.error?.detail || err.message || 'Bilinmeyen hata';
            alert('Hata: ' + errorMsg);
            console.error('Cihaz kaydedilemedi:', err);
          }
        });
    }
  }

  sil(id: number): void {
    if (!confirm('Bu cihazı silmek istediğinizden emin misiniz?')) {
      return;
    }

    this.http.delete(`${this.apiUrl}/cihazlar/${id}`, { headers: this.headers })
      .subscribe({
        next: () => {
          alert('Cihaz silindi');
          this.cihazlariYukle();
        },
        error: (err) => {
          const errorMsg = err.error?.detail || err.message || 'Bilinmeyen hata';
          alert('Hata: ' + errorMsg);
        }
      });
  }


  tespitEt(): void {
    if (!confirm('Anlık cihaz tespiti yapılsın mı?')) {
      return;
    }

    // Gerçek cihaz tespiti için API çağrısı
    this.http.get<any[]>(`${this.apiUrl}/cihazlar/detect`, { headers: this.headers })
      .subscribe({
        next: (detectedDevices) => {
          if (detectedDevices.length === 0) {
            alert('Tespit edilen cihaz bulunamadı. Manuel ekleme yapınız.');
            return;
          }

          let added = 0;
          detectedDevices.forEach(device => {
            // Mevcut cihazlar listesinde var mı kontrol et
            const exists = this.cihazlar.some(c => c.cihaz_adi === device.name);
            
            if (!exists) {
              // Cihaz adından marka ve model çıkarmaya çalış
              const nameParts = device.name.split(' ');
              const marka = nameParts.length > 0 ? nameParts[0] : 'Bilinmiyor';
              const model = nameParts.length > 1 ? nameParts.slice(1).join(' ') : 'Bilinmiyor';

              const newCihaz = {
                cihaz_adi: device.name,
                cihaz_tipi: device.device_type || 'mikrofon',
                marka: marka,
                model: model,
                baglant_tipi: device.connection_type || 'usb',
                durum: 'aktif'
              };

              this.http.post<Cihaz>(`${this.apiUrl}/cihazlar`, newCihaz, { headers: this.headers })
                .subscribe({
                  next: () => added++,
                  error: (err) => console.error('Cihaz eklenemedi:', err)
                });
            }
          });

          setTimeout(() => {
            if (added > 0) {
              alert(`${added} yeni cihaz tespit edildi ve eklendi!`);
              this.cihazlariYukle();
            } else {
              alert('Tüm cihazlar zaten eklenmiş veya yeni cihaz bulunamadı.');
              this.cihazlariYukle();
            }
          }, 1500);
        },
        error: (err) => {
          console.error('Cihaz tespiti hatası:', err);
          alert('Cihaz tespiti yapılamadı. Manuel ekleme yapınız.');
        }
      });
  }
}

