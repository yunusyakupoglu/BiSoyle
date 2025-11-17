using Microsoft.EntityFrameworkCore;

namespace BiSoyle.Transaction.Service.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionItem> TransactionItems { get; set; }
    public DbSet<Expense> Expenses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.HasIndex(e => e.TenantId); // Tenant filter için
            entity.HasIndex(e => new { e.TenantId, e.IslemKodu }).IsUnique(); // Tenant içinde unique
            entity.Property(e => e.IslemKodu).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IslemTipi).HasMaxLength(50).HasDefaultValue("SATIS");
            entity.Property(e => e.ToplamTutar).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.OdemeTipi).HasMaxLength(50);
            entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.IslemTipi);
        });

        modelBuilder.Entity<TransactionItem>(entity =>
        {
            entity.ToTable("transaction_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UrunAdi).IsRequired().HasMaxLength(200);
            entity.Property(e => e.BirimFiyat).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Transaction)
                  .WithMany(t => t.Items)
                  .HasForeignKey(e => e.TransactionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("expenses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.HasIndex(e => e.TenantId); // Tenant filter için
            entity.Property(e => e.GiderAdi).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Tutar).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Kategori).HasMaxLength(100);
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.OlusturmaTarihi);
        });
    }
}

public class Transaction
{
    public int Id { get; set; }
    public int TenantId { get; set; } // Firma ID - izolasyon için
    public int UserId { get; set; } // Hangi kullanıcı oluşturdu
    public string IslemKodu { get; set; } = "";
    public string IslemTipi { get; set; } = "SATIS";
    public decimal ToplamTutar { get; set; }
    public string? OdemeTipi { get; set; }
    public int? ReceiptId { get; set; }
    public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    
    public List<TransactionItem> Items { get; set; } = new();
}

public class TransactionItem
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int UrunId { get; set; }
    public string UrunAdi { get; set; } = "";
    public int Miktar { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal Subtotal { get; set; }
    
    public Transaction Transaction { get; set; } = null!;
}

public class Expense
{
    public int Id { get; set; }
    public int TenantId { get; set; } // Firma ID - izolasyon için
    public int UserId { get; set; } // Hangi kullanıcı oluşturdu
    public string GiderAdi { get; set; } = "";
    public decimal Tutar { get; set; }
    public string? Kategori { get; set; } // Örn: Kira, Elektrik, Su, Personel, vb.
    public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
}


