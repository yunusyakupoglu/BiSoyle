import { Component, OnInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface OlcuBirimi {
  id: number;
  olcu_birimi_adi: string;
}

@Component({
  selector: 'app-olcu-birimleri',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './olcu-birimleri.component.html'
})
export class OlcuBirimleriComponent implements OnInit {
  olcuBirimleri: OlcuBirimi[] = [];
  yukleniyor = false;
  olcuAdi: string = '';

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.olcuBirimleriniGetir();
  }

  olcuBirimleriniGetir() {
    this.yukleniyor = true;
    this.http.get<OlcuBirimi[]>('http://localhost:8000/olcu-birimleri').subscribe({
      next: (data) => {
        this.olcuBirimleri = data;
        this.yukleniyor = false;
      },
      error: (err) => {
        console.error('Ölçü birimleri yüklenemedi:', err);
        this.yukleniyor = false;
      }
    });
  }

  olcuEkle() {
    if (!this.olcuAdi) {
      alert('Ölçü birimi adını girin');
      return;
    }

    const yeniOlcu = {
      olcu_birimi_adi: this.olcuAdi
    };

    this.http.post<OlcuBirimi>('http://localhost:8000/olcu-birimleri', yeniOlcu).subscribe({
      next: () => {
        this.olcuBirimleriniGetir();
        this.olcuAdi = '';
      },
      error: (err) => console.error('Ölçü birimi eklenemedi:', err)
    });
  }

  olcuSil(id: number) {
    if (confirm('Bu ölçü birimini silmek istediğinizden emin misiniz?')) {
      this.http.delete(`http://localhost:8000/olcu-birimleri/${id}`).subscribe({
        next: () => this.olcuBirimleriniGetir(),
        error: (err) => console.error('Ölçü birimi silinemedi:', err)
      });
    }
  }
}


