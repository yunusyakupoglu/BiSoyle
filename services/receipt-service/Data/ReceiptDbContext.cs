using Microsoft.EntityFrameworkCore;

namespace BiSoyle.Receipt.Service.Data;

public class ReceiptDbContext : DbContext
{
    public ReceiptDbContext(DbContextOptions<ReceiptDbContext> options) : base(options)
    {
    }

    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<ReceiptItem> ReceiptItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.ToTable("receipts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.HasIndex(e => e.TenantId); // Tenant filter için
            entity.HasIndex(e => new { e.TenantId, e.IslemKodu }).IsUnique(); // Tenant içinde unique
            entity.Property(e => e.IslemKodu).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PdfPath).HasMaxLength(500);
            entity.Property(e => e.OlusturmaTarihi).IsRequired();
            entity.Property(e => e.ToplamTutar).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ReceiptItem>(entity =>
        {
            entity.ToTable("receipt_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UrunAdi).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OlcuBirimi).HasMaxLength(50);
            entity.Property(e => e.BirimFiyat).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Receipt)
                  .WithMany(r => r.Items)
                  .HasForeignKey(e => e.ReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public class Receipt
{
    public int Id { get; set; }
    public int TenantId { get; set; } // Firma ID - izolasyon için
    public int UserId { get; set; } // Hangi kullanıcı yazdırdı
    public string IslemKodu { get; set; } = "";
    public double ToplamTutar { get; set; }
    public string PdfPath { get; set; } = "";
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    
    public List<ReceiptItem> Items { get; set; } = new();
}

public class ReceiptItem
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public int UrunId { get; set; }
    public string UrunAdi { get; set; } = "";
    public int Miktar { get; set; }
    public double BirimFiyat { get; set; }
    public string OlcuBirimi { get; set; } = "";
    public double Subtotal { get; set; }
    
    public Receipt Receipt { get; set; } = null!;
}





