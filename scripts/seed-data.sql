-- BiSoyle - Örnek Veri Yükleme Script'i

-- bisoyle_product veritabanı
\c bisoyle_product;

-- Ölçü birimleri
INSERT INTO unit_of_measures ("BirimAdi", "Kisaltma", "Aktif", "OlusturmaTarihi") VALUES 
('Adet', 'Ad', true, NOW()),
('Kilogram', 'Kg', true, NOW()),
('Gram', 'Gr', true, NOW()),
('Litre', 'Lt', true, NOW()),
('Paket', 'Pkt', true, NOW()),
('Dilim', 'Dlm', true, NOW())
ON CONFLICT ("BirimAdi") DO NOTHING;

-- Kategoriler (TenantId=1 varsayılan firma için)
INSERT INTO categories ("TenantId", "KategoriAdi", "Aciklama", "Aktif") VALUES 
(1, 'Hamur İşleri', 'Tüm hamur işleri ve unlu mamuller', true),
(1, 'Tatlılar', 'Tatlı ürünler', true),
(1, 'Kahvaltılık', 'Kahvaltı ürünleri', true),
(1, 'İçecekler', 'Sıcak ve soğuk içecekler', true),
(1, 'Süt Ürünleri', 'Peynir, yoğurt vs.', true)
ON CONFLICT ("TenantId", "KategoriAdi") DO NOTHING;

-- Ürünler (TenantId=1 varsayılan firma için)
INSERT INTO products ("TenantId", "UrunAdi", "BirimFiyat", "OlcuBirimi", "StokMiktari", "Aktif", "OlusturmaTarihi") VALUES 
(1, 'Çikolatalı kruvasan', 15.00, 'Adet', 100, true, NOW()),
(1, 'Patatesli poğaça', 10.00, 'Adet', 200, true, NOW()),
(1, 'Sade simit', 5.00, 'Adet', 300, true, NOW()),
(1, 'Peynirli börek', 12.00, 'Adet', 150, true, NOW()),
(1, 'Cevizli baklava', 500.00, 'Kg', 50, true, NOW()),
(1, 'Sütlaç', 8.00, 'Adet', 80, true, NOW()),
(1, 'Beyaz peynir', 100.00, 'Kg', 30, true, NOW()),
(1, 'Kaşar peyniri', 120.00, 'Kg', 25, true, NOW()),
(1, 'Ayran', 3.00, 'Litre', 100, true, NOW()),
(1, 'Türk kahvesi', 15.00, 'Adet', 200, true, NOW())
ON CONFLICT ("TenantId", "UrunAdi") DO NOTHING;

-- bisoyle_transaction veritabanı
\c bisoyle_transaction;

-- Örnek işlemler
INSERT INTO transactions ("IslemKodu", "IslemTipi", "ToplamTutar", "OdemeTipi", "OlusturmaTarihi") VALUES
('TX-20251028001', 'SATIS', 1000.00, 'NAKİT', NOW() - INTERVAL '2 hours'),
('TX-20251028002', 'SATIS', 1500.00, 'KREDİ KARTI', NOW() - INTERVAL '1 hour'),
('TX-20251028003', 'SATIS', 750.00, 'NAKİT', NOW() - INTERVAL '30 minutes')
ON CONFLICT DO NOTHING;

SELECT 'Seed data loaded successfully!' as status;






