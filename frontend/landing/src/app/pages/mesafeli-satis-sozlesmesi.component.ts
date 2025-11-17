import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-mesafeli-satis-sozlesmesi',
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
          <h1>Mesafeli Satış Sözleşmesi</h1>
          
          <section>
            <h2>1. Taraflar</h2>
            <p>
              <strong>SATICI:</strong><br>
              Hosteagle Information Technologies<br>
              E-posta: destek@bisoyle.com<br>
              Telefon: +90 212 000 00 00
            </p>
            <p>
              <strong>ALICI:</strong> Bu sözleşmeyi kabul eden ve abonelik satın alan gerçek veya tüzel kişi.
            </p>
          </section>

          <section>
            <h2>2. Konu</h2>
            <p>
              Bu sözleşme, Alıcı'nın Satıcı'dan satın aldığı De'Bakiim platformu abonelik hizmetinin 
              koşullarını, tarafların hak ve yükümlülüklerini düzenlemektedir.
            </p>
          </section>

          <section>
            <h2>3. Sözleşme Konusu Ürün/Hizmet</h2>
            <p>
              De'Bakiim, yapay zeka destekli sesli sipariş ve abonelik yönetim platformudur. Abonelik kapsamında:
            </p>
            <ul>
              <li>Firma (tenant) kaydı oluşturulması</li>
              <li>Lisans anahtarı ve admin kullanıcı bilgilerinin teslimi</li>
              <li>Platform erişim hakkı</li>
              <li>Teknik destek hizmeti</li>
            </ul>
          </section>

          <section>
            <h2>4. Fiyat ve Ödeme</h2>
            <p>
              Abonelik ücreti, seçilen plana göre belirlenir ve aylık olarak tahsil edilir. Ödeme, 
              iyzico ödeme altyapısı üzerinden güvenli şekilde gerçekleştirilir. Tüm fiyatlar KDV dahildir.
            </p>
          </section>

          <section>
            <h2>5. Teslimat</h2>
            <p>
              Hizmet dijital bir ürün olduğundan, abonelik satın alma işlemi tamamlandıktan sonra:
            </p>
            <ul>
              <li>Firma kaydı otomatik oluşturulur</li>
              <li>Lisans anahtarı ve admin bilgileri e-posta ile gönderilir</li>
              <li>Teslimat süresi maksimum 24 saattir</li>
            </ul>
          </section>

          <section>
            <h2>6. Cayma Hakkı</h2>
            <p>
              Mesafeli Satışlar Hakkında Yönetmelik uyarınca, satın alma işleminden itibaren 14 gün içinde 
              cayma hakkınızı kullanabilirsiniz. Ancak:
            </p>
            <ul>
              <li>Lisans anahtarı kullanılmış ve aktivasyon yapılmış ise cayma hakkı kullanılamaz</li>
              <li>Hizmet kullanılmaya başlanmış ise cayma hakkı kullanılamaz</li>
              <li>Cayma talebi <a href="mailto:destek@bisoyle.com">destek@bisoyle.com</a> adresine gönderilmelidir</li>
            </ul>
          </section>

          <section>
            <h2>7. İade</h2>
            <p>
              Cayma hakkı kullanıldığında, ödeme yönteminize göre 5-10 iş günü içinde iade işlemi gerçekleştirilir.
            </p>
          </section>

          <section>
            <h2>8. Garanti ve Sorumluluk</h2>
            <p>
              Satıcı, hizmetin sözleşme hükümlerine uygun şekilde sunulmasından sorumludur. Platform erişiminde 
              yaşanan teknik sorunlar için teknik destek sağlanır. Ancak internet bağlantısı veya kullanıcı 
              kaynaklı sorunlardan Satıcı sorumlu değildir.
            </p>
          </section>

          <section>
            <h2>9. Uyuşmazlıkların Çözümü</h2>
            <p>
              Bu sözleşmeden kaynaklanan uyuşmazlıklar, Türkiye Cumhuriyeti yasalarına tabidir ve İstanbul 
              Mahkemeleri ve İcra Daireleri yetkilidir.
            </p>
          </section>

          <section>
            <h2>10. İletişim</h2>
            <p>
              Sözleşme ile ilgili sorularınız için:<br>
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
export class MesafeliSatisSozlesmesiComponent {}

