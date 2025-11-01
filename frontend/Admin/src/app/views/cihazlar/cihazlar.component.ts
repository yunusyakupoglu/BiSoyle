import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '@/app/services/auth.service';

interface Device {
  id: number;
  tenantId: number;
  cihazAdi: string;
  cihazTipi: 'yazici' | 'mikrofon';
  marka: string;
  model: string;
  baglantiTipi: 'usb' | 'bluetooth' | 'wifi';
  durum: 'aktif' | 'pasif';
  olusturmaTarihi: string;
}

@Component({
  selector: 'app-cihazlar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cihazlar.component.html',
  styleUrls: ['./cihazlar.component.scss']
})
export class CihazlarComponent implements OnInit {
  devices: Device[] = [];
  loading = false;
  error: string | null = null;
  
  // Modal state
  showModal = false;
  editingDevice: Device | null = null;
  saving = false;
  
  // Discovery state
  testingDeviceId: number | null = null; // For device testing
  scanning = false;
  
  // Form data
  formData = {
    tenantId: null as number | null,
    cihazAdi: '',
    cihazTipi: 'yazici' as 'yazici' | 'mikrofon',
    marka: '',
    model: '',
    baglantiTipi: 'usb' as 'usb' | 'bluetooth' | 'wifi',
    durum: 'aktif' as 'aktif' | 'pasif'
  };

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadDevices();
  }

  loadDevices(): void {
    this.loading = true;
    this.error = null;

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.get<Device[]>(`${environment.apiUrl}/devices`, { headers })
      .subscribe({
        next: (data) => {
          this.devices = data;
          this.loading = false;
        },
        error: (err) => {
          console.error('Cihazlar yüklenemedi:', err);
          this.error = 'Cihazlar yüklenirken bir hata oluştu.';
          this.loading = false;
        }
      });
  }

  openCreateModal(): void {
    const currentUser = this.authService.getUser();
    this.editingDevice = null;
    this.formData = {
      tenantId: currentUser?.tenantId || null,
      cihazAdi: '',
      cihazTipi: 'yazici',
      marka: '',
      model: '',
      baglantiTipi: 'usb',
      durum: 'aktif'
    };
    this.showModal = true;
  }

  openEditModal(device: Device): void {
    this.editingDevice = device;
    this.formData = {
      tenantId: device.tenantId,
      cihazAdi: device.cihazAdi,
      cihazTipi: device.cihazTipi,
      marka: device.marka,
      model: device.model,
      baglantiTipi: device.baglantiTipi,
      durum: device.durum
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingDevice = null;
  }

  saveDevice(): void {
    if (!this.formData.cihazAdi || !this.formData.marka || !this.formData.model) {
      alert('Lütfen tüm zorunlu alanları doldurun!');
      return;
    }

    this.saving = true;
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    const url = this.editingDevice 
      ? `${environment.apiUrl}/devices/${this.editingDevice.id}`
      : `${environment.apiUrl}/devices`;

    const method = this.editingDevice ? 'put' : 'post';

    this.http.request(method, url, { headers, body: this.formData })
      .subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadDevices();
          alert(`✅ Cihaz ${this.editingDevice ? 'güncellendi' : 'eklendi'}!`);
        },
        error: (err) => {
          console.error('Cihaz kaydedilemedi:', err);
          alert(err.error?.message || 'Cihaz kaydedilirken bir hata oluştu!');
          this.saving = false;
        }
      });
  }

  toggleStatus(device: Device): void {
    if (!confirm(`${device.cihazAdi} cihazını ${device.durum === 'aktif' ? 'pasif' : 'aktif'} etmek istediğinize emin misiniz?`)) {
      return;
    }

    const headers = { 'Authorization': `Bearer ${this.authService.getToken()}` };
    this.http.put(`${environment.apiUrl}/devices/${device.id}/toggle-status`, {}, { headers })
      .subscribe({
        next: () => {
          this.loadDevices();
          alert(`✅ Cihaz durumu değiştirildi!`);
        },
        error: (err) => {
          console.error('Durum değiştirilemedi:', err);
          alert('Durum değiştirilirken bir hata oluştu!');
        }
      });
  }

  deleteDevice(device: Device): void {
    if (!confirm(`${device.cihazAdi} cihazını silmek istediğinize emin misiniz? (Pasif edilecek)`)) {
      return;
    }

    const headers = { 'Authorization': `Bearer ${this.authService.getToken()}` };
    this.http.delete(`${environment.apiUrl}/devices/${device.id}`, { headers })
      .subscribe({
        next: () => {
          this.loadDevices();
          alert(`✅ Cihaz pasif edildi!`);
        },
        error: (err) => {
          console.error('Cihaz silinemedi:', err);
          alert(err.error?.message || 'Cihaz silinirken bir hata oluştu!');
        }
      });
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

  getDurumBadgeClass(durum: string): string {
    return durum === 'aktif' ? 'success' : 'secondary';
  }

  // Otomatik Cihaz Tarama
  discoverDevices(): void {
    const currentUser = this.authService.getUser();
    if (!currentUser?.tenantId) {
      alert('Tenant bilgisi bulunamadı!');
      return;
    }

    if (!confirm('Bilgisayara bağlı tüm yazıcı ve mikrofonlar taranacak. Devam etmek istiyor musunuz?')) {
      return;
    }

    this.scanning = true;
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    this.http.post(`${environment.apiUrl.replace('/api/v1', '')}/api/devices/discover?tenantId=${currentUser.tenantId}`, {}, { headers })
      .subscribe({
        next: (response: any) => {
          this.scanning = false;
          alert(`✅ Cihaz Tarama Tamamlandı!\n\nBulunan: ${response.discovered}\nEklenen: ${response.added}\n\n${response.message}`);
          this.loadDevices(); // Refresh list
        },
        error: (err) => {
          console.error('Cihaz tarama hatası:', err);
          this.scanning = false;
          alert(err.error?.message || 'Cihaz taraması sırasında bir hata oluştu!');
        }
      });
  }

  // Bluetooth Cihaz Test
  testBluetoothDevice(device: Device): void {
    if (device.baglantiTipi !== 'bluetooth') {
      alert('Bu özellik sadece Bluetooth cihazları için kullanılabilir.');
      return;
    }

    if (!confirm(`${device.cihazAdi} Bluetooth cihazını test etmek istediğinize emin misiniz?`)) {
      return;
    }

    this.testingDeviceId = device.id;
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    this.http.post(`${environment.apiUrl.replace('/api/v1', '')}/api/devices/${device.id}/test`, {}, { headers })
      .subscribe({
        next: (response: any) => {
          this.testingDeviceId = null;
          const result = response.testResult;
          
          let message = `🔍 Cihaz Test Sonucu: ${device.cihazAdi}\n\n`;
          message += `${result.message}\n\n`;
          
          if (result.isDocker) {
            message += `⚠️ NOT: Docker container içinde Bluetooth test yapılamaz.\n`;
            message += `💡 Çözüm: Product Service'i Windows'ta native çalıştırın:\n`;
            message += `   cd services/product-service\n`;
            message += `   dotnet run\n`;
          }
          
          if (result.success) {
            alert(message);
          } else {
            alert(message);
          }
        },
        error: (err) => {
          console.error('Cihaz test hatası:', err);
          this.testingDeviceId = null;
          alert(err.error?.message || 'Cihaz testi sırasında bir hata oluştu!');
        }
      });
  }
}
