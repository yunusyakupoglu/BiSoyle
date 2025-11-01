using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using BiSoyle.User.Service.Data;
using BiSoyle.User.Service.Services;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "De' Bakiim - User Service API",
        Version = "v1",
        Description = "Multi-Tenant Kullanıcı Yönetimi ve Authentication API<br/><strong>BY HOSTEAGLE INFORMATION TECHNOLOGIES</strong>",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HostEagle Information Technologies",
            Email = "info@hosteagle.com"
        }
    });
});

// Database
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Service
builder.Services.AddSingleton<JwtService>();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "BiSoyleSecretKey123456789BiSoyleSecretKey123456789";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    try
    {
        db.Database.Migrate();
        
        // Seed initial data if empty
        if (!db.Roles.Any())
        {
            db.Roles.AddRange(new[]
            {
                new Role { RoleAdi = "SuperAdmin", Aciklama = "Süper yönetici - Tüm sistem erişimi", Aktif = true },
                new Role { RoleAdi = "Admin", Aciklama = "Firma yöneticisi - Kendi firması", Aktif = true },
                new Role { RoleAdi = "User", Aciklama = "Firma çalışanı - Sipariş ve fiş", Aktif = true }
            });
            db.SaveChanges();
        }
        
        // Create default SuperAdmin if no users
        if (!db.Users.Any())
        {
            var superAdminRole = db.Roles.First(r => r.RoleAdi == "SuperAdmin");
            var superAdminUser = new User
            {
                TenantId = null, // NULL = SuperAdmin (firmaya bağlı değil)
                Username = "superadmin",
                Email = "superadmin@bisoyle.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("super123"),
                FirstName = "Super",
                LastName = "Admin",
                Avatar = "assets/images/users/user-1.jpg",
                Location = "Turkey",
                Title = "System Super Administrator",
                Aktif = true,
                OlusturmaTarihi = DateTime.UtcNow
            };
            db.Users.Add(superAdminUser);
            db.SaveChanges();
            
            db.UserRoles.Add(new UserRole
            {
                UserId = superAdminUser.Id,
                RoleId = superAdminRole.Id,
                OlusturmaTarihi = DateTime.UtcNow
            });
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration error: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Login
app.MapPost("/api/auth/login", async (LoginRequest request, UserDbContext dbContext, JwtService jwtService) =>
{
    var user = await dbContext.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.Email);
    
    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }
    
    if (!user.Aktif)
    {
        return Results.BadRequest(new { message = "Kullanıcı hesabı deaktif" });
    }
    
    var roles = user.UserRoles.Select(ur => ur.Role.RoleAdi).ToList();
    var accessToken = jwtService.GenerateAccessToken(user, roles);
    var refreshToken = jwtService.GenerateRefreshToken();
    
    // Save refresh token
    dbContext.RefreshTokens.Add(new RefreshToken
    {
        UserId = user.Id,
        Token = refreshToken,
        OlusturmaTarihi = DateTime.UtcNow,
        ExpiresAt = jwtService.GetRefreshTokenExpiration()
    });
    
    // Update last login
    user.SonGirisTarihi = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new
    {
        user = new
        {
            id = user.Id,
            tenantId = user.TenantId,
            username = user.Username,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            avatar = user.Avatar,
            location = user.Location,
            title = user.Title,
            roles = roles
        },
        token = accessToken,
        refreshToken = refreshToken
    });
});

// Register
app.MapPost("/api/auth/register", async (RegisterRequest request, UserDbContext dbContext) =>
{
    // Check if user exists
    if (await dbContext.Users.AnyAsync(u => u.Email == request.Email || u.Username == request.Username))
    {
        return Results.BadRequest(new { message = "Kullanıcı zaten mevcut" });
    }
    
    var user = new User
    {
        Username = request.Username,
        Email = request.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        FirstName = request.FirstName,
        LastName = request.LastName,
        Avatar = "assets/images/users/avatar-default.jpg",
        Aktif = true,
        OlusturmaTarihi = DateTime.UtcNow
    };
    
    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();
    
    // Assign default role
    var defaultRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.RoleAdi == "User");
    if (defaultRole != null)
    {
        dbContext.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = defaultRole.Id,
            OlusturmaTarihi = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }
    
    return Results.Created($"/api/users/{user.Id}", new { message = "Kullanıcı oluşturuldu", userId = user.Id });
});

// Get current user
app.MapGet("/api/auth/me", async (HttpContext context, UserDbContext dbContext) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();
    
    var user = await dbContext.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
    
    if (user == null)
        return Results.NotFound();
    
    var roles = user.UserRoles.Select(ur => ur.Role.RoleAdi).ToList();
    
    return Results.Ok(new
    {
        id = user.Id,
        username = user.Username,
        email = user.Email,
        firstName = user.FirstName,
        lastName = user.LastName,
        avatar = user.Avatar,
        location = user.Location,
        title = user.Title,
        roles = roles
    });
}).RequireAuthorization();

// Get all users (Admin only)
app.MapGet("/api/users", async (UserDbContext dbContext) =>
{
    var users = await dbContext.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .Select(u => new
        {
            id = u.Id,
            username = u.Username,
            email = u.Email,
            firstName = u.FirstName,
            lastName = u.LastName,
            avatar = u.Avatar,
            location = u.Location,
            title = u.Title,
            aktif = u.Aktif,
            olusturmaTarihi = u.OlusturmaTarihi,
            sonGirisTarihi = u.SonGirisTarihi,
            roles = u.UserRoles.Select(ur => ur.Role.RoleAdi).ToList()
        })
        .ToListAsync();
    
    return Results.Ok(users);
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

// Get user by ID
app.MapGet("/api/users/{id}", async (int id, UserDbContext dbContext) =>
{
    var user = await dbContext.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.Id == id);
    
    if (user == null)
        return Results.NotFound(new { message = "Kullanıcı bulunamadı" });
    
    return Results.Ok(new
    {
        id = user.Id,
        tenantId = user.TenantId,
        username = user.Username,
        email = user.Email,
        firstName = user.FirstName,
        lastName = user.LastName,
        avatar = user.Avatar,
        location = user.Location,
        title = user.Title,
        aktif = user.Aktif,
        olusturmaTarihi = user.OlusturmaTarihi,
        sonGirisTarihi = user.SonGirisTarihi,
        roles = user.UserRoles.Select(ur => ur.Role.RoleAdi).ToList()
    });
}).RequireAuthorization();

// Create user (Admin only)
app.MapPost("/api/users", async (CreateUserRequest request, UserDbContext dbContext) =>
{
    // Check if user exists
    if (await dbContext.Users.AnyAsync(u => u.Email == request.Email || u.Username == request.Username))
    {
        return Results.BadRequest(new { message = "Kullanıcı zaten mevcut" });
    }
    
    var user = new User
    {
        TenantId = request.TenantId,
        Username = request.Username,
        Email = request.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        FirstName = request.FirstName,
        LastName = request.LastName,
        Avatar = request.Avatar ?? "assets/images/users/avatar-default.jpg",
        Location = request.Location,
        Title = request.Title,
        Aktif = true,
        OlusturmaTarihi = DateTime.UtcNow
    };
    
    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();
    
    // Assign roles
    if (request.RoleIds != null && request.RoleIds.Any())
    {
        foreach (var roleId in request.RoleIds)
        {
            dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
                OlusturmaTarihi = DateTime.UtcNow
            });
        }
        await dbContext.SaveChangesAsync();
    }
    
    return Results.Created($"/api/users/{user.Id}", new { message = "Kullanıcı oluşturuldu", userId = user.Id });
}).RequireAuthorization(policy => policy.RequireRole("Admin", "SuperAdmin"));

// Update user (Admin only)
app.MapPut("/api/users/{id}", async (int id, UpdateUserRequest request, UserDbContext dbContext) =>
{
    var user = await dbContext.Users.FindAsync(id);
    
    if (user == null)
        return Results.NotFound(new { message = "Kullanıcı bulunamadı" });
    
    user.Username = request.Username;
    user.Email = request.Email;
    user.FirstName = request.FirstName;
    user.LastName = request.LastName;
    user.Avatar = request.Avatar;
    user.Location = request.Location;
    user.Title = request.Title;
    
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Kullanıcı güncellendi" });
}).RequireAuthorization(policy => policy.RequireRole("Admin", "SuperAdmin"));

// Toggle user active status (Admin only)
app.MapPut("/api/users/{id}/toggle-active", async (int id, UserDbContext dbContext) =>
{
    var user = await dbContext.Users.FindAsync(id);
    
    if (user == null)
        return Results.NotFound(new { message = "Kullanıcı bulunamadı" });
    
    user.Aktif = !user.Aktif;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = $"Kullanıcı {(user.Aktif ? "aktif" : "pasif")} edildi", aktif = user.Aktif });
}).RequireAuthorization(policy => policy.RequireRole("Admin", "SuperAdmin"));

// Change password
app.MapPut("/api/users/{id}/change-password", async (int id, ChangePasswordRequest request, UserDbContext dbContext) =>
{
    var user = await dbContext.Users.FindAsync(id);
    
    if (user == null)
        return Results.NotFound(new { message = "Kullanıcı bulunamadı" });
    
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Şifre değiştirildi" });
}).RequireAuthorization();

// Delete user (Admin only - soft delete)
app.MapDelete("/api/users/{id}", async (int id, UserDbContext dbContext) =>
{
    var user = await dbContext.Users.FindAsync(id);
    
    if (user == null)
        return Results.NotFound(new { message = "Kullanıcı bulunamadı" });
    
    // Soft delete - just deactivate
    user.Aktif = false;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Kullanıcı silindi (deaktif edildi)" });
}).RequireAuthorization(policy => policy.RequireRole("Admin", "SuperAdmin"));

// Update user roles (Admin only)
app.MapPut("/api/users/{id}/roles", async (int id, UpdateUserRolesRequest request, UserDbContext dbContext) =>
{
    var user = await dbContext.Users
        .Include(u => u.UserRoles)
        .FirstOrDefaultAsync(u => u.Id == id);
    
    if (user == null)
        return Results.NotFound(new { message = "Kullanıcı bulunamadı" });
    
    // Remove all existing roles
    dbContext.UserRoles.RemoveRange(user.UserRoles);
    
    // Add new roles
    foreach (var roleId in request.RoleIds)
    {
        dbContext.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = roleId,
            OlusturmaTarihi = DateTime.UtcNow
        });
    }
    
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Kullanıcı rolleri güncellendi" });
}).RequireAuthorization(policy => policy.RequireRole("Admin", "SuperAdmin"));

// Get all roles
app.MapGet("/api/roles", async (UserDbContext dbContext) =>
{
    var roles = await dbContext.Roles.Where(r => r.Aktif).ToListAsync();
    return Results.Ok(roles);
}).RequireAuthorization();

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5004";
var url = $"http://0.0.0.0:{port}";
app.Run(url);

// Request Models
public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class CreateUserRequest
{
    public int? TenantId { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Avatar { get; set; }
    public string? Location { get; set; }
    public string? Title { get; set; }
    public List<int>? RoleIds { get; set; }
}

public class UpdateUserRequest
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Avatar { get; set; }
    public string? Location { get; set; }
    public string? Title { get; set; }
}

public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = "";
}

public class UpdateUserRolesRequest
{
    public List<int> RoleIds { get; set; } = new();
}

