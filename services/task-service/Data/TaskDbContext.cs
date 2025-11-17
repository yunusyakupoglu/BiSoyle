using Microsoft.EntityFrameworkCore;

namespace BiSoyle.Task.Service.Data;

public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options)
    {
    }

    public DbSet<Task> Tasks { get; set; }
    public DbSet<TaskComment> TaskComments { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Task>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.CreatedByUserId).IsRequired();
            entity.Property(e => e.AssignedToUserId).IsRequired();
            entity.HasIndex(e => e.TenantId); // Tenant filter için
            entity.HasIndex(e => e.AssignedToUserId); // Assigned user filter için
            entity.HasIndex(e => e.Status); // Status filter için
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.Priority).HasMaxLength(50).HasDefaultValue("Medium");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.ToTable("task_comments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TaskId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Comment).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.Task)
                  .WithMany(t => t.Comments)
                  .HasForeignKey(e => e.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.UserId).IsRequired(); // Bildirimi alacak kullanıcı
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.CreatedDate);
        });
    }
}

public class Task
{
    public int Id { get; set; }
    public int TenantId { get; set; } // Firma ID - izolasyon için
    public int CreatedByUserId { get; set; } // Task oluşturan admin
    public int AssignedToUserId { get; set; } // Task atanan kullanıcı (admin/user)
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Accepted, InProgress, Completed
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    public DateTime? DueDate { get; set; } // Bitiş tarihi (opsiyonel)
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    
    public List<TaskComment> Comments { get; set; } = new();
}

public class TaskComment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; } // Yorum yapan kullanıcı
    public string Comment { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public Task Task { get; set; } = null!;
}

public class Notification
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int UserId { get; set; } // Bildirimi alacak kullanıcı
    public string Type { get; set; } = ""; // TaskAccepted, TaskCompleted, etc.
    public string Title { get; set; } = "";
    public string? Message { get; set; }
    public int? RelatedTaskId { get; set; } // İlgili görev ID
    public bool IsRead { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

