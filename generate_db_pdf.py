from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.lib.units import cm, mm
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_LEFT, TA_CENTER, TA_RIGHT
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, Image
from reportlab.pdfgen import canvas
from reportlab.lib.utils import ImageReader
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from PIL import Image as PILImage, ImageEnhance
import os
from datetime import datetime

# Türkçe karakter desteği için font kayıt sistemi
def register_fonts():
    """Windows sistem fontlarını kaydet - Türkçe karakter desteği için"""
    try:
        # DejaVu Sans fontunu kaydet (eğer varsa)
        font_paths = [
            r"C:\Windows\Fonts\DejaVuSans.ttf",
            r"C:\Windows\Fonts\DejaVuSans-Bold.ttf",
            r"C:\Windows\Fonts\arial.ttf",
            r"C:\Windows\Fonts\arialbd.ttf",
        ]
        
        if os.path.exists(font_paths[0]):
            pdfmetrics.registerFont(TTFont('DejaVu', font_paths[0]))
            pdfmetrics.registerFont(TTFont('DejaVu-Bold', font_paths[1]))
            return 'DejaVu', 'DejaVu-Bold'
        elif os.path.exists(font_paths[2]):
            pdfmetrics.registerFont(TTFont('Arial', font_paths[2]))
            pdfmetrics.registerFont(TTFont('Arial-Bold', font_paths[3]))
            return 'Arial', 'Arial-Bold'
        else:
            # Varsayılan Helvetica kullan
            return 'Helvetica', 'Helvetica-Bold'
    except Exception as e:
        print(f"Font kayıt hatası: {e}")
        return 'Helvetica', 'Helvetica-Bold'

# Logo yolları
LOGO_DEBAKIIM = "frontend/Admin/src/assets/images/debakiim-logo.png"
LOGO_HOSTEAGLE = "frontend/Admin/src/assets/images/HOSTEAGLE.png"
LOGO_BG = "frontend/Admin/src/assets/images/hosteagle-logo.png"
OUTPUT_PATH = os.path.join(os.path.expanduser("~"), "Desktop", "BiSoyle_Veritabani_Dokumantasyonu.pdf")

# Veritabanı bilgileri
DATABASES = {
    "User Database (bisoyle_user)": {
        "description": "Kullanıcı yönetimi, roller, oturum yönetimi ve ses örnekleri",
        "tables": [
            {
                "name": "users",
                "description": "Sistem kullanıcıları",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("TenantId", "int?", "NULL = SuperAdmin, !NULL = Firma kullanıcısı", ""),
                    ("Username", "string(100)", "Required", "Unique (TenantId ile birlikte)"),
                    ("Email", "string(200)", "Required", "Unique (TenantId ile birlikte)"),
                    ("PasswordHash", "string", "Required", "Şifre hash"),
                    ("FirstName", "string(100)", "Optional", ""),
                    ("LastName", "string(100)", "Optional", ""),
                    ("Avatar", "string(500)", "Optional", "Avatar URL"),
                    ("Location", "string(200)", "Optional", ""),
                    ("Title", "string(100)", "Optional", ""),
                    ("Aktif", "bool", "Default: true", "Kullanıcı aktif mi"),
                    ("MaxVoiceSamples", "int?", "Default: 6", "Maksimum ses kaydı sayısı"),
                    ("CurrentVoiceSamples", "int?", "Default: 0", "Mevcut ses kaydı sayısı"),
                    ("OlusturmaTarihi", "DateTime", "Default: CURRENT_TIMESTAMP", ""),
                    ("SonGirisTarihi", "DateTime?", "Optional", ""),
                ],
                "relationships": [
                    "HasMany UserRoles",
                    "HasMany RefreshTokens",
                    "HasMany VoiceSamples"
                ]
            },
            {
                "name": "roles",
                "description": "Kullanıcı rolleri",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("RoleAdi", "string(50)", "Required, Unique", "Rol adı"),
                    ("Aciklama", "string(500)", "Optional", ""),
                    ("Aktif", "bool", "Default: true", ""),
                ],
                "relationships": [
                    "HasMany UserRoles"
                ]
            },
            {
                "name": "user_roles",
                "description": "Kullanıcı-Rol ilişkileri (Many-to-Many)",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("UserId", "int", "FK → users.Id", "Cascade Delete"),
                    ("RoleId", "int", "FK → roles.Id", "Cascade Delete"),
                    ("OlusturmaTarihi", "DateTime", "Default: CURRENT_TIMESTAMP", ""),
                ],
                "relationships": [
                    "BelongsTo User",
                    "BelongsTo Role",
                    "Unique Index: (UserId, RoleId)"
                ]
            },
            {
                "name": "refresh_tokens",
                "description": "JWT yenileme token'ları",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("UserId", "int", "FK → users.Id", "Cascade Delete"),
                    ("Token", "string", "Required, Unique", "Token string"),
                    ("OlusturmaTarihi", "DateTime", "Default: CURRENT_TIMESTAMP", ""),
                    ("ExpiresAt", "DateTime", "Required", "Token son kullanma tarihi"),
                    ("IsRevoked", "bool", "Default: false", "Token iptal edildi mi"),
                ],
                "relationships": [
                    "BelongsTo User"
                ]
            },
            {
                "name": "voice_samples",
                "description": "Kullanıcı ses örnekleri",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("UserId", "int", "FK → users.Id", "Cascade Delete"),
                    ("FilePath", "string(500)", "Required", "Ses dosyası yolu"),
                    ("Duration", "int", "Required", "Süre (saniye)"),
                    ("IsProcessed", "bool", "Default: false", "İşlendi mi"),
                    ("OlusturmaTarihi", "DateTime", "Default: CURRENT_TIMESTAMP", ""),
                ],
                "relationships": [
                    "BelongsTo User",
                    "Index: UserId"
                ]
            }
        ]
    },
    "Tenant Database (bisoyle_tenant)": {
        "description": "Firma/tenant yönetimi ve abonelik planları",
        "tables": [
            {
                "name": "tenants",
                "description": "Firmalar/Müşteriler",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("FirmaAdi", "string(200)", "Required, Unique", "Firma adı"),
                    ("TenantKey", "string(50)", "Required, Unique", "Unique key: firma-adi-slug"),
                    ("Email", "string(200)", "Required", "İletişim e-postası"),
                    ("Telefon", "string(50)", "Optional", ""),
                    ("Adres", "string(500)", "Optional", ""),
                    ("VergiNo", "string(50)", "Optional", ""),
                    ("Aktif", "bool", "Default: true", "Firma aktif mi"),
                    ("SesTanimaAktif", "bool", "Default: false", "Ses tanıma özelliği aktif mi"),
                    ("AktifAbonelikId", "int?", "Optional", "Aktif abonelik ID"),
                    ("OlusturmaTarihi", "DateTime", "Default: CURRENT_TIMESTAMP", ""),
                ],
                "relationships": [
                    "HasMany Subscriptions"
                ]
            },
            {
                "name": "subscription_plans",
                "description": "Abonelik planları",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("PlanAdi", "string(100)", "Required, Unique", "Plan adı (örn: '5 Çalışan')"),
                    ("MaxKullaniciSayisi", "int", "Required", "Maksimum kullanıcı sayısı (5, 10, 20, 50)"),
                    ("AylikUcret", "decimal(18,2)", "Required", "Aylık ücret"),
                    ("Ozellikler", "string?", "Optional", "JSON: features"),
                    ("Aktif", "bool", "Default: true", ""),
                ],
                "relationships": [
                    "HasMany Subscriptions"
                ]
            },
            {
                "name": "subscriptions",
                "description": "Firma abonelikleri",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("TenantId", "int", "FK → tenants.Id", "Cascade Delete"),
                    ("PlanId", "int", "FK → subscription_plans.Id", "Restrict Delete"),
                    ("BaslangicTarihi", "DateTime", "Required", ""),
                    ("BitisTarihi", "DateTime?", "Optional", ""),
                    ("Aktif", "bool", "Default: true", ""),
                    ("OlusturmaTarihi", "DateTime", "Default: CURRENT_TIMESTAMP", ""),
                ],
                "relationships": [
                    "BelongsTo Tenant",
                    "BelongsTo SubscriptionPlan"
                ]
            }
        ]
    },
    "Product Database (bisoyle_product)": {
        "description": "Ürün, kategori, ölçü birimi ve cihaz yönetimi",
        "tables": [
            {
                "name": "products",
                "description": "Ürünler",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("TenantId", "int", "Required", "Firma ID - Tenant izolasyonu için"),
                    ("KategoriId", "int?", "Optional", "Kategori ID"),
                    ("UrunAdi", "string(200)", "Required", "Unique (TenantId ile birlikte)"),
                    ("BirimFiyat", "decimal(18,2)", "Required", ""),
                    ("OlcuBirimi", "string(50)", "Default: 'Adet'", ""),
                    ("StokMiktari", "int", "Default: 0", ""),
                    ("Aktif", "bool", "Default: true", ""),
                    ("OlusturmaTarihi", "DateTime", "Default: CURRENT_TIMESTAMP", ""),
                    ("GuncellemeTarihi", "DateTime?", "Optional", ""),
                ],
                "relationships": [
                    "Index: TenantId",
                    "Index: Aktif"
                ]
            },
            {
                "name": "categories",
                "description": "Ürün kategorileri",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("TenantId", "int", "Required", "Firma ID - Tenant izolasyonu için"),
                    ("KategoriAdi", "string(100)", "Required", "Unique (TenantId ile birlikte)"),
                    ("Aciklama", "string?", "Optional", ""),
                    ("Aktif", "bool", "Default: true", ""),
                ],
                "relationships": [
                    "Index: TenantId"
                ]
            },
            {
                "name": "unit_of_measures",
                "description": "Ölçü birimleri",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("BirimAdi", "string(100)", "Required, Unique", "Ölçü birimi adı"),
                    ("Kisaltma", "string(20)", "Required, Unique", "Kısaltma (örn: 'kg', 'adet')"),
                    ("Aktif", "bool", "Default: true", ""),
                    ("OlusturmaTarihi", "DateTime", "Default: CURRENT_TIMESTAMP", ""),
                ],
                "relationships": []
            },
            {
                "name": "devices",
                "description": "Cihazlar (yazıcı, mikrofon vb.)",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("TenantId", "int", "Required", "Firma ID - Tenant izolasyonu için"),
                    ("CihazAdi", "string(100)", "Required", "Unique (TenantId ile birlikte)"),
                    ("CihazTipi", "string(50)", "Required", "Default: 'yazici' (yazici, mikrofon)"),
                    ("Marka", "string(100)", "Required", ""),
                    ("Model", "string(100)", "Required", ""),
                    ("BaglantiTipi", "string(50)", "Required", "Default: 'usb' (usb, bluetooth, wifi)"),
                    ("Durum", "string(20)", "Required", "Default: 'aktif' (aktif, pasif)"),
                    ("OlusturmaTarihi", "DateTime", "Default: CURRENT_TIMESTAMP", ""),
                ],
                "relationships": [
                    "Index: TenantId"
                ]
            }
        ]
    },
    "Receipt Database (bisoyle_receipt)": {
        "description": "Fiş/fatura yönetimi",
        "tables": [
            {
                "name": "receipts",
                "description": "Fişler",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("TenantId", "int", "Required", "Firma ID - Tenant izolasyonu için"),
                    ("UserId", "int", "Required", "Hangi kullanıcı yazdırdı"),
                    ("IslemKodu", "string(50)", "Required", "Unique (TenantId ile birlikte)"),
                    ("ToplamTutar", "decimal(18,2)", "Required", ""),
                    ("PdfPath", "string(500)", "Optional", "PDF dosya yolu"),
                    ("OlusturmaTarihi", "DateTime", "Required", ""),
                ],
                "relationships": [
                    "HasMany ReceiptItems",
                    "Index: TenantId"
                ]
            },
            {
                "name": "receipt_items",
                "description": "Fiş kalemleri",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("ReceiptId", "int", "FK → receipts.Id", "Cascade Delete"),
                    ("UrunId", "int", "Required", "Ürün ID (referans)"),
                    ("UrunAdi", "string(200)", "Required", "Ürün adı (snapshot)"),
                    ("Miktar", "int", "Required", ""),
                    ("BirimFiyat", "decimal(18,2)", "Required", ""),
                    ("OlcuBirimi", "string(50)", "Required", ""),
                    ("Subtotal", "decimal(18,2)", "Required", "Miktar × BirimFiyat"),
                ],
                "relationships": [
                    "BelongsTo Receipt"
                ]
            }
        ]
    },
    "Transaction Database (bisoyle_transaction)": {
        "description": "İşlem/transaksiyon yönetimi",
        "tables": [
            {
                "name": "transactions",
                "description": "İşlemler",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("TenantId", "int", "Required", "Firma ID - Tenant izolasyonu için"),
                    ("UserId", "int", "Required", "Hangi kullanıcı oluşturdu"),
                    ("IslemKodu", "string(50)", "Required", "Unique (TenantId ile birlikte)"),
                    ("IslemTipi", "string(50)", "Default: 'SATIS'", "İşlem tipi (SATIS, IADE vb.)"),
                    ("ToplamTutar", "decimal(18,2)", "Required", ""),
                    ("OdemeTipi", "string(50)", "Optional", "Ödeme tipi (NAKIT, KART vb.)"),
                    ("ReceiptId", "int?", "Optional", "İlişkili fiş ID"),
                    ("Aciklama", "string?", "Optional", ""),
                    ("OlusturmaTarihi", "DateTime", "Default: CURRENT_TIMESTAMP", ""),
                ],
                "relationships": [
                    "HasMany TransactionItems",
                    "Index: TenantId",
                    "Index: IslemTipi"
                ]
            },
            {
                "name": "transaction_items",
                "description": "İşlem kalemleri",
                "columns": [
                    ("Id", "int", "PK", "Primary Key"),
                    ("TransactionId", "int", "FK → transactions.Id", "Cascade Delete"),
                    ("UrunId", "int", "Required", "Ürün ID (referans)"),
                    ("UrunAdi", "string(200)", "Required", "Ürün adı (snapshot)"),
                    ("Miktar", "int", "Required", ""),
                    ("BirimFiyat", "decimal(18,2)", "Required", ""),
                    ("Subtotal", "decimal(18,2)", "Required", "Miktar × BirimFiyat"),
                ],
                "relationships": [
                    "BelongsTo Transaction"
                ]
            }
        ]
    }
}

class PDFPageTemplate:
    def __init__(self, canvas, doc, debakiim_logo, hosteagle_logo, bg_logo, normal_font, bold_font):
        self.canvas = canvas
        self.doc = doc
        self.debakiim_logo = debakiim_logo
        self.hosteagle_logo = hosteagle_logo
        self.bg_logo = bg_logo
        self.normal_font = normal_font
        self.bold_font = bold_font
        
    def __call__(self, canvas, doc):
        # Arka plan logo (gri)
        if os.path.exists(self.bg_logo):
            try:
                bg_img = PILImage.open(self.bg_logo).convert("RGBA")
                
                # Gri tonlamaya çevir ve şeffaflık ekle
                # RGB'yi griye çevir
                gray_img = bg_img.convert("L")
                # Alpha kanalını kullanarak şeffaflık ekle
                alpha = PILImage.new("L", gray_img.size, 40)  # %15-16 opacity için 40/255
                gray_img_rgba = gray_img.convert("RGBA")
                gray_img_rgba.putalpha(alpha)
                
                # Ortaya yerleştir, sayfanın ortasında - daha büyük
                img_width = 400  # Daha büyük
                img_height = 400
                x = (A4[0] - img_width) / 2
                y = (A4[1] - img_height) / 2
                
                canvas.saveState()
                # Alpha değerini canvas'ta da ayarla
                canvas.setFillAlpha(0.15)  # %15 opacity
                canvas.drawImage(ImageReader(gray_img_rgba), x, y, width=img_width, height=img_height, mask='auto')
                canvas.restoreState()
            except Exception as e:
                print(f"Arka plan logo hatası: {e}")
                pass
        
        # Üst bilgi - Sol üstte HOSTEAGLE
        if os.path.exists(self.hosteagle_logo):
            try:
                canvas.drawImage(self.hosteagle_logo, 20*mm, A4[1] - 30*mm, 
                               width=60*mm, height=20*mm, preserveAspectRatio=True)
            except:
                pass
        
        # Üst bilgi - Sağ üstte Debakiim
        if os.path.exists(self.debakiim_logo):
            try:
                canvas.drawImage(self.debakiim_logo, A4[0] - 50*mm, A4[1] - 30*mm, 
                               width=40*mm, height=20*mm, preserveAspectRatio=True)
            except:
                pass
        
        # Sayfa numarası - Türkçe karakter desteği ile
        canvas.saveState()
        try:
            canvas.setFont(self.normal_font, 9)
        except:
            canvas.setFont("Helvetica", 9)
        page_num = canvas.getPageNumber()
        text = f"Sayfa {page_num}"
        canvas.drawCentredString(A4[0] / 2.0, 20*mm, text)
        canvas.restoreState()

def create_pdf():
    # Türkçe karakter desteği için fontları kaydet
    normal_font, bold_font = register_fonts()
    print(f"Kullanılan fontlar: Normal={normal_font}, Bold={bold_font}")
    
    # Logo dosyalarını kontrol et
    if not os.path.exists(LOGO_DEBAKIIM):
        print(f"Uyarı: {LOGO_DEBAKIIM} bulunamadı")
    if not os.path.exists(LOGO_HOSTEAGLE):
        print(f"Uyarı: {LOGO_HOSTEAGLE} bulunamadı")
    if not os.path.exists(LOGO_BG):
        print(f"Uyarı: {LOGO_BG} bulunamadı")
    
    # PDF oluştur
    doc = SimpleDocTemplate(OUTPUT_PATH, pagesize=A4,
                            rightMargin=30*mm, leftMargin=30*mm,
                            topMargin=50*mm, bottomMargin=40*mm)
    
    # Stil tanımlamaları - Türkçe karakter desteği ile
    styles = getSampleStyleSheet()
    title_style = ParagraphStyle(
        'CustomTitle',
        parent=styles['Heading1'],
        fontSize=18,
        textColor=colors.HexColor('#2c3e50'),
        spaceAfter=12,
        alignment=TA_CENTER,
        fontName=bold_font,
        encoding='utf-8'
    )
    
    db_title_style = ParagraphStyle(
        'DBTitle',
        parent=styles['Heading2'],
        fontSize=16,
        textColor=colors.HexColor('#34495e'),
        spaceAfter=8,
        spaceBefore=20,
        fontName=bold_font,
        encoding='utf-8'
    )
    
    table_title_style = ParagraphStyle(
        'TableTitle',
        parent=styles['Heading3'],
        fontSize=14,
        textColor=colors.HexColor('#34495e'),
        spaceAfter=6,
        spaceBefore=12,
        fontName=bold_font,
        encoding='utf-8'
    )
    
    description_style = ParagraphStyle(
        'Description',
        parent=styles['Normal'],
        fontSize=11,
        textColor=colors.HexColor('#7f8c8d'),
        spaceAfter=12,
        fontStyle='italic',
        fontName=normal_font,
        encoding='utf-8'
    )
    
    normal_style = ParagraphStyle(
        'Normal',
        parent=styles['Normal'],
        fontName=normal_font,
        encoding='utf-8'
    )
    
    story = []
    
    # Kapak sayfası
    story.append(Spacer(1, 4*cm))
    story.append(Paragraph("BiSoyle POS Sistemi", title_style))
    story.append(Spacer(1, 1*cm))
    story.append(Paragraph("Veritabanı Dokümantasyonu", title_style))
    story.append(Spacer(1, 2*cm))
    date_style = ParagraphStyle('Date', parent=normal_style, fontSize=12, alignment=TA_CENTER, encoding='utf-8')
    story.append(Paragraph(f"Oluşturulma Tarihi: {datetime.now().strftime('%d.%m.%Y %H:%M')}", date_style))
    story.append(PageBreak())
    
    # İçindekiler
    story.append(Paragraph("İçindekiler", db_title_style))
    story.append(Spacer(1, 0.5*cm))
    
    toc_items = []
    for db_name in DATABASES.keys():
        toc_items.append([Paragraph(db_name, normal_style)])
    
    toc_table = Table(toc_items, colWidths=[17*cm])
    toc_table.setStyle(TableStyle([
        ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
        ('VALIGN', (0, 0), (-1, -1), 'TOP'),
        ('LEFTPADDING', (0, 0), (-1, -1), 0),
        ('RIGHTPADDING', (0, 0), (-1, -1), 0),
        ('TOPPADDING', (0, 0), (-1, -1), 6),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
    ]))
    story.append(toc_table)
    story.append(PageBreak())
    
    # Her veritabanı için
    for db_name, db_info in DATABASES.items():
        story.append(Paragraph(db_name, db_title_style))
        story.append(Paragraph(db_info["description"], description_style))
        story.append(Spacer(1, 0.5*cm))
        
        # Her tablo için
        for table in db_info["tables"]:
            story.append(Paragraph(f"Tablo: {table['name']}", table_title_style))
            story.append(Paragraph(table["description"], description_style))
            story.append(Spacer(1, 0.3*cm))
            
            # Kolonlar tablosu
            col_data = [["Kolon Adı", "Tip", "Özellikler", "Açıklama"]]
            
            for col_info in table["columns"]:
                # 3 veya 4 değerli tuple'ları handle et
                if len(col_info) == 3:
                    col_name, col_type, col_props = col_info
                    col_desc = ""
                else:
                    col_name, col_type, col_props, col_desc = col_info
                
                col_data.append([
                    Paragraph(f"<b>{col_name}</b>", normal_style),
                    Paragraph(col_type, normal_style),
                    Paragraph(col_props, normal_style),
                    Paragraph(col_desc, normal_style)
                ])
            
            col_table = Table(col_data, colWidths=[4*cm, 3*cm, 4*cm, 6*cm])
            col_table.setStyle(TableStyle([
                ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#3498db')),
                ('TEXTCOLOR', (0, 0), (-1, 0), colors.whitesmoke),
                ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
                ('FONTNAME', (0, 0), (-1, 0), bold_font),
                ('FONTSIZE', (0, 0), (-1, 0), 10),
                ('BOTTOMPADDING', (0, 0), (-1, 0), 12),
                ('TOPPADDING', (0, 0), (-1, 0), 12),
                ('BACKGROUND', (0, 1), (-1, -1), colors.white),
                ('GRID', (0, 0), (-1, -1), 1, colors.HexColor('#bdc3c7')),
                ('VALIGN', (0, 0), (-1, -1), 'TOP'),
                ('FONTNAME', (0, 1), (-1, -1), normal_font),
                ('LEFTPADDING', (0, 0), (-1, -1), 6),
                ('RIGHTPADDING', (0, 0), (-1, -1), 6),
                ('TOPPADDING', (0, 1), (-1, -1), 8),
                ('BOTTOMPADDING', (0, 1), (-1, -1), 8),
            ]))
            story.append(col_table)
            story.append(Spacer(1, 0.3*cm))
            
            # İlişkiler
            if table["relationships"]:
                story.append(Paragraph("<b>İlişkiler:</b>", normal_style))
                rel_data = []
                for rel in table["relationships"]:
                    rel_data.append([Paragraph(f"• {rel}", normal_style)])
                
                rel_table = Table(rel_data, colWidths=[17*cm])
                rel_table.setStyle(TableStyle([
                    ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
                    ('VALIGN', (0, 0), (-1, -1), 'TOP'),
                    ('LEFTPADDING', (0, 0), (-1, -1), 0),
                    ('RIGHTPADDING', (0, 0), (-1, -1), 0),
                    ('TOPPADDING', (0, 0), (-1, -1), 4),
                    ('BOTTOMPADDING', (0, 0), (-1, -1), 4),
                ]))
                story.append(rel_table)
            
            story.append(Spacer(1, 0.5*cm))
        
        story.append(PageBreak())
    
    # Sayfa template'i oluştur
    page_template = PDFPageTemplate(canvas, doc, LOGO_DEBAKIIM, LOGO_HOSTEAGLE, LOGO_BG, normal_font, bold_font)
    doc.build(story, onFirstPage=page_template, onLaterPages=page_template)
    
    print(f"PDF başarıyla oluşturuldu: {OUTPUT_PATH}")

if __name__ == "__main__":
    create_pdf()

