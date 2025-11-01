using Microsoft.EntityFrameworkCore;

namespace BiSoyle.User.Service.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<VoiceSample> VoiceSamples { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired(false); // NULL = SuperAdmin
            entity.HasIndex(e => new { e.TenantId, e.Username }); // Composite index
            entity.HasIndex(e => new { e.TenantId, e.Email }); // Composite index
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.MaxVoiceSamples).HasDefaultValue(6);
            entity.Property(e => e.CurrentVoiceSamples).HasDefaultValue(0);
            entity.Property(e => e.Aktif).HasDefaultValue(true);
            entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoleAdi).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.RoleAdi).IsUnique();
            entity.Property(e => e.Aciklama).HasMaxLength(500);
            entity.Property(e => e.Aktif).HasDefaultValue(true);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired();
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VoiceSample>(entity =>
        {
            entity.ToTable("voice_samples");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Duration).IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany(u => u.VoiceSamples)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
        });
    }
}

public class VoiceSample
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FilePath { get; set; } = "";
    public int Duration { get; set; } // seconds
    public bool IsProcessed { get; set; } = false;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    
    public User User { get; set; } = null!;
}

public class User
{
    public int Id { get; set; }
    public int? TenantId { get; set; } // NULL = SuperAdmin, !NULL = Firma kullanıcısı
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Avatar { get; set; }
    public string? Location { get; set; }
    public string? Title { get; set; }
    public bool Aktif { get; set; } = true;
    public int? MaxVoiceSamples { get; set; } = 6; // Max ses kaydı sayısı
    public int? CurrentVoiceSamples { get; set; } = 0; // Mevcut ses kaydı sayısı
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public DateTime? SonGirisTarihi { get; set; }
    
    public List<UserRole> UserRoles { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = new();
    public List<VoiceSample> VoiceSamples { get; set; } = new();
}

public class Role
{
    public int Id { get; set; }
    public string RoleAdi { get; set; } = "";
    public string? Aciklama { get; set; }
    public bool Aktif { get; set; } = true;
    
    public List<UserRole> UserRoles { get; set; } = new();
}

public class UserRole
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = "";
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    
    public User User { get; set; } = null!;
}


