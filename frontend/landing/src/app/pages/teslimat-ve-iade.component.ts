import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-teslimat-ve-iade',
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
          <h1>Teslimat ve İade Şartları</h1>
          
          <section>
            <h2>1. Teslimat</h2>
            <p>
              De'Bakiim platformu dijital bir hizmettir. Abonelik satın alma işleminiz tamamlandıktan sonra:
            </p>
            <ul>
              <li>Firma kaydınız otomatik olarak oluşturulur</li>
              <li>Lisans anahtarınız ve admin kullanıcı bilgileriniz e-posta adresinize gönderilir</li>
              <li>Teslimat süresi maksimum 24 saattir</li>
              <li>E-posta adresinizi kontrol etmeyi unutmayın</li>
            </ul>
          </section>

          <section>
            <h2>2. İade ve İptal</h2>
            <h3>2.1. İptal Hakkı</h3>
            <p>
              Mesafeli Satış Sözleşmesi kapsamında, abonelik satın alma işleminden itibaren 14 gün içinde 
              cayma hakkınızı kullanabilirsiniz.
            </p>
            
            <h3>2.2. İade Koşulları</h3>
            <ul>
              <li>İptal talebi, satın alma tarihinden itibaren 14 gün içinde yapılmalıdır</li>
              <li>İptal talebi <a href="mailto:destek@bisoyle.com">destek@bisoyle.com</a> adresine gönderilmelidir</li>
              <li>İade işlemi, ödeme yönteminize göre 5-10 iş günü içinde gerçekleştirilir</li>
            </ul>

            <h3>2.3. İade Edilemeyecek Durumlar</h3>
            <ul>
              <li>Lisans anahtarı kullanılmış ve aktivasyon yapılmış ise</li>
              <li>14 günlük cayma süresi geçmiş ise</li>
              <li>Hizmet kullanılmaya başlanmış ise</li>
            </ul>
          </section>

          <section>
            <h2>3. İletişim</h2>
            <p>
              İade ve iptal talepleriniz için:<br>
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

    h3 {
      font-size: 1.25rem;
      color: #3a2380;
      margin-top: 1.5rem;
      margin-bottom: 0.75rem;
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
export class TeslimatVeIadeComponent {}

