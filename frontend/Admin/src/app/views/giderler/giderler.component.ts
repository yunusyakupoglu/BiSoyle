import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '@/app/services/auth.service';

interface Expense {
  id: number;
  tenantId: number;
  userId: number;
  giderAdi: string;
  tutar: number;
  kategori?: string;
  aciklama?: string;
  olusturmaTarihi: string;
}

@Component({
  selector: 'app-giderler',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './giderler.component.html'
})
export class GiderlerComponent implements OnInit {
  giderler: Expense[] = [];
  loading = false;
  error: string | null = null;
  
  // Modal state
  showModal = false;
  editingExpense: Expense | null = null;
  saving = false;
  
  // Form data
  formData = {
    giderAdi: '',
    tutar: 0,
    kategori: '',
    aciklama: '',
    tenantId: null as number | null,
    userId: null as number | null
  };

  // Kategori seçenekleri
  kategoriSecenekleri = [
    'Kira',
    'Elektrik',
    'Su',
    'Doğalgaz',
    'İnternet',
    'Telefon',
    'Personel',
    'Yakıt',
    'Bakım-Onarım',
    'Temizlik',
    'Güvenlik',
    'Vergi',
    'Sigorta',
    'Reklam',
    'Diğer'
  ];

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.loadGiderler();
  }

  get headers() {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  loadGiderler() {
    this.loading = true;
    this.error = null;

    const user = this.authService.getUser();
    const tenantId = user?.tenantId;
    let url = `${environment.apiUrl}/expenses`;
    if (tenantId) {
      url += `?tenantId=${tenantId}`;
    }

    this.http.get<Expense[]>(url, { headers: this.headers })
      .subscribe({
        next: (data) => {
          this.giderler = data;
          this.loading = false;
        },
        error: (err) => {
          console.error('Giderler yüklenemedi:', err);
          this.error = 'Giderler yüklenirken bir hata oluştu.';
          this.loading = false;
        }
      });
  }

  openCreateModal(): void {
    const currentUser = this.authService.getUser();
    this.editingExpense = null;
    this.formData = {
      giderAdi: '',
      tutar: 0,
      kategori: '',
      aciklama: '',
      tenantId: currentUser?.tenantId || null,
      userId: currentUser?.id || null
    };
    this.showModal = true;
  }

  openEditModal(expense: Expense): void {
    this.editingExpense = expense;
    this.formData = {
      giderAdi: expense.giderAdi || '',
      tutar: expense.tutar || 0,
      kategori: expense.kategori || '',
      aciklama: expense.aciklama || '',
      tenantId: expense.tenantId,
      userId: expense.userId
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingExpense = null;
  }

  saveExpense(): void {
    if (!this.formData.giderAdi) {
      alert('Gider adı zorunludur!');
      return;
    }

    if (!this.formData.tutar || this.formData.tutar <= 0) {
      alert('Gider tutarı 0\'dan büyük olmalıdır!');
      return;
    }

    this.saving = true;

    if (this.editingExpense) {
      // Update
      this.http.put<Expense>(`${environment.apiUrl}/expenses/${this.editingExpense.id}`, this.formData, { headers: this.headers })
        .subscribe({
          next: () => {
            alert('Gider güncellendi!');
            this.closeModal();
            this.loadGiderler();
          },
          error: (err) => {
            console.error('Gider güncellenemedi:', err);
            alert('Gider güncellenirken bir hata oluştu: ' + (err.error?.detail || err.message));
            this.saving = false;
          }
        });
    } else {
      // Create
      this.http.post<Expense>(`${environment.apiUrl}/expenses`, this.formData, { headers: this.headers })
        .subscribe({
          next: () => {
            alert('Gider eklendi!');
            this.closeModal();
            this.loadGiderler();
          },
          error: (err) => {
            console.error('Gider eklenemedi:', err);
            alert('Gider eklenirken bir hata oluştu: ' + (err.error?.detail || err.message));
            this.saving = false;
          }
        });
    }
  }

  deleteExpense(id: number): void {
    if (!confirm('Bu gideri silmek istediğinizden emin misiniz?')) {
      return;
    }

    this.http.delete(`${environment.apiUrl}/expenses/${id}`, { headers: this.headers })
      .subscribe({
        next: () => {
          alert('Gider silindi!');
          this.loadGiderler();
        },
        error: (err) => {
          console.error('Gider silinemedi:', err);
          alert('Gider silinirken bir hata oluştu: ' + (err.error?.detail || err.message));
        }
      });
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY'
    }).format(value);
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('tr-TR', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  }
}









