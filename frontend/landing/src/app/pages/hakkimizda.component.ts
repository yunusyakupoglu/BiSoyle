import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-hakkimizda',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="page-wrapper">
      <header class="page-header">
        <div class="brand">
          <img src="assets/images/bisoyle-logo.png" alt="De'Bakiim Logo">
          <div>
            <span class="brand-title">De'Bakiim</span>
            <span class="brand-tagline">Sesli Sipariş &amp; Abonelik Platformu</span>
          </div>
        </div>
        <nav class="header-actions">
          <a class="header-link" routerLink="/">Ana Sayfa</a>
          <a class="cta-button" routerLink="/#plans">Hemen Başla</a>
        </nav>
      </header>

      <main class="content-page">
        <div class="content-container">
          <h1>Hakkımızda</h1>
          
          <section>
            <h2>De'Bakiim Nedir?</h2>
            <p>
              De'Bakiim, modern işletmeler için tasarlanmış, yapay zeka destekli sesli sipariş ve abonelik yönetim platformudur. 
              Çok kanallı sipariş yönetimi, lisans yönetimi ve cihaz aktivasyonu için uçtan uca çözümler sunar.
            </p>
          </section>

          <section>
            <h2>Misyonumuz</h2>
            <p>
              İşletmelerin sesli sipariş süreçlerini dijitalleştirerek, müşteri deneyimini geliştirmek ve operasyonel verimliliği artırmak.
            </p>
          </section>

          <section>
            <h2>Vizyonumuz</h2>
            <p>
              Türkiye'nin önde gelen sesli sipariş ve abonelik yönetim platformu olmak, işletmelere yenilikçi teknolojilerle değer katmak.
            </p>
          </section>

          <section>
            <h2>Değerlerimiz</h2>
            <ul>
              <li><strong>Güvenilirlik:</strong> %99.9 SLA garantisi ile kesintisiz hizmet</li>
              <li><strong>İnovasyon:</strong> Sürekli gelişen teknoloji ve özellikler</li>
              <li><strong>Müşteri Odaklılık:</strong> 7/24 destek ve hızlı çözümler</li>
              <li><strong>Güvenlik:</strong> SSL sertifikalı, güvenli ödeme altyapısı</li>
            </ul>
          </section>

          <section>
            <h2>İletişim</h2>
            <p>
              <strong>E-posta:</strong> <a href="mailto:destek@bisoyle.com">destek@bisoyle.com</a><br>
              <strong>Telefon:</strong> <a href="tel:+902120000000">+90 212 000 00 00</a>
            </p>
          </section>

          <div class="back-link">
            <a routerLink="/" class="primary-button">Ana Sayfaya Dön</a>
          </div>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .content-page {
      padding: 3rem 4rem 4rem;
      max-width: 900px;
      margin: 0 auto;
    }

    .content-container {
      background: #fff;
      border-radius: 28px;
      padding: 3rem;
      box-shadow: 0 22px 45px rgba(15, 47, 88, 0.12);
    }

    h1 {
      font-size: 2.5rem;
      color: #0f2f58;
      margin-bottom: 2rem;
    }

    h2 {
      font-size: 1.75rem;
      color: #2a1a60;
      margin-top: 2rem;
      margin-bottom: 1rem;
    }

    p {
      color: #5d547c;
      line-height: 1.8;
      margin-bottom: 1.5rem;
    }

    ul {
      color: #5d547c;
      line-height: 1.8;
      padding-left: 1.5rem;
    }

    li {
      margin-bottom: 0.75rem;
    }

    .back-link {
      margin-top: 3rem;
      text-align: center;
    }

    a {
      color: #125dff;
      text-decoration: none;
    }

    a:hover {
      text-decoration: underline;
    }
  `]
})
export class HakkimizdaComponent {}

