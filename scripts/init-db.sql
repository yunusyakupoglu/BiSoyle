-- BiSoyle Database Initialization Script
-- Creates databases for all microservices

-- Receipt Service Database
CREATE DATABASE bisoyle_receipt;

-- Product Service Database
CREATE DATABASE bisoyle_product;

-- Transaction Service Database
CREATE DATABASE bisoyle_transaction;

-- User Service Database
CREATE DATABASE bisoyle_user;

-- Tenant Service Database
CREATE DATABASE bisoyle_tenant;

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE bisoyle_receipt TO postgres;
GRANT ALL PRIVILEGES ON DATABASE bisoyle_product TO postgres;
GRANT ALL PRIVILEGES ON DATABASE bisoyle_transaction TO postgres;
GRANT ALL PRIVILEGES ON DATABASE bisoyle_user TO postgres;
GRANT ALL PRIVILEGES ON DATABASE bisoyle_tenant TO postgres;

\c bisoyle_receipt;

-- Receipt Service Tables
CREATE TABLE IF NOT EXISTS receipts (
    id SERIAL PRIMARY KEY,
    islem_kodu VARCHAR(50) NOT NULL UNIQUE,
    toplam_tutar DECIMAL(18,2) NOT NULL,
    pdf_path VARCHAR(500),
    olusturma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS receipt_items (
    id SERIAL PRIMARY KEY,
    receipt_id INTEGER NOT NULL REFERENCES receipts(id) ON DELETE CASCADE,
    urun_id INTEGER NOT NULL,
    urun_adi VARCHAR(200) NOT NULL,
    miktar INTEGER NOT NULL,
    birim_fiyat DECIMAL(18,2) NOT NULL,
    olcu_birimi VARCHAR(50),
    subtotal DECIMAL(18,2) NOT NULL
);

CREATE INDEX idx_receipts_islem_kodu ON receipts(islem_kodu);
CREATE INDEX idx_receipt_items_receipt_id ON receipt_items(receipt_id);

\c bisoyle_product;

-- Product Service Tables
CREATE TABLE IF NOT EXISTS products (
    id SERIAL PRIMARY KEY,
    urun_adi VARCHAR(200) NOT NULL,
    birim_fiyat DECIMAL(18,2) NOT NULL,
    olcu_birimi VARCHAR(50) DEFAULT 'Adet',
    stok_miktari INTEGER DEFAULT 0,
    aktif BOOLEAN DEFAULT TRUE,
    olusturma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncelleme_tarihi TIMESTAMP
);

CREATE TABLE IF NOT EXISTS categories (
    id SERIAL PRIMARY KEY,
    kategori_adi VARCHAR(100) NOT NULL UNIQUE,
    aciklama TEXT,
    aktif BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS product_categories (
    product_id INTEGER NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    category_id INTEGER NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
    PRIMARY KEY (product_id, category_id)
);

-- Insert sample products
INSERT INTO products (urun_adi, birim_fiyat, olcu_birimi, stok_miktari) VALUES
('Çikolatalı kruvasan', 15.00, 'Adet', 100),
('Cevizli baklava', 500.00, 'Kg', 50),
('Patatesli poğaça', 10.00, 'Adet', 200),
('Beyaz peynir', 100.00, 'Kg', 30),
('Sucuk', 150.00, 'Kg', 25),
('Peynirli börek', 12.00, 'Adet', 150),
('Sade simit', 5.00, 'Adet', 300),
('Açma', 8.00, 'Adet', 180);

-- Insert sample categories
INSERT INTO categories (kategori_adi, aciklama) VALUES
('Hamur İşleri', 'Tüm hamur işleri ve unlu mamuller'),
('Tatlılar', 'Tatlı ürünler'),
('Kahvaltılık', 'Kahvaltı ürünleri'),
('Ekmek Çeşitleri', 'Ekmek ve simit türleri');

CREATE INDEX idx_products_urun_adi ON products(urun_adi);
CREATE INDEX idx_products_aktif ON products(aktif);

\c bisoyle_transaction;

-- Transaction Service Tables
CREATE TABLE IF NOT EXISTS transactions (
    id SERIAL PRIMARY KEY,
    islem_kodu VARCHAR(50) NOT NULL UNIQUE,
    islem_tipi VARCHAR(50) NOT NULL DEFAULT 'SATIS',
    toplam_tutar DECIMAL(18,2) NOT NULL,
    odeme_tipi VARCHAR(50),
    receipt_id INTEGER,
    aciklama TEXT,
    olusturma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS transaction_items (
    id SERIAL PRIMARY KEY,
    transaction_id INTEGER NOT NULL REFERENCES transactions(id) ON DELETE CASCADE,
    urun_id INTEGER NOT NULL,
    urun_adi VARCHAR(200) NOT NULL,
    miktar INTEGER NOT NULL,
    birim_fiyat DECIMAL(18,2) NOT NULL,
    subtotal DECIMAL(18,2) NOT NULL
);

-- Insert sample transactions
INSERT INTO transactions (islem_kodu, islem_tipi, toplam_tutar, odeme_tipi) VALUES
('FS-20251028001', 'SATIS', 1000.00, 'NAKİT'),
('FS-20251028002', 'SATIS', 1500.00, 'KREDİ KARTI'),
('FS-20251028003', 'SATIS', 750.00, 'NAKİT');

INSERT INTO transaction_items (transaction_id, urun_id, urun_adi, miktar, birim_fiyat, subtotal) VALUES
(1, 1, 'Çikolatalı kruvasan', 10, 15.00, 150.00),
(1, 2, 'Cevizli baklava', 1, 500.00, 500.00),
(2, 3, 'Patatesli poğaça', 20, 10.00, 200.00),
(2, 4, 'Beyaz peynir', 1, 100.00, 100.00),
(3, 5, 'Sucuk', 2, 150.00, 300.00);

CREATE INDEX idx_transactions_islem_kodu ON transactions(islem_kodu);
CREATE INDEX idx_transactions_islem_tipi ON transactions(islem_tipi);
CREATE INDEX idx_transaction_items_transaction_id ON transaction_items(transaction_id);






