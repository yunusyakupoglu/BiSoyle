using Microsoft.EntityFrameworkCore;

namespace BiSoyle.Product.Service.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<UnitOfMeasure> UnitOfMeasures { get; set; }
    public DbSet<Device> Devices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.HasIndex(e => e.TenantId); // Tenant filter için
            entity.HasIndex(e => new { e.TenantId, e.UrunAdi }); // Tenant içinde unique
            entity.Property(e => e.UrunAdi).IsRequired().HasMaxLength(200);
            entity.Property(e => e.BirimFiyat).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.OlcuBirimi).HasMaxLength(50);
            entity.Property(e => e.Aktif).HasDefaultValue(true);
            entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.Aktif);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.KategoriAdi }).IsUnique(); // Tenant içinde unique
            entity.Property(e => e.KategoriAdi).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Aktif).HasDefaultValue(true);
        });

        modelBuilder.Entity<UnitOfMeasure>(entity =>
        {
            entity.ToTable("unit_of_measures");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BirimAdi).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Kisaltma).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.BirimAdi).IsUnique();
            entity.HasIndex(e => e.Kisaltma).IsUnique();
            entity.Property(e => e.Aktif).HasDefaultValue(true);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("devices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.HasIndex(e => e.TenantId); // Tenant filter için
            entity.HasIndex(e => new { e.TenantId, e.CihazAdi }); // Tenant içinde unique
            entity.Property(e => e.CihazAdi).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CihazTipi).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Marka).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BaglantiTipi).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Durum).IsRequired().HasMaxLength(20);
            entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}

public class Product
{
    public int Id { get; set; }
    public int TenantId { get; set; } // Firma ID - izolasyon için
    public int? KategoriId { get; set; } // Kategori ID - opsiyonel
    public string UrunAdi { get; set; } = "";
    public decimal BirimFiyat { get; set; }
    public string OlcuBirimi { get; set; } = "Adet";
    public int StokMiktari { get; set; } = 0;
    public bool Aktif { get; set; } = true;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public DateTime? GuncellemeTarihi { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public int TenantId { get; set; } // Firma ID - izolasyon için
    public string KategoriAdi { get; set; } = "";
    public string? Aciklama { get; set; }
    public bool Aktif { get; set; } = true;
}

public class UnitOfMeasure
{
    public int Id { get; set; }
    public string BirimAdi { get; set; } = "";
    public string Kisaltma { get; set; } = "";
    public bool Aktif { get; set; } = true;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
}

public class Device
{
    public int Id { get; set; }
    public int TenantId { get; set; } // Firma ID - izolasyon için
    public string CihazAdi { get; set; } = "";
    public string CihazTipi { get; set; } = "yazici"; // yazici, mikrofon
    public string Marka { get; set; } = "";
    public string Model { get; set; } = "";
    public string BaglantiTipi { get; set; } = "usb"; // usb, bluetooth, wifi
    public string Durum { get; set; } = "aktif"; // aktif, pasif
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
}

