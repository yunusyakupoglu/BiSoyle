using Microsoft.EntityFrameworkCore;

namespace BiSoyle.Receipt.Service.Data;

public class ReceiptDbContext : DbContext
{
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<ReceiptItem> ReceiptItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Database=bisoyle_receipt;Username=postgres;Password=1234");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.ToTable("receipts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IslemKodu).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PdfPath).HasMaxLength(500);
            entity.Property(e => e.OlusturmaTarihi).IsRequired();
        });

        modelBuilder.Entity<ReceiptItem>(entity =>
        {
            entity.ToTable("receipt_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UrunAdi).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OlcuBirimi).HasMaxLength(50);
            entity.HasOne(e => e.Receipt)
                  .WithMany(r => r.Items)
                  .HasForeignKey(e => e.ReceiptId);
        });
    }
}

public class Receipt
{
    public int Id { get; set; }
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





