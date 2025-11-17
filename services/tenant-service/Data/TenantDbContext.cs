using Microsoft.EntityFrameworkCore;

namespace BiSoyle.Tenant.Service.Data;

public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<PlatformSetting> PlatformSettings { get; set; }
    public DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }
    
    // Device Binding & License Management
    public DbSet<License> Licenses { get; set; }
    public DbSet<LicenseActivation> LicenseActivations { get; set; }
    public DbSet<RebindRequest> RebindRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirmaAdi).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.FirmaAdi).IsUnique();
            entity.Property(e => e.TenantKey).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.TenantKey).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Telefon).HasMaxLength(50);
            entity.Property(e => e.Adres).HasMaxLength(500);
            entity.Property(e => e.VergiNo).HasMaxLength(50);
            entity.Property(e => e.Aktif).HasDefaultValue(true);
            entity.Property(e => e.SesTanimaAktif).HasDefaultValue(false);
            entity.Property(e => e.LicenseKey).HasMaxLength(100);
            entity.HasIndex(e => e.LicenseKey).IsUnique();
            entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // License entity configuration
        modelBuilder.Entity<License>(entity =>
        {
            entity.ToTable("licenses");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseKey).IsUnique();
            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.LicenseKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LicenseJson).IsRequired();
            entity.Property(e => e.Signature).IsRequired();
            entity.Property(e => e.MaxInstallations).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.ToleranceThreshold).HasDefaultValue(1.0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // LicenseActivation entity configuration
        modelBuilder.Entity<LicenseActivation>(entity =>
        {
            entity.ToTable("license_activations");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.License)
                  .WithMany(l => l.Activations)
                  .HasForeignKey(e => e.LicenseId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.LicenseId, e.DeviceFingerprint }).IsUnique();
            entity.Property(e => e.DeviceFingerprint).IsRequired().HasMaxLength(256);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ActivatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // RebindRequest entity configuration
        modelBuilder.Entity<RebindRequest>(entity =>
        {
            entity.ToTable("rebind_requests");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.License)
                  .WithMany()
                  .HasForeignKey(e => e.LicenseId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.Property(e => e.RequestedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("subscription_plans");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlanAdi).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.PlanAdi).IsUnique();
            entity.Property(e => e.MaxKullaniciSayisi).IsRequired();
            entity.Property(e => e.MaxBayiSayisi).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.AylikUcret).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Aktif).HasDefaultValue(true);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.Subscriptions)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Plan)
                  .WithMany()
                  .HasForeignKey(e => e.PlanId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.BaslangicTarihi).IsRequired();
            entity.Property(e => e.Aktif).HasDefaultValue(true);
        });

        modelBuilder.Entity<SubscriptionPayment>(entity =>
        {
            entity.ToTable("subscription_payments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tutar).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.KomisyonTutari).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.ParaBirimi).HasMaxLength(10).HasDefaultValue("TRY");
            entity.Property(e => e.KartSahibi).HasMaxLength(150);
            entity.Property(e => e.KartSon4).HasMaxLength(4);
            entity.Property(e => e.ReferansKodu).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.ReferansKodu).IsUnique();
            entity.Property(e => e.Durum).IsRequired().HasMaxLength(30);
            entity.Property(e => e.BankaAdi).HasMaxLength(100);
            entity.Property(e => e.Mesaj).HasMaxLength(250);
            entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne<Subscription>()
                  .WithOne()
                  .HasForeignKey<SubscriptionPayment>(p => p.SubscriptionId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.TenantId, e.PlanId, e.Kullanildi });
        });

        modelBuilder.Entity<PlatformSetting>(entity =>
        {
            entity.ToTable("platform_settings");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).HasMaxLength(2000);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}

public class Tenant
{
    public int Id { get; set; }
    public string FirmaAdi { get; set; } = "";
    public string TenantKey { get; set; } = ""; // Unique key: firma-adi-slug
    public string Email { get; set; } = "";
    public string? Telefon { get; set; }
    public string? Adres { get; set; }
    public string? VergiNo { get; set; }
    public bool Aktif { get; set; } = true;
    public bool SesTanimaAktif { get; set; } = false;
    public int? AktifAbonelikId { get; set; }
    public string? LicenseKey { get; set; } // Lisans anahtarı
    public DateTime? LicenseKeyGirisTarihi { get; set; } // İlk license key giriş tarihi
    public string? AdminInitialPassword { get; set; } // Tenant oluştururken üretilen admin parolası (email gönderilene kadar saklanır)
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    
    public List<Subscription> Subscriptions { get; set; } = new();
}

public class SubscriptionPlan
{
    public int Id { get; set; }
    public string PlanAdi { get; set; } = ""; // "5 Çalışan", "10 Çalışan"
    public int MaxKullaniciSayisi { get; set; } // 5, 10, 20, 50
    public int MaxBayiSayisi { get; set; } // 1, 3, 5, 7, 9, 11
    public decimal AylikUcret { get; set; }
    public string? Ozellikler { get; set; } // JSON: features
    public bool Aktif { get; set; } = true;
}

public class Subscription
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PlanId { get; set; }
    public DateTime BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public bool Aktif { get; set; } = true;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    
    public Tenant Tenant { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}

public class SubscriptionPayment
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PlanId { get; set; }
    public int? SubscriptionId { get; set; }
    public decimal Tutar { get; set; }
    public decimal KomisyonTutari { get; set; } = 0m;
    public string ParaBirimi { get; set; } = "TRY";
    public int TaksitSayisi { get; set; } = 1;
    public string KartSahibi { get; set; } = "";
    public string KartSon4 { get; set; } = "";
    public string ReferansKodu { get; set; } = "";
    public string Durum { get; set; } = "Beklemede"; // Basarili, Basarisiz
    public string? BankaAdi { get; set; }
    public string? Mesaj { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public DateTime? OnayTarihi { get; set; }
    public bool Kullanildi { get; set; } = false;
    public DateTime? KullanimTarihi { get; set; }
    public bool UcDSecure { get; set; } = true;
    public string? IslemNo { get; set; }
    public string? ProvizyonKodu { get; set; }
}

public class PlatformSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string? Value { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}






