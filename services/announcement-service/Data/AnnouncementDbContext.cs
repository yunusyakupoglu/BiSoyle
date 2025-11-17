using Microsoft.EntityFrameworkCore;

namespace BiSoyle.Announcement.Service.Data;

public class AnnouncementDbContext : DbContext
{
    public AnnouncementDbContext(DbContextOptions<AnnouncementDbContext> options) : base(options)
    {
    }

    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<AnnouncementRead> AnnouncementReads { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.ToTable("announcements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Priority).HasMaxLength(50).HasDefaultValue("Normal");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.TenantId); // TenantId null ise tüm firmalara
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedDate);
        });

        modelBuilder.Entity<AnnouncementRead>(entity =>
        {
            entity.ToTable("announcement_reads");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AnnouncementId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ReadDate);
        });
    }
}

public class Announcement
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public int? TenantId { get; set; } // null ise tüm firmalara, değilse belirli bir firmaya
    public int CreatedByUserId { get; set; } // SuperAdmin ID
    public string Priority { get; set; } = "Normal"; // Normal, Yüksek, Kritik
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; } // Opsiyonel - duyurunun geçerlilik süresi
}

public class AnnouncementRead
{
    public int Id { get; set; }
    public int AnnouncementId { get; set; }
    public int UserId { get; set; }
    public DateTime ReadDate { get; set; } = DateTime.UtcNow;
}

