import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '@/app/services/auth.service';

@Component({
  selector: 'app-kullanicilar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './kullanicilar.component.html',
  styleUrls: ['./kullanicilar.component.scss']
})
export class KullanicilarComponent implements OnInit {
  kullanicilar: any[] = [];
  roller: any[] = [];
  availableRoles: any[] = []; // SuperAdmin hariç roller
  loading = false;
  error: string | null = null;
  generatingPassword: Record<number, boolean> = {};
  showPasswordModal = false;
  generatedPasswordInfo: { username: string; password: string } | null = null;
  
  // SuperAdmin kontrolü - method olarak tanımlandı
  isSuperAdmin(): boolean {
    const user = this.authService.getUser();
    if (!user) return false;
    const hasSuperAdminRole = user.roles && user.roles.includes('SuperAdmin');
    const isSuperAdminTenant = !user.tenantId || user.tenantId === 0;
    return isSuperAdminTenant || hasSuperAdminRole || false;
  }
  
  // Helper method for template - SuperAdmin rolünün varlığını kontrol et
  hasSuperAdminRole(): boolean {
    return this.roller.some(r => r.roleAdi === 'SuperAdmin');
  }
  
  // Modal state
  showModal = false;
  editingUser: any = null;
  saving = false;
  
  // Form data
  formData = {
    username: '',
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    title: '',
    location: '',
    tenantId: null as number | null,
    roleIds: [] as number[]
  };

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadKullanicilar();
    this.loadRoller();
  }

  loadKullanicilar(): void {
    this.loading = true;
    this.error = null;

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.get<any[]>(`${environment.apiUrl}/users`, { headers })
      .subscribe({
        next: (data) => {
          this.kullanicilar = data;
          this.loading = false;
        },
        error: (err) => {
          console.error('Kullanıcılar yüklenemedi:', err);
          this.error = 'Kullanıcılar yüklenirken bir hata oluştu.';
          this.loading = false;
        }
      });
  }

  loadRoller(): void {
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.get<any[]>(`${environment.apiUrl}/roles`, { headers })
      .subscribe({
        next: (data) => {
          this.roller = data;
          // SuperAdmin rolünü filtrele - sadece SuperAdmin kullanıcılar görebilir
          if (this.isSuperAdmin()) {
            this.availableRoles = data; // SuperAdmin tüm rollerini görebilir
          } else {
            this.availableRoles = data.filter(r => r.roleAdi !== 'SuperAdmin'); // Admin SuperAdmin'i göremez
          }
        },
        error: (err) => {
          console.error('Roller yüklenemedi:', err);
        }
      });
  }

  openCreateModal(): void {
    const currentUser = this.authService.getUser();
    this.editingUser = null;
    this.formData = {
      username: '',
      email: '',
      password: '',
      firstName: '',
      lastName: '',
      title: '',
      location: '',
      tenantId: currentUser?.tenantId || null,
      roleIds: []
    };
    this.showModal = true;
  }

  openEditModal(user: any): void {
    this.editingUser = user;
    
    // Get role IDs from role names (SuperAdmin hariç - Admin kullanıcılar göremez)
    const roleIds = user.roles?.map((roleName: string) => {
      // Admin kullanıcılar SuperAdmin rolünü göremez, bu yüzden filtrele
      if (!this.isSuperAdmin() && roleName === 'SuperAdmin') {
        return null; // SuperAdmin rolü Admin tarafından görülemez
      }
      const role = this.roller.find(r => r.roleAdi === roleName);
      return role?.id;
    }).filter((id: number | undefined) => id !== undefined && id !== null) || [];
    
    this.formData = {
      username: user.username || '',
      email: user.email || '',
      password: '',
      firstName: user.firstName || '',
      lastName: user.lastName || '',
      title: user.title || '',
      location: user.location || '',
      tenantId: user.tenantId,
      roleIds: roleIds
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingUser = null;
  }

  isRoleSelected(roleId: number): boolean {
    return this.formData.roleIds.includes(roleId);
  }

  toggleRole(roleId: number): void {
    const role = this.roller.find(r => r.id === roleId);
    const roleName = role?.roleAdi || '';
    
    // Admin ve User rollerini aynı anda seçilemez
    if (roleName === 'Admin') {
      // Admin seçilirse User rolünü kaldır
      const userRole = this.roller.find(r => r.roleAdi === 'User');
      if (userRole) {
        const userIndex = this.formData.roleIds.indexOf(userRole.id);
        if (userIndex > -1) {
          this.formData.roleIds.splice(userIndex, 1);
        }
      }
    } else if (roleName === 'User') {
      // User seçilirse Admin rolünü kaldır
      const adminRole = this.roller.find(r => r.roleAdi === 'Admin');
      if (adminRole) {
        const adminIndex = this.formData.roleIds.indexOf(adminRole.id);
        if (adminIndex > -1) {
          this.formData.roleIds.splice(adminIndex, 1);
        }
      }
    }
    
    // Rol seçimini toggle et
    const index = this.formData.roleIds.indexOf(roleId);
    if (index > -1) {
      this.formData.roleIds.splice(index, 1);
    } else {
      this.formData.roleIds.push(roleId);
    }
  }

  saveUser(): void {
    if (!this.formData.username || !this.formData.email || !this.formData.firstName) {
      alert('Kullanıcı adı, e-posta ve ad zorunludur!');
      return;
    }

    if (!this.editingUser && !this.formData.password) {
      alert('Şifre zorunludur!');
      return;
    }

    if (this.formData.roleIds.length === 0) {
      alert('En az bir rol seçmelisiniz!');
      return;
    }

    this.saving = true;
    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    if (this.editingUser) {
      // Update user
      const updateData = {
        username: this.formData.username,
        email: this.formData.email,
        firstName: this.formData.firstName,
        lastName: this.formData.lastName,
        title: this.formData.title,
        location: this.formData.location
      };

      this.http.put(`${environment.apiUrl}/users/${this.editingUser.id}`, updateData, { headers })
        .subscribe({
          next: () => {
            // Update roles
            this.http.put(`${environment.apiUrl}/users/${this.editingUser.id}/roles`, 
              { roleIds: this.formData.roleIds }, { headers })
              .subscribe({
                next: () => {
                  this.saving = false;
                  this.closeModal();
                  this.loadKullanicilar();
                },
                error: (err) => {
                  console.error('Roller güncellenemedi:', err);
                  this.saving = false;
                  alert('Roller güncellenirken bir hata oluştu!');
                }
              });
          },
          error: (err) => {
            console.error('Kullanıcı güncellenemedi:', err);
            alert('Kullanıcı güncellenirken bir hata oluştu!');
            this.saving = false;
          }
        });
    } else {
      // Create user
      this.http.post(`${environment.apiUrl}/users`, this.formData, { headers })
        .subscribe({
          next: () => {
            this.saving = false;
            this.closeModal();
            this.loadKullanicilar();
          },
          error: (err) => {
            console.error('Kullanıcı oluşturulamadı:', err);
            alert(err.error?.message || 'Kullanıcı oluşturulurken bir hata oluştu!');
            this.saving = false;
          }
        });
    }
  }

  toggleActive(user: any): void {
    if (!confirm(`${user.firstName} ${user.lastName} kullanıcısını ${user.aktif ? 'pasif' : 'aktif'} etmek istediğinize emin misiniz?`)) {
      return;
    }

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.put(`${environment.apiUrl}/users/${user.id}/toggle-active`, {}, { headers })
      .subscribe({
        next: () => {
          this.loadKullanicilar();
        },
        error: (err) => {
          console.error('Durum değiştirilemedi:', err);
          alert('Durum değiştirilirken bir hata oluştu!');
        }
      });
  }

  deleteUser(user: any): void {
    if (!confirm(`${user.firstName} ${user.lastName} kullanıcısını silmek istediğinize emin misiniz?`)) {
      return;
    }

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`
    };

    this.http.delete(`${environment.apiUrl}/users/${user.id}`, { headers })
      .subscribe({
        next: () => {
          this.loadKullanicilar();
        },
        error: (err) => {
          console.error('Kullanıcı silinemedi:', err);
          alert('Kullanıcı silinirken bir hata oluştu!');
        }
      });
  }

  generatePassword(user: any): void {
    if (!confirm(`${user.firstName} ${user.lastName} için yeni bir parola üretmek istediğinize emin misiniz?`)) {
      return;
    }

    this.generatingPassword[user.id] = true;

    const headers = {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    };

    this.http.post(`${environment.apiUrl}/users/${user.id}/generate-password`, {}, { headers })
      .subscribe({
        next: (response: any) => {
          this.generatingPassword[user.id] = false;
          this.generatedPasswordInfo = {
            username: user.username,
            password: response.password
          };
          this.showPasswordModal = true;
        },
        error: (err) => {
          console.error('Parola üretilemedi:', err);
          alert(err.error?.detail || 'Parola üretilirken bir hata oluştu!');
          this.generatingPassword[user.id] = false;
        }
      });
  }

  closePasswordModal(): void {
    this.showPasswordModal = false;
    this.generatedPasswordInfo = null;
  }

  copyToClipboard(text: string): void {
    if (!text) {
      return;
    }
    navigator.clipboard.writeText(text).then(() => {
      alert('Panoya kopyalandı');
    }).catch(err => {
      console.error('Kopyalama hatası:', err);
      alert('Kopyalama başarısız!');
    });
  }
}
