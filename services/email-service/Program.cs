using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Set port for Email Service (5011)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5011";
app.Urls.Add($"http://localhost:{port}");

// Test endpoint: HTML template'i döndür (tarayıcıda görüntülemek için)
app.MapGet("/api/email/preview-html", () =>
{
    var testModel = new LicenseEmailModel
    {
        TenantName = "MEY Ticaret 2",
        PlanName = "Professional - 20 Çalışan",
        LicenseKey = "4ACM-GZSH-R95R-7CHQ",
        MaxUsers = 20,
        MaxDevices = 5,
        ToEmail = "cubekod@gmail.com",
        ToName = "MEY Ticaret 2",
        AdminEmail = "cubekod@gmail.com",
        PaymentReference = "VP2025111404414001866",
        Amount = 1600.00m,
        Currency = "TRY",
        Installment = 1,
        PortalUrl = "https://panel.debakiim.com",
        SupportEmail = "destek@debakiim.com",
        SupportPhone = "+90 212 000 00 00",
        SubscriptionDate = new DateTime(2025, 11, 14, 7, 41, 0, DateTimeKind.Utc)
    };
    
    var htmlBody = BuildHtmlBody(testModel);
    return Results.Content(htmlBody, "text/html; charset=utf-8");
});

app.MapPost("/api/email/test", async (TestEmailRequest request) =>
{
    if (request == null || request.MailSettings == null)
    {
        return Results.BadRequest(new { detail = "Geçersiz istek. Mail ayarları zorunludur." });
    }

    var validationError = ValidateMailSettings(request.MailSettings);
    if (!string.IsNullOrWhiteSpace(validationError))
    {
        return Results.BadRequest(new { detail = validationError });
    }

    if (string.IsNullOrWhiteSpace(request.ToEmail))
    {
        return Results.BadRequest(new { detail = "Alıcı e-posta adresi zorunludur." });
    }

    try
    {
        await SendTestEmailAsync(request.MailSettings, request.ToEmail);
        return Results.Ok(new { sent = true, message = "Test maili başarıyla gönderildi." });
    }
    catch (SmtpException smtpEx)
    {
        return Results.Problem($"SMTP hatası: {smtpEx.Message}", statusCode: 502);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Mail gönderimi başarısız: {ex.Message}", statusCode: 500);
    }
});

app.MapPost("/api/email/send-license", async (LicenseEmailRequest request) =>
{
    if (request == null || request.MailSettings == null || request.Model == null)
    {
        return Results.BadRequest(new { detail = "Geçersiz istek. Mail ayarları ve model zorunludur." });
    }

    var validationError = ValidateMailSettings(request.MailSettings);
    if (!string.IsNullOrWhiteSpace(validationError))
    {
        return Results.BadRequest(new { detail = validationError });
    }

    if (string.IsNullOrWhiteSpace(request.Model.ToEmail))
    {
        return Results.BadRequest(new { detail = "Alıcı e-posta adresi zorunludur." });
    }

    try
    {
        await SendLicenseEmailAsync(request.MailSettings, request.Model);
        return Results.Ok(new { sent = true });
    }
    catch (SmtpException smtpEx)
    {
        return Results.Problem($"SMTP hatası: {smtpEx.Message}", statusCode: 502);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Mail gönderimi başarısız: {ex.Message}", statusCode: 500);
    }
});

app.Run();
static string? ValidateMailSettings(MailSettingsDto settings)
{
    if (string.IsNullOrWhiteSpace(settings.Host))
        return "SMTP sunucu adresi zorunludur.";
    if (settings.Port <= 0)
        return "Geçerli bir SMTP portu belirtiniz.";
    if (string.IsNullOrWhiteSpace(settings.Username))
        return "SMTP kullanıcı adı zorunludur.";
    if (string.IsNullOrWhiteSpace(settings.Password))
        return "SMTP parolası zorunludur.";
    if (string.IsNullOrWhiteSpace(settings.FromEmail))
        return "Gönderici e-posta adresi zorunludur.";
    return null;
}

// Logo base64'leri runtime'da dosyalardan okunacak (fallback yok)

static async Task SendLicenseEmailAsync(MailSettingsDto settings, LicenseEmailModel model)
{
    var recipientName = string.IsNullOrWhiteSpace(model.ToName) ? model.TenantName : model.ToName;

    using var message = new MailMessage
    {
        From = new MailAddress(settings.FromEmail, string.IsNullOrWhiteSpace(settings.FromName) ? "De'Bakiim" : settings.FromName),
        Subject = $"De'Bakiim Lisansınız Hazır: {model.PlanName}",
        BodyEncoding = Encoding.UTF8,
        SubjectEncoding = Encoding.UTF8,
        IsBodyHtml = true
    };

    message.To.Add(new MailAddress(model.ToEmail, recipientName));

    if (!string.IsNullOrWhiteSpace(settings.ReplyTo))
    {
        message.ReplyToList.Add(new MailAddress(settings.ReplyTo));
    }

    var htmlBody = BuildHtmlBody(model);
    var textBody = BuildTextBody(model);

    // Debug: HTML body uzunluğunu logla
    Console.WriteLine($"[DEBUG] HTML body length: {htmlBody.Length} characters");
    Console.WriteLine($"[DEBUG] HTML body starts with: {htmlBody.Substring(0, Math.Min(100, htmlBody.Length))}...");

    // HTML body'yi hem Body hem de AlternateView olarak ayarla (en güvenilir yöntem)
    // Body: Bazı email client'lar için gerekli (Outlook, Gmail)
    // AlternateView: Multipart/alternative için gerekli (tüm modern client'lar)
    message.Body = htmlBody;
    message.IsBodyHtml = true;
    message.BodyEncoding = Encoding.UTF8;
    
    // HTML AlternateView ekle (multipart/alternative için)
    var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html);
    htmlView.ContentType.CharSet = "UTF-8";
    message.AlternateViews.Add(htmlView);
    
    // Plain text alternatif view ekle (HTML desteklemeyen client'lar için fallback)
    var plainTextView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, MediaTypeNames.Text.Plain);
    plainTextView.ContentType.CharSet = "UTF-8";
    message.AlternateViews.Add(plainTextView);
    
    Console.WriteLine($"[DEBUG] MailMessage configured: IsBodyHtml={message.IsBodyHtml}, AlternateViews.Count={message.AlternateViews.Count}");

    using var smtpClient = new SmtpClient(settings.Host, settings.Port)
    {
        EnableSsl = !string.Equals(settings.Security, "NONE", StringComparison.OrdinalIgnoreCase),
        DeliveryMethod = SmtpDeliveryMethod.Network,
        Credentials = new NetworkCredential(settings.Username, settings.Password),
        Timeout = 30000
    };

    // Port 465 için SSL/TLS ayarı
    if (settings.Port == 465)
    {
        smtpClient.EnableSsl = true;
    }
    else if (string.Equals(settings.Security, "STARTTLS", StringComparison.OrdinalIgnoreCase))
    {
        smtpClient.TargetName = $"STARTTLS/{settings.Host}";
    }

    await smtpClient.SendMailAsync(message);
}

static string BuildHtmlBody(LicenseEmailModel model)
{
    var culture = CultureInfo.GetCultureInfo("tr-TR");
    var portalUrl = string.IsNullOrWhiteSpace(model.PortalUrl) ? "https://panel.debakiim.com" : model.PortalUrl;
    var supportEmail = string.IsNullOrWhiteSpace(model.SupportEmail) ? "destek@debakiim.com" : model.SupportEmail;
    var supportPhone = string.IsNullOrWhiteSpace(model.SupportPhone) ? "+90 212 000 00 00" : model.SupportPhone;
    var adminEmail = string.IsNullOrWhiteSpace(model.AdminEmail) ? model.ToEmail : model.AdminEmail;
    var subscriptionDate = model.SubscriptionDate.ToLocalTime().ToString("dd MMMM yyyy HH:mm", culture);
    var licenseKey = string.IsNullOrWhiteSpace(model.LicenseKey) ? "Lisans anahtarınız sistemde oluşturuldu." : model.LicenseKey.ToUpperInvariant();
    var amountText = model.Amount.HasValue ? $"{model.Amount.Value.ToString("N2", culture)} {model.Currency ?? "TRY"}" : null;
    var installmentText = model.Installment.HasValue && model.Installment.Value > 1
        ? $"{model.Installment.Value} taksit"
        : "Peşin";

    // Payment meta items (Gmail uyumlu - tablo tabanlı)
    var paymentMeta = new StringBuilder();
    if (!string.IsNullOrWhiteSpace(amountText))
    {
        paymentMeta.Append($@"
                <tr>
                  <td style=""padding: 12px 16px; border-bottom: 1px solid rgba(193, 89, 63, 0.1); font-size: 14px; color: #5d7186;"">
                    <span style=""opacity: 0.75; font-weight: 500;"">Ödenen Tutar</span>
                  </td>
                  <td style=""padding: 12px 16px; border-bottom: 1px solid rgba(193, 89, 63, 0.1); font-size: 15px; color: #36404a; font-weight: 600; text-align: right;"">
                    <strong>{amountText}</strong>
                  </td>
                </tr>
                <tr>
                  <td style=""padding: 12px 16px; border-bottom: 1px solid rgba(193, 89, 63, 0.1); font-size: 14px; color: #5d7186;"">
                    <span style=""opacity: 0.75; font-weight: 500;"">Ödeme Şekli</span>
                  </td>
                  <td style=""padding: 12px 16px; border-bottom: 1px solid rgba(193, 89, 63, 0.1); font-size: 15px; color: #36404a; font-weight: 600; text-align: right;"">
                    <strong>{installmentText}</strong>
                  </td>
                </tr>");
    }
    if (!string.IsNullOrWhiteSpace(model.PaymentReference))
    {
        paymentMeta.Append($@"
                <tr>
                  <td style=""padding: 12px 16px; border-bottom: 1px solid rgba(193, 89, 63, 0.1); font-size: 14px; color: #5d7186;"">
                    <span style=""opacity: 0.75; font-weight: 500;"">Ödeme Referansı</span>
                  </td>
                  <td style=""padding: 12px 16px; border-bottom: 1px solid rgba(193, 89, 63, 0.1); font-size: 15px; color: #36404a; font-weight: 600; text-align: right;"">
                    <strong>{model.PaymentReference}</strong>
                  </td>
                </tr>");
    }
    paymentMeta.Append($@"
                <tr>
                  <td style=""padding: 12px 16px; font-size: 14px; color: #5d7186;"">
                    <span style=""opacity: 0.75; font-weight: 500;"">Abonelik Tarihi</span>
                  </td>
                  <td style=""padding: 12px 16px; font-size: 15px; color: #36404a; font-weight: 600; text-align: right;"">
                    <strong>{subscriptionDate}</strong>
                  </td>
                </tr>");

    // Logo base64'lerini runtime'da dosyalardan oku (ZORUNLU)
    string debakiimLogoBase64 = "";
    string hosteagleLogoBase64 = "";
    string devicePhotoBase64 = "";
    
    try
    {
        // Email service'in çalıştığı dizinden logo dosyalarına ulaş
        var currentDir = Directory.GetCurrentDirectory();
        var debakiimLogoPath = Path.Combine(currentDir, "..", "..", "..", "frontend", "Admin", "src", "assets", "images", "debakiim-logo.png");
        var hosteagleLogoPath = Path.Combine(currentDir, "..", "..", "..", "frontend", "Admin", "src", "assets", "images", "HOSTEAGLE.png");
        var devicePhotoPath = Path.Combine(currentDir, "..", "..", "..", "frontend", "Admin", "src", "assets", "images", "device-photo.png");
        
        // Normalize path (relative path'leri çöz)
        debakiimLogoPath = Path.GetFullPath(debakiimLogoPath);
        hosteagleLogoPath = Path.GetFullPath(hosteagleLogoPath);
        devicePhotoPath = Path.GetFullPath(devicePhotoPath);
        
        Console.WriteLine($"[DEBUG] Current directory: {currentDir}");
        Console.WriteLine($"[DEBUG] DeBakiim logo path: {debakiimLogoPath}");
        Console.WriteLine($"[DEBUG] Hosteagle logo path: {hosteagleLogoPath}");
        Console.WriteLine($"[DEBUG] Device photo path: {devicePhotoPath}");
        Console.WriteLine($"[DEBUG] DeBakiim exists: {File.Exists(debakiimLogoPath)}");
        Console.WriteLine($"[DEBUG] Hosteagle exists: {File.Exists(hosteagleLogoPath)}");
        Console.WriteLine($"[DEBUG] Device photo exists: {File.Exists(devicePhotoPath)}");
        
        if (File.Exists(debakiimLogoPath))
        {
            debakiimLogoBase64 = Convert.ToBase64String(File.ReadAllBytes(debakiimLogoPath));
            Console.WriteLine("DeBakiim logo bulundu ve yuklendi!");
        }
        else
        {
            Console.WriteLine($"WARNING: DeBakiim logo bulunamadi: {debakiimLogoPath}");
        }
        
        if (File.Exists(hosteagleLogoPath))
        {
            hosteagleLogoBase64 = Convert.ToBase64String(File.ReadAllBytes(hosteagleLogoPath));
            Console.WriteLine("Hosteagle logo bulundu ve yuklendi!");
        }
        else
        {
            Console.WriteLine($"WARNING: Hosteagle logo bulunamadi: {hosteagleLogoPath}");
        }
        
        if (File.Exists(devicePhotoPath))
        {
            devicePhotoBase64 = Convert.ToBase64String(File.ReadAllBytes(devicePhotoPath));
            Console.WriteLine("Device photo bulundu ve yuklendi!");
        }
        else
        {
            Console.WriteLine($"WARNING: Device photo bulunamadi: {devicePhotoPath}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Logo dosyalari okunurken hata: {ex.Message}");
        Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
        throw; // Logo dosyaları zorunlu, hata fırlat
    }
    
    if (string.IsNullOrEmpty(debakiimLogoBase64) || string.IsNullOrEmpty(hosteagleLogoBase64))
    {
        Console.WriteLine($"[ERROR] Logo dosyalari bulunamadi!");
        Console.WriteLine($"[ERROR] debakiimLogoBase64 empty: {string.IsNullOrEmpty(debakiimLogoBase64)}");
        Console.WriteLine($"[ERROR] hosteagleLogoBase64 empty: {string.IsNullOrEmpty(hosteagleLogoBase64)}");
        throw new InvalidOperationException("Logo dosyalari bulunamadi veya okunamadi!");
    }

    // Gmail uyumlu HTML template - Tüm CSS inline, tablo tabanlı layout, DeBakiim renkleri
    // DeBakiim renkleri: Primary #c1593f, Secondary #5d7186, Background #f8f9fa, Text #36404a
    return $@"<!DOCTYPE html>
<html lang=""tr"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>De'Bakiim Lisans Aktivasyonu</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Wix Madefor Text', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8f9fa; color: #36404a; -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale;"">
  <!-- Gmail uyumlu wrapper table -->
  <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""margin: 0; padding: 0; background-color: #f8f9fa;"">
    <tr>
      <td align=""center"" style=""padding: 32px 20px;"">
        <!-- Main content table -->
        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""640"" style=""max-width: 640px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);"">
          <!-- Header with logos -->
          <tr>
            <td align=""center"" style=""padding: 40px 36px 20px;"">
              <!-- Logos table -->
              <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
                <tr>
                  <td width=""50%"" align=""left"" style=""vertical-align: middle;"">
                    <img src=""data:image/png;base64,{debakiimLogoBase64}"" alt=""De'Bakiim"" style=""display: block; max-height: 50px; width: auto; height: auto;"">
                  </td>
                  <td width=""50%"" align=""right"" style=""vertical-align: middle;"">
                    <img src=""data:image/png;base64,{hosteagleLogoBase64}"" alt=""Hosteagle"" style=""display: block; max-height: 50px; width: auto; height: auto;"">
                  </td>
                </tr>
              </table>
              <!-- Badge -->
              <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px auto;"">
                <tr>
                  <td style=""padding: 6px 14px; border-radius: 20px; background-color: rgba(193, 89, 63, 0.12); color: #c1593f; font-weight: 600; font-size: 13px; letter-spacing: 0.05em;"">
                    Lisans Aktivasyonu
                  </td>
                </tr>
              </table>
              <!-- Title -->
              <h1 style=""margin: 18px 0 12px; padding: 0; font-size: 28px; font-weight: 700; color: #36404a; line-height: 1.3;"">
                {model.TenantName} için abonelik tamamlandı
              </h1>
              <!-- Lead text -->
              <p style=""margin: 0; padding: 0; font-size: 16px; line-height: 1.6; color: #5d7186;"">
                Tebrikler! {model.PlanName} planınızı başarıyla aktive ettik. Lisans anahtarınız ve sonraki adımlar aşağıda.
              </p>
            </td>
          </tr>
          
          <!-- Plan Card -->
          <tr>
            <td style=""padding: 0 36px 20px;"">
              <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color: rgba(193, 89, 63, 0.08); border-radius: 12px; border: 1px solid rgba(193, 89, 63, 0.15);"">
                <tr>
                  <td style=""padding: 22px;"">
                    <h2 style=""margin: 0 0 12px; padding: 0; font-size: 20px; font-weight: 600; color: #36404a;"">Plan Özeti</h2>
                    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td style=""padding: 8px 0; font-size: 15px; color: #5d7186;"">
                          <strong style=""color: #36404a;"">{model.PlanName}</strong>
                        </td>
                      </tr>
                      <tr>
                        <td style=""padding: 8px 0; font-size: 15px; color: #5d7186;"">
                          <strong style=""color: #c1593f;"">{model.MaxUsers}</strong> kullanıcı
                        </td>
                      </tr>
                      <tr>
                        <td style=""padding: 8px 0; font-size: 15px; color: #5d7186;"">
                          <strong style=""color: #c1593f;"">{model.MaxDevices}</strong> cihaz lisansı
                        </td>
                      </tr>
                      <tr>
                        <td style=""padding: 8px 0; font-size: 15px; color: #5d7186;"">
                          Admin e-posta: <strong style=""color: #36404a;"">{adminEmail}</strong>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          
          {(string.IsNullOrWhiteSpace(model.AdminUsername) || string.IsNullOrWhiteSpace(model.AdminPassword) ? "" : $@"
          <!-- Admin Credentials -->
          <tr>
            <td style=""padding: 0 36px 20px;"">
              <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color: rgba(93, 113, 134, 0.08); border-radius: 12px; border: 1px solid rgba(93, 113, 134, 0.15);"">
                <tr>
                  <td style=""padding: 22px;"">
                    <h2 style=""margin: 0 0 12px; padding: 0; font-size: 20px; font-weight: 600; color: #36404a;"">Yönetim Paneli Giriş Bilgileri</h2>
                    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td style=""padding: 8px 0; font-size: 15px; color: #5d7186;"">
                          Kullanıcı Adı: <strong style=""color: #36404a; font-family: 'SFMono-Regular', Consolas, monospace;"">{model.AdminUsername}</strong>
                        </td>
                      </tr>
                      <tr>
                        <td style=""padding: 8px 0; font-size: 15px; color: #5d7186;"">
                          Parola: <strong style=""color: #c1593f; font-family: 'SFMono-Regular', Consolas, monospace;"">{model.AdminPassword}</strong>
                        </td>
                      </tr>
                    </table>
                    <p style=""margin: 12px 0 0; padding: 0; font-size: 13px; color: #5d7186; font-style: italic;"">
                      ⚠️ Bu bilgileri güvenli bir yerde saklayın. İlk girişten sonra parolanızı değiştirmenizi öneririz.
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          ")}
          
          <!-- License Key -->
          <tr>
            <td style=""padding: 0 36px 20px;"">
              <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color: #36404a; border-radius: 12px;"">
                <tr>
                  <td align=""center"" style=""padding: 18px 20px; font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace; letter-spacing: 0.24em; font-size: 17px; color: #ffffff; font-weight: 600;"">
                    {licenseKey}
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          
          <!-- Payment Meta -->
          <tr>
            <td style=""padding: 0 36px 20px;"">
              <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color: rgba(193, 89, 63, 0.05); border-radius: 12px; border: 1px solid rgba(193, 89, 63, 0.1);"">
                {paymentMeta}
              </table>
            </td>
          </tr>
          
          <!-- CTA Button -->
          <tr>
            <td align=""center"" style=""padding: 0 36px 20px;"">
              <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                <tr>
                  <td align=""center"" style=""padding: 14px 26px; border-radius: 8px; background-color: #c1593f;"">
                    <a href=""{portalUrl}"" target=""_blank"" rel=""noopener"" style=""display: inline-block; color: #ffffff; font-weight: 600; text-decoration: none; font-size: 15px; letter-spacing: 0.04em;"">
                      Yönetim Paneline Giriş Yap
                    </a>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          
          {(string.IsNullOrEmpty(devicePhotoBase64) ? "" : $@"
          <!-- Device Photo -->
          <tr>
            <td align=""center"" style=""padding: 0 36px 20px;"">
              <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
                <tr>
                  <td align=""center"" style=""padding: 0;"">
                    <img src=""data:image/png;base64,{devicePhotoBase64}"" alt=""De'Bakiim Cihaz Görüntüsü"" style=""display: block; max-width: 100%; width: auto; height: auto; border-radius: 12px; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);"">
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          ")}
          
          <!-- Steps -->
          <tr>
            <td style=""padding: 0 36px 20px; border-top: 1px solid rgba(193, 89, 63, 0.1);"">
              <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""margin-top: 24px;"">
                <tr>
                  <td>
                    <h3 style=""margin: 0 0 14px; padding: 0; font-size: 18px; font-weight: 600; color: #36404a;"">Sonraki adımlar</h3>
                    <ol style=""margin: 0; padding-left: 20px; color: #5d7186; line-height: 1.7; font-size: 15px;"">
                      <li style=""margin-bottom: 8px;"">Lisans anahtarınızı De'Bakiim yönetim paneline girerek cihaz aktivasyonunu başlatın.</li>
                      <li style=""margin-bottom: 8px;"">Admin hesabınızla oturum açın ve ekip üyelerinize kullanıcı rolleri atayın.</li>
                      <li style=""margin-bottom: 8px;"">Cihazlarınızı ve lokasyonlarınızı tanımladıktan sonra sesli sipariş akışını test edin.</li>
                      <li style=""margin-bottom: 8px;"">Herhangi bir soruda {supportEmail} adresi veya {supportPhone} üzerinden bizimle iletişime geçin.</li>
                    </ol>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          
          <!-- Footer -->
          <tr>
            <td style=""padding: 20px 36px 30px; border-top: 1px solid rgba(193, 89, 63, 0.1);"">
              <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
                <tr>
                  <td align=""center"" style=""font-size: 13px; color: #5d7186; line-height: 1.6;"">
                    <p style=""margin: 0 0 8px; padding: 0;"">
                      Destek ekibimiz yardım için burada: 
                      <a href=""mailto:{supportEmail}"" style=""color: #c1593f; text-decoration: none; font-weight: 600;"">{supportEmail}</a> · 
                      <a href=""tel:{supportPhone}"" style=""color: #c1593f; text-decoration: none; font-weight: 600;"">{supportPhone}</a>
                    </p>
                    <p style=""margin: 0; padding: 0;"">
                      © {DateTime.UtcNow.Year} De'Bakiim · Hosteagle Information Technologies
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
}

static string BuildTextBody(LicenseEmailModel model)
{
    var culture = CultureInfo.GetCultureInfo("tr-TR");
    var portalUrl = string.IsNullOrWhiteSpace(model.PortalUrl) ? "https://panel.debakiim.com" : model.PortalUrl;
    var supportEmail = string.IsNullOrWhiteSpace(model.SupportEmail) ? "destek@debakiim.com" : model.SupportEmail;
    var supportPhone = string.IsNullOrWhiteSpace(model.SupportPhone) ? "+90 212 000 00 00" : model.SupportPhone;
    var adminEmail = string.IsNullOrWhiteSpace(model.AdminEmail) ? model.ToEmail : model.AdminEmail;
    var subscriptionDate = model.SubscriptionDate.ToLocalTime().ToString("dd MMMM yyyy HH:mm", culture);
    var licenseKey = string.IsNullOrWhiteSpace(model.LicenseKey) ? "Lisans anahtarınız sistemde oluşturuldu." : model.LicenseKey.ToUpperInvariant();
    var amountText = model.Amount.HasValue ? $"{model.Amount.Value.ToString("N2", culture)} {model.Currency ?? "TRY"}" : null;
    var installmentText = model.Installment.HasValue && model.Installment.Value > 1
        ? $"{model.Installment.Value} taksit"
        : "Peşin";

    var builder = new StringBuilder();

    builder.AppendLine($"De'Bakiim Lisans Aktivasyonu - {model.PlanName}");
    builder.AppendLine(new string('-', 60));
    builder.AppendLine($"Firma: {model.TenantName}");
    builder.AppendLine($"Plan: {model.PlanName}");
    builder.AppendLine($"Kullanıcı Kotası: {model.MaxUsers}");
    builder.AppendLine($"Cihaz Kotası: {model.MaxDevices}");
    builder.AppendLine($"Admin E-posta: {adminEmail}");
    builder.AppendLine();
    builder.AppendLine($"Lisans Anahtarı: {licenseKey}");
    builder.AppendLine();
    builder.AppendLine("Abonelik Bilgileri:");
    builder.AppendLine($"- Tarih: {subscriptionDate}");

    if (!string.IsNullOrWhiteSpace(amountText))
    {
        builder.AppendLine($"- Ödenen Tutar: {amountText}");
        builder.AppendLine($"- Ödeme Şekli: {installmentText}");
    }

    if (!string.IsNullOrWhiteSpace(model.PaymentReference))
    {
        builder.AppendLine($"- Ödeme Referansı: {model.PaymentReference}");
    }

    builder.AppendLine();
    builder.AppendLine("Sonraki Adımlar:");
    builder.AppendLine("1. Yönetim panelinde lisans anahtarınızı girin.");
    builder.AppendLine("2. Admin hesabınızla oturum açarak kullanıcı rolleri tanımlayın.");
    builder.AppendLine("3. Cihaz aktivasyonlarını tamamlayın ve sistemi test edin.");
    builder.AppendLine("4. Destek için bizimle iletişime geçmekten çekinmeyin.");
    builder.AppendLine();
    builder.AppendLine($"Yönetim paneli: {portalUrl}");
    builder.AppendLine($"Destek e-posta: {supportEmail}");
    builder.AppendLine($"Destek telefon: {supportPhone}");
    builder.AppendLine();
    builder.AppendLine("De'Bakiim ve Hosteagle Information Technologies ekibi olarak başarılı kurulumlar dileriz!");

    return builder.ToString();
}

static async Task SendTestEmailAsync(MailSettingsDto settings, string toEmail)
{
    // Test email için template kullan
    var testModel = new LicenseEmailModel
    {
        TenantName = "Test Firma",
        PlanName = "Test Planı",
        LicenseKey = "TEST-XXXX-XXXX-XXXX",
        MaxUsers = 1,
        MaxDevices = 1,
        ToEmail = toEmail,
        ToName = "Test Kullanıcı",
        AdminEmail = toEmail,
        PortalUrl = "https://panel.debakiim.com",
        SupportEmail = "destek@debakiim.com",
        SupportPhone = "+90 212 000 00 00",
        SubscriptionDate = DateTime.UtcNow
    };

    using var message = new MailMessage
    {
        From = new MailAddress(settings.FromEmail, string.IsNullOrWhiteSpace(settings.FromName) ? "De'Bakiim" : settings.FromName),
        Subject = "De'Bakiim - Test Maili",
        BodyEncoding = Encoding.UTF8,
        SubjectEncoding = Encoding.UTF8,
        IsBodyHtml = true
    };

    message.To.Add(new MailAddress(toEmail));

    if (!string.IsNullOrWhiteSpace(settings.ReplyTo))
    {
        message.ReplyToList.Add(new MailAddress(settings.ReplyTo));
    }

    var htmlBody = BuildHtmlBody(testModel).Replace("Test Firma için abonelik tamamlandı", "Test Maili - SMTP Ayarları Başarılı");
    var textBody = BuildTextBody(testModel).Replace("De'Bakiim Lisans Aktivasyonu - Test Planı", "De'Bakiim - Test Maili");

    // Debug: HTML body uzunluğunu logla
    Console.WriteLine($"[DEBUG TEST] HTML body length: {htmlBody.Length} characters");
    Console.WriteLine($"[DEBUG TEST] HTML body starts with: {htmlBody.Substring(0, Math.Min(100, htmlBody.Length))}...");

    // HTML body'yi hem Body hem de AlternateView olarak ayarla (en güvenilir yöntem)
    // Body: Bazı email client'lar için gerekli (Outlook, Gmail)
    // AlternateView: Multipart/alternative için gerekli (tüm modern client'lar)
    message.Body = htmlBody;
    message.IsBodyHtml = true;
    message.BodyEncoding = Encoding.UTF8;
    
    // HTML AlternateView ekle (multipart/alternative için)
    var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html);
    htmlView.ContentType.CharSet = "UTF-8";
    message.AlternateViews.Add(htmlView);
    
    // Plain text alternatif view ekle (HTML desteklemeyen client'lar için fallback)
    var plainTextView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, MediaTypeNames.Text.Plain);
    plainTextView.ContentType.CharSet = "UTF-8";
    message.AlternateViews.Add(plainTextView);
    
    Console.WriteLine($"[DEBUG TEST] MailMessage configured: IsBodyHtml={message.IsBodyHtml}, AlternateViews.Count={message.AlternateViews.Count}");

    using var smtpClient = new SmtpClient(settings.Host, settings.Port)
    {
        EnableSsl = !string.Equals(settings.Security, "NONE", StringComparison.OrdinalIgnoreCase),
        DeliveryMethod = SmtpDeliveryMethod.Network,
        Credentials = new NetworkCredential(settings.Username, settings.Password),
        Timeout = 30000
    };

    // Port 465 için SSL/TLS ayarı
    if (settings.Port == 465)
    {
        smtpClient.EnableSsl = true;
    }
    else if (string.Equals(settings.Security, "STARTTLS", StringComparison.OrdinalIgnoreCase))
    {
        smtpClient.TargetName = $"STARTTLS/{settings.Host}";
    }

    await smtpClient.SendMailAsync(message);
}

// Request Models
internal class TestEmailRequest
{
    public MailSettingsDto? MailSettings { get; set; }
    public string ToEmail { get; set; } = "";
}

