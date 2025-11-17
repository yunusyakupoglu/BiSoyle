import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-gizlilik-sozlesmesi',
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
          <h1>Gizlilik Sözleşmesi</h1>
          
          <section>
            <h2>1. Genel Bilgiler</h2>
            <p>
              Bu Gizlilik Sözleşmesi, De'Bakiim platformu tarafından toplanan kişisel verilerin korunması ve işlenmesi 
              hakkında bilgi vermek amacıyla hazırlanmıştır. 6698 sayılı Kişisel Verilerin Korunması Kanunu ("KVKK") 
              kapsamında veri sorumlusu sıfatıyla hareket etmekteyiz.
            </p>
          </section>

          <section>
            <h2>2. Toplanan Kişisel Veriler</h2>
            <p>Platformumuz aracılığıyla aşağıdaki kişisel veriler toplanmaktadır:</p>
            <ul>
              <li>Kimlik bilgileri (Ad, Soyad)</li>
              <li>İletişim bilgileri (E-posta, Telefon, Adres)</li>
              <li>Firma bilgileri (Firma Adı, Vergi Numarası)</li>
              <li>Ödeme bilgileri (Kart bilgileri iyzico altyapısı üzerinden işlenir, saklanmaz)</li>
              <li>Teknik veriler (IP adresi, tarayıcı bilgileri, çerezler)</li>
            </ul>
          </section>

          <section>
            <h2>3. Verilerin İşlenme Amacı</h2>
            <p>Toplanan kişisel veriler aşağıdaki amaçlarla işlenmektedir:</p>
            <ul>
              <li>Abonelik işlemlerinin gerçekleştirilmesi</li>
              <li>Firma ve kullanıcı hesaplarının yönetilmesi</li>
              <li>Ödeme işlemlerinin güvenli şekilde tamamlanması</li>
              <li>Müşteri hizmetleri ve destek süreçlerinin yürütülmesi</li>
              <li>Yasal yükümlülüklerin yerine getirilmesi</li>
            </ul>
          </section>

          <section>
            <h2>4. Verilerin Paylaşılması</h2>
            <p>
              Kişisel verileriniz, yalnızca yasal zorunluluklar ve hizmet sunumu için gerekli olduğu durumlarda, 
              iyzico ödeme altyapısı gibi güvenilir üçüncü taraflarla paylaşılabilir. Verileriniz asla üçüncü 
              taraflara satılmaz veya pazarlama amaçlı kullanılmaz.
            </p>
          </section>

          <section>
            <h2>5. Veri Güvenliği</h2>
            <p>
              Kişisel verilerinizin güvenliği için SSL sertifikası kullanılmakta, veriler şifrelenmiş kanallar 
              üzerinden iletilmekte ve güvenli sunucularda saklanmaktadır.
            </p>
          </section>

          <section>
            <h2>6. Haklarınız</h2>
            <p>KVKK kapsamında aşağıdaki haklara sahipsiniz:</p>
            <ul>
              <li>Kişisel verilerinizin işlenip işlenmediğini öğrenme</li>
              <li>İşlenen verileriniz hakkında bilgi talep etme</li>
              <li>Verilerinizin düzeltilmesini veya silinmesini talep etme</li>
              <li>İşleme itiraz etme</li>
              <li>Verilerinizin aktarılmasını talep etme</li>
            </ul>
            <p>
              Bu haklarınızı kullanmak için <a href="mailto:destek@bisoyle.com">destek@bisoyle.com</a> adresine başvurabilirsiniz.
            </p>
          </section>

          <section>
            <h2>7. Çerezler (Cookies)</h2>
            <p>
              Platformumuz, kullanıcı deneyimini iyileştirmek ve site performansını analiz etmek amacıyla çerezler kullanmaktadır. 
              Tarayıcı ayarlarınızdan çerezleri yönetebilirsiniz.
            </p>
          </section>

          <section>
            <h2>8. İletişim</h2>
            <p>
              Gizlilik politikamız hakkında sorularınız için:<br>
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
export class GizlilikSozlesmesiComponent {}

