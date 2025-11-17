import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '@/app/services/auth.service';

interface Announcement {
  id: number;
  title: string;
  message: string;
  tenantId?: number | null;
  createdByUserId: number;
  priority: string;
  isActive: boolean;
  createdDate: string;
  expiryDate?: string | null;
}

interface Tenant {
  id: number;
  firmaAdi: string;
}

@Component({
  selector: 'app-duyurular',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './duyurular.component.html'
})
export class DuyurularComponent implements OnInit {
  announcements: Announcement[] = [];
  tenants: Tenant[] = [];
  loading = false;
  error: string | null = null;
  
  // Modal state
  showModal = false;
  editingAnnouncement: Announcement | null = null;
  saving = false;
  
  // Form data
  formData = {
    title: '',
    message: '',
    tenantId: null as number | null,
    priority: 'Normal',
    expiryDate: ''
  };
  
  currentUser: any = null;
  isSuperAdmin = false;
  isAdmin = false;
  
  priorityColors: Record<string, string> = {
    'Normal': 'bg-secondary',
    'Yüksek': 'bg-warning',
    'Kritik': 'bg-danger'
  };

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.currentUser = this.authService.getUser();
    const roles = this.currentUser?.roles || [];
    this.isSuperAdmin = roles.includes('SuperAdmin');
    this.isAdmin = roles.includes('Admin') && !this.isSuperAdmin;
    
    if (this.isSuperAdmin) {
      this.loadTenants();
      this.loadAllAnnouncements();
    } else if (this.isAdmin) {
      this.loadAnnouncements();
      // Duyurular sayfası açıldığında tüm duyuruları okundu olarak işaretle
      setTimeout(() => {
        this.markAllAsRead();
      }, 1000);
    }
  }

  markAllAsRead(): void {
    if (!this.currentUser?.id) return;
    
    // Tüm duyuruları okundu olarak işaretle
    this.announcements.forEach(announcement => {
      this.http.post(`${environment.apiUrl}/announcements/${announcement.id}/mark-read`, 
        { userId: this.currentUser.id }, 
        { headers: this.headers }).subscribe({
        next: () => {
          // Başarılı
        },
        error: (err) => {
          console.error('Duyuru okundu işaretlenemedi:', err);
        }
      });
    });
  }

  get headers() {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  loadTenants() {
    this.http.get<Tenant[]>(`${environment.apiUrl}/tenants`, { headers: this.headers }).subscribe({
      next: (data) => {
        this.tenants = data || [];
      },
      error: (err) => {
        console.error('Firmalar yüklenemedi:', err);
      }
    });
  }

  loadAllAnnouncements() {
    this.loading = true;
    this.error = null;

    // SuperAdmin tüm duyuruları görür (aktif ve pasif) - allTenants=true ile tüm duyuruları getir
    this.http.get<Announcement[]>(`${environment.apiUrl}/announcements?allTenants=true`, { headers: this.headers }).subscribe({
      next: (data) => {
        this.announcements = data || [];
        this.loading = false;
      },
      error: (err) => {
        console.error('Duyurular yüklenemedi:', err);
        this.error = 'Duyurular yüklenirken bir hata oluştu.';
        this.loading = false;
      }
    });
  }

  loadAnnouncements() {
    this.loading = true;
    this.error = null;

    const tenantId = this.currentUser?.tenantId;
    const params = new URLSearchParams();
    if (tenantId) {
      params.append('tenantId', tenantId.toString());
    }
    
    const qs = params.toString() ? `?${params.toString()}` : '';
    
    this.http.get<Announcement[]>(`${environment.apiUrl}/announcements${qs}`, { headers: this.headers }).subscribe({
      next: (data) => {
        this.announcements = data || [];
        this.loading = false;
      },
      error: (err) => {
        console.error('Duyurular yüklenemedi:', err);
        this.error = 'Duyurular yüklenirken bir hata oluştu.';
        this.loading = false;
      }
    });
  }

  getTenantName(tenantId: number | null | undefined): string {
    if (!tenantId) return 'Tüm Firmalar';
    const tenant = this.tenants.find(t => t.id === tenantId);
    return tenant?.firmaAdi || `Firma ${tenantId}`;
  }

  openCreateModal(): void {
    this.editingAnnouncement = null;
    this.formData = {
      title: '',
      message: '',
      tenantId: null,
      priority: 'Normal',
      expiryDate: ''
    };
    this.showModal = true;
  }

  openEditModal(announcement: Announcement): void {
    this.editingAnnouncement = announcement;
    this.formData = {
      title: announcement.title || '',
      message: announcement.message || '',
      tenantId: announcement.tenantId || null,
      priority: announcement.priority || 'Normal',
      expiryDate: announcement.expiryDate ? announcement.expiryDate.split('T')[0] : ''
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingAnnouncement = null;
  }

  saveAnnouncement(): void {
    if (!this.formData.title || !this.formData.message) {
      alert('Başlık ve mesaj zorunludur!');
      return;
    }

    this.saving = true;
    const announcementData: any = {
      createdByUserId: this.currentUser?.id || 1,
      title: this.formData.title,
      message: this.formData.message,
      priority: this.formData.priority,
      isActive: true
    };

    // TenantId null ise tüm firmalara, değilse belirli bir firmaya
    if (this.formData.tenantId) {
      announcementData.tenantId = this.formData.tenantId;
    } else {
      announcementData.tenantId = null;
    }

    if (this.formData.expiryDate) {
      announcementData.expiryDate = new Date(this.formData.expiryDate).toISOString();
    }

    if (this.editingAnnouncement) {
      // Update
      this.http.put(`${environment.apiUrl}/announcements/${this.editingAnnouncement.id}`, announcementData, { headers: this.headers }).subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadAllAnnouncements();
        },
        error: (err) => {
          console.error('Duyuru güncellenemedi:', err);
          alert(err.error?.detail || 'Duyuru güncellenirken bir hata oluştu!');
          this.saving = false;
        }
      });
    } else {
      // Create
      this.http.post(`${environment.apiUrl}/announcements`, announcementData, { headers: this.headers }).subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadAllAnnouncements();
        },
        error: (err) => {
          console.error('Duyuru oluşturulamadı:', err);
          alert(err.error?.detail || 'Duyuru oluşturulurken bir hata oluştu!');
          this.saving = false;
        }
      });
    }
  }

  deleteAnnouncement(announcement: Announcement): void {
    if (!confirm(`${announcement.title} duyurusunu silmek istediğinizden emin misiniz?`)) {
      return;
    }

    this.http.delete(`${environment.apiUrl}/announcements/${announcement.id}`, { headers: this.headers }).subscribe({
      next: () => {
        if (this.isSuperAdmin) {
          this.loadAllAnnouncements();
        } else {
          this.loadAnnouncements();
        }
      },
      error: (err) => {
        console.error('Duyuru silinemedi:', err);
        alert('Duyuru silinirken bir hata oluştu!');
      }
    });
  }

  toggleActive(announcement: Announcement): void {
    const updatedData = {
      ...announcement,
      isActive: !announcement.isActive
    };

    this.http.put(`${environment.apiUrl}/announcements/${announcement.id}`, updatedData, { headers: this.headers }).subscribe({
      next: () => {
        announcement.isActive = !announcement.isActive;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Duyuru durumu güncellenemedi:', err);
        alert('Duyuru durumu güncellenirken bir hata oluştu!');
      }
    });
  }

  trackByAnnouncementId(index: number, announcement: Announcement): any {
    return announcement?.id || index;
  }

  trackByTenantId(index: number, tenant: Tenant): any {
    return tenant?.id || index;
  }
}

