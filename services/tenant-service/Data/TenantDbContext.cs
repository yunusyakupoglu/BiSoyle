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
            entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("subscription_plans");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlanAdi).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.PlanAdi).IsUnique();
            entity.Property(e => e.MaxKullaniciSayisi).IsRequired();
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
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    
    public List<Subscription> Subscriptions { get; set; } = new();
}

public class SubscriptionPlan
{
    public int Id { get; set; }
    public string PlanAdi { get; set; } = ""; // "5 Çalışan", "10 Çalışan"
    public int MaxKullaniciSayisi { get; set; } // 5, 10, 20, 50
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





