using Microsoft.EntityFrameworkCore;

namespace BiSoyle.Ticket.Service.Data;

public class TicketDbContext : DbContext
{
    public TicketDbContext(DbContextOptions<TicketDbContext> options) : base(options)
    {
    }

    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<TicketComment> TicketComments { get; set; }
    public DbSet<TicketAttachment> TicketAttachments { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Open");
            entity.Property(e => e.Priority).HasMaxLength(50).HasDefaultValue("Medium");
            entity.Property(e => e.IsStarred).HasColumnName("isstarred").HasDefaultValue(false);
            entity.Property(e => e.IsImportant).HasColumnName("isimportant").HasDefaultValue(false);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedDate);
        });

        modelBuilder.Entity<TicketComment>(entity =>
        {
            entity.ToTable("ticket_comments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Comment).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.Ticket)
                  .WithMany(t => t.Comments)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TicketAttachment>(entity =>
        {
            entity.ToTable("ticket_attachments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.UploadedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.CommentId);
            entity.HasOne(e => e.Ticket)
                  .WithMany(t => t.Attachments)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Comment)
                  .WithMany(c => c.Attachments)
                  .HasForeignKey(e => e.CommentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.RelatedTicketId);
            entity.HasIndex(e => e.IsRead);
        });
    }
}

public class Ticket
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CreatedByUserId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Open"; // Open, InProgress, Resolved, Closed
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    public bool IsStarred { get; set; } = false;
    public bool IsImportant { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedDate { get; set; }
    public List<TicketComment> Comments { get; set; } = new();
    public List<TicketAttachment> Attachments { get; set; } = new();
}

public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int UserId { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public Ticket Ticket { get; set; } = null!;
    public List<TicketAttachment> Attachments { get; set; } = new();
}

public class TicketAttachment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int? CommentId { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string FileType { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public Ticket Ticket { get; set; } = null!;
    public TicketComment? Comment { get; set; }
}

public class Notification
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Message { get; set; }
    public int? RelatedTicketId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}






namespace BiSoyle.Ticket.Service.Data;

public class TicketDbContext : DbContext
{
    public TicketDbContext(DbContextOptions<TicketDbContext> options) : base(options)
    {
    }

    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<TicketComment> TicketComments { get; set; }
    public DbSet<TicketAttachment> TicketAttachments { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Open");
            entity.Property(e => e.Priority).HasMaxLength(50).HasDefaultValue("Medium");
            entity.Property(e => e.IsStarred).HasColumnName("isstarred").HasDefaultValue(false);
            entity.Property(e => e.IsImportant).HasColumnName("isimportant").HasDefaultValue(false);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedDate);
        });

        modelBuilder.Entity<TicketComment>(entity =>
        {
            entity.ToTable("ticket_comments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Comment).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.Ticket)
                  .WithMany(t => t.Comments)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TicketAttachment>(entity =>
        {
            entity.ToTable("ticket_attachments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.UploadedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.CommentId);
            entity.HasOne(e => e.Ticket)
                  .WithMany(t => t.Attachments)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Comment)
                  .WithMany(c => c.Attachments)
                  .HasForeignKey(e => e.CommentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.RelatedTicketId);
            entity.HasIndex(e => e.IsRead);
        });
    }
}

public class Ticket
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CreatedByUserId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Open"; // Open, InProgress, Resolved, Closed
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    public bool IsStarred { get; set; } = false;
    public bool IsImportant { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedDate { get; set; }
    public List<TicketComment> Comments { get; set; } = new();
    public List<TicketAttachment> Attachments { get; set; } = new();
}

public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int UserId { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public Ticket Ticket { get; set; } = null!;
    public List<TicketAttachment> Attachments { get; set; } = new();
}

public class TicketAttachment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int? CommentId { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string FileType { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public Ticket Ticket { get; set; } = null!;
    public TicketComment? Comment { get; set; }
}

public class Notification
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Message { get; set; }
    public int? RelatedTicketId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}







