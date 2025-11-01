import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '@/app/services/auth.service';

@Component({
  selector: 'app-kullanicilar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './kullanicilar.component.html',
  styleUrls: ['./kullanicilar.component.scss']
})
export class KullanicilarComponent implements OnInit {
  kullanicilar: any[] = [];
  roller: any[] = [];
  loading = false;
  error: string | null = null;
  
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
    
    // Get role IDs from role names
    const roleIds = user.roles?.map((roleName: string) => {
      const role = this.roller.find(r => r.roleAdi === roleName);
      return role?.id;
    }).filter((id: number | undefined) => id !== undefined) || [];
    
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
}
