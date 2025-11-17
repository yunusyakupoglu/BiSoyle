using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Linq;
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

// CORS - Güvenli CORS ayarları
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Development: Frontend (Angular) ve diğer local servislere izin
            policy.WithOrigins("http://localhost:4200", "http://localhost:3000", "http://127.0.0.1:4200")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Production: Sadece belirtilen origin'lere izin
            var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(',') 
                ?? new[] { "http://localhost:4200" };
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Set port for User Service (5004)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5004";
app.Urls.Add($"http://localhost:{port}");

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    try
    {
        db.Database.Migrate();
        
        // Seed initial data if empty
        // Rollback TCKN: drop column and index if exist (idempotent)
        try
        {
            var ddlDrop = @"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_indexes WHERE indexname = 'ix_users_tckn'
    ) THEN
        DROP INDEX ix_users_tckn;
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'users' AND column_name = 'tckn'
    ) THEN
        ALTER TABLE users DROP COLUMN tckn;
    END IF;
END
$$;";
            db.Database.ExecuteSqlRaw(ddlDrop);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TCKN rollback DDL warning: {ex.Message}");
        }
        
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
                Avatar = "assets/images/profile-man.png",
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
        
        // Create or update SuperAdmin user with specified email
        var targetEmail = "superadmin@debakiim.com";
        var existingSuperAdmin = db.Users.FirstOrDefault(u => u.Email == targetEmail);
        if (existingSuperAdmin == null)
        {
            var superAdminRole = db.Roles.First(r => r.RoleAdi == "SuperAdmin");
            var newSuperAdmin = new User
            {
                TenantId = null, // NULL = SuperAdmin (firmaya bağlı değil)
                Username = "superadmin",
                Email = targetEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("super@debakiim"),
                FirstName = "Super",
                LastName = "Admin",
                Avatar = "assets/images/profile-man.png",
                Location = "Turkey",
                Title = "System Super Administrator",
                Aktif = true,
                OlusturmaTarihi = DateTime.UtcNow
            };
            db.Users.Add(newSuperAdmin);
            db.SaveChanges();
            
            db.UserRoles.Add(new UserRole
            {
                UserId = newSuperAdmin.Id,
                RoleId = superAdminRole.Id,
                OlusturmaTarihi = DateTime.UtcNow
            });
            db.SaveChanges();
            Console.WriteLine($"SuperAdmin kullanıcısı oluşturuldu: {targetEmail}");
        }
        else
        {
            // Update password if user exists
            existingSuperAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("super@debakiim");
            existingSuperAdmin.Avatar = "assets/images/profile-man.png";
            existingSuperAdmin.Aktif = true;
            db.SaveChanges();
            Console.WriteLine($"SuperAdmin kullanıcısı güncellendi: {targetEmail}");
        }
        
        // Create or update example company admin user (De'Bakiim firması için örnek kullanıcı)
        var exampleCompanyEmail = "debakiim-admin@debakiim.com";
        var exampleCompanyUser = db.Users.FirstOrDefault(u => u.Email == exampleCompanyEmail);
        var adminRole = db.Roles.First(r => r.RoleAdi == "Admin");
        if (exampleCompanyUser == null)
        {
            var newCompanyAdmin = new User
            {
                TenantId = 1, // Örnek firma ID'si
                Username = "debakiim-admin",
                Email = exampleCompanyEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                FirstName = "De'Bakiim",
                LastName = "Admin",
                Avatar = "assets/images/profile-man.png",
                Location = "Istanbul, Turkey",
                Title = "Firma Yöneticisi",
                Aktif = true,
                OlusturmaTarihi = DateTime.UtcNow
            };
            db.Users.Add(newCompanyAdmin);
            db.SaveChanges();
            
            db.UserRoles.Add(new UserRole
            {
                UserId = newCompanyAdmin.Id,
                RoleId = adminRole.Id,
                OlusturmaTarihi = DateTime.UtcNow
            });
            db.SaveChanges();
            Console.WriteLine($"Örnek firma admin kullanıcısı oluşturuldu: {exampleCompanyEmail}");
        }
        else
        {
            // Update password if user exists
            exampleCompanyUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            exampleCompanyUser.Avatar = "assets/images/profile-man.png";
            exampleCompanyUser.Aktif = true;
            exampleCompanyUser.TenantId = 1;
            db.SaveChanges();
            Console.WriteLine($"Örnek firma admin kullanıcısı güncellendi: {exampleCompanyEmail}");
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
app.MapPost("/api/auth/login", async (HttpContext context, UserDbContext dbContext, JwtService jwtService) =>
{
    try
    {
        // Enable buffering to allow body to be read
        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        
        // Read request body manually
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { detail = "İstek verisi boş!" });
        }
        
        // Deserialize request
        LoginRequest? request = null;
        try
        {
            request = System.Text.Json.JsonSerializer.Deserialize<LoginRequest>(body, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (System.Text.Json.JsonException ex)
        {
            return Results.BadRequest(new { detail = $"Geçersiz JSON formatı: {ex.Message}" });
        }
        
        // Null check: Request null olabilir
        if (request == null)
        {
            return Results.BadRequest(new { detail = "İstek verisi boş!" });
        }
        
        // Null check: Email ve Password
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { detail = "Email ve şifre gerekli!" });
        }
        
        var loginId = request.Email.Trim();
        var loginLower = loginId.ToLower();
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => 
                (u.Email != null && u.Email.ToLower() == loginLower) || 
                (u.Username != null && u.Username.ToLower() == loginLower));
        
        if (user == null)
        {
            return Results.Json(new { detail = "Geçersiz email veya şifre!" }, statusCode: 401);
        }
        
        // Null check: PasswordHash null olabilir
        var pwd = (request.Password ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(pwd, user.PasswordHash))
        {
            return Results.Json(new { detail = "Geçersiz email veya şifre!" }, statusCode: 401);
        }
        
        if (!user.Aktif)
        {
            return Results.BadRequest(new { detail = "Kullanıcı hesabı deaktif" });
        }
        
        // Null check: UserRoles null olabilir
        var roles = user.UserRoles?
            .Where(ur => ur != null && ur.Role != null)
            .Select(ur => ur.Role!.RoleAdi)
            .ToList() ?? new List<string>();
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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Login error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        
        // Database connection error check
        if (ex.Message.Contains("transient failure") || ex.Message.Contains("connection") || ex.Message.Contains("database"))
        {
            return Results.Problem($"Veritabanı bağlantı hatası: {ex.Message}. PostgreSQL'in çalıştığından emin olun (Port 5433).", statusCode: 500);
        }
        
        return Results.Problem($"Giriş hatası: {ex.Message}", statusCode: 500);
    }
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
        Avatar = "assets/images/profile-man.png",
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
    // Null check: User context
    if (context.User == null)
        return Results.Unauthorized();
    
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();
    
    // Null check: userId parse edilebilir mi?
    if (!int.TryParse(userId, out var userIdInt))
    {
        return Results.BadRequest(new { detail = "Geçersiz kullanıcı ID formatı!" });
    }
    
    var user = await dbContext.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.Id == userIdInt);
    
    if (user == null)
        return Results.NotFound();
    
    // Null check: UserRoles null olabilir
    var roles = user.UserRoles?
        .Where(ur => ur != null && ur.Role != null)
        .Select(ur => ur.Role!.RoleAdi)
        .ToList() ?? new List<string>();
    
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
        roles = roles
    });
}).RequireAuthorization();

// Get all users (Admin only) - TenantId bazlı filtreleme
app.MapGet("/api/users", async (HttpContext context, UserDbContext dbContext) =>
{
    // Null check: User context
    if (context.User == null)
        return Results.Unauthorized();
    
    // JWT token'dan kullanıcı bilgilerini al
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var tenantIdClaim = context.User.FindFirst("tenantId")?.Value;
    var userRoles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();
    
    // SuperAdmin kontrolü: tenantId "0" veya null ise SuperAdmin
    bool isSuperAdmin = userRoles.Contains("SuperAdmin") || 
                        tenantIdClaim == "0" || 
                        string.IsNullOrEmpty(tenantIdClaim);
    
    IQueryable<User> query = dbContext.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role);
    
    // Firma Admin ise sadece kendi tenant'ına ait kullanıcıları göster
    if (!isSuperAdmin && int.TryParse(tenantIdClaim, out int currentUserTenantId))
    {
        query = query.Where(u => u.TenantId == currentUserTenantId);
    }
    // SuperAdmin ise query değişmez, tüm kullanıcılar döner
    
    var users = await query
        .Select(u => new
        {
            id = u.Id,
            tenantId = u.TenantId,
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
            roles = u.UserRoles.Where(ur => ur.Role != null).Select(ur => ur.Role!.RoleAdi).ToList()
        })
        .ToListAsync();
    
    return Results.Ok(users);
}).RequireAuthorization(policy => policy.RequireRole("Admin", "SuperAdmin"));

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
    // Username tenant-scoped unique
    var usernameExistsSameTenant = await dbContext.Users.AnyAsync(u => 
        u.TenantId == request.TenantId && u.Username == request.Username);
    if (usernameExistsSameTenant)
    {
        return Results.BadRequest(new { message = "Aynı firmada bu kullanıcı adı zaten mevcut" });
    }
    // Email global unique
    if (!string.IsNullOrWhiteSpace(request.Email))
    {
        var emailExists = await dbContext.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return Results.BadRequest(new { message = "E-posta benzersiz olmalı (başka kullanıcıda kayıtlı)" });
    }
    
    var user = new User
    {
        TenantId = request.TenantId,
        Username = request.Username,
        Email = request.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        FirstName = request.FirstName,
        LastName = request.LastName,
        Avatar = string.IsNullOrWhiteSpace(request.Avatar)
            ? "assets/images/profile-man.png"
            : (request.Avatar.EndsWith("avatar-default.jpg") || request.Avatar.Contains("assets/images/users/"))
                ? "assets/images/profile-man.png"
                : request.Avatar,
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

// Internal endpoint: Tenant oluşturulurken otomatik admin kullanıcı oluşturmak için (Authorization yok)
app.MapPost("/api/users/internal/create-admin", async (CreateUserRequest request, UserDbContext dbContext) =>
{
    // Username tenant-scoped unique
    var usernameExistsSameTenant = await dbContext.Users.AnyAsync(u => 
        u.TenantId == request.TenantId && u.Username == request.Username);
    if (usernameExistsSameTenant)
    {
        return Results.BadRequest(new { message = "Aynı firmada bu kullanıcı adı zaten mevcut" });
    }
    // Email global unique
    if (!string.IsNullOrWhiteSpace(request.Email))
    {
        var emailExists = await dbContext.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return Results.BadRequest(new { message = "E-posta benzersiz olmalı (başka kullanıcıda kayıtlı)" });
    }
    
    var user = new User
    {
        TenantId = request.TenantId,
        Username = request.Username,
        Email = request.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        FirstName = request.FirstName,
        LastName = request.LastName,
        Avatar = string.IsNullOrWhiteSpace(request.Avatar)
            ? "assets/images/profile-man.png"
            : (request.Avatar.EndsWith("avatar-default.jpg") || request.Avatar.Contains("assets/images/users/"))
                ? "assets/images/profile-man.png"
                : request.Avatar,
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
    
    return Results.Created($"/api/users/{user.Id}", new { message = "Admin kullanıcı oluşturuldu", userId = user.Id });
});

// Internal: Get user by ID (no authorization required for internal services)
app.MapGet("/api/users/internal/{id}", async (int id, UserDbContext dbContext) =>
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
});

// Internal: Find user by tenant ID
app.MapGet("/api/users/internal/find-by-tenant/{tenantId}", async (int tenantId, UserDbContext dbContext) =>
{
    var user = await dbContext.Users
        .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => 
            u.TenantId == tenantId && 
            u.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleAdi != null && ur.Role.RoleAdi.ToLower() == "admin"));
    
    if (user == null)
    {
        // Fallback: Admin rolü bulunamazsa, tenant'a ait ilk aktif kullanıcıyı döndür
        user = await dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.TenantId == tenantId && u.Aktif)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync();
        
        if (user == null)
            return Results.NotFound();
    }
    
    return Results.Ok(new { id = user.Id, username = user.Username, email = user.Email });
});

// Internal: Find user by email (exact match, case-insensitive)
app.MapGet("/api/users/internal/find-by-email/{email}", async (string email, UserDbContext dbContext) =>
{
    if (string.IsNullOrWhiteSpace(email))
        return Results.BadRequest(new { message = "Email gerekli" });

    var normalized = email.Trim().ToLower();
    var user = await dbContext.Users
        .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);

    if (user == null)
        return Results.NotFound();

    return Results.Ok(new { id = user.Id, username = user.Username, email = user.Email, tenantId = user.TenantId });
});

// Internal: Update user password
app.MapPut("/api/users/internal/{id}/update-password", async (int id, UpdatePasswordRequest request, UserDbContext dbContext) =>
{
    var user = await dbContext.Users.FindAsync(id);
    
    if (user == null)
        return Results.NotFound(new { message = "Kullanıcı bulunamadı" });
    
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Parola güncellendi" });
});

// Internal: Set/Update user tenantId (maintenance)
app.MapPut("/api/users/internal/{id}/set-tenant/{tenantId}", async (int id, int tenantId, UserDbContext dbContext) =>
{
    var user = await dbContext.Users.FindAsync(id);
    if (user == null)
        return Results.NotFound(new { message = "Kullanıcı bulunamadı" });

    user.TenantId = tenantId;
    await dbContext.SaveChangesAsync();
    return Results.Ok(new { message = "TenantId güncellendi", userId = id, tenantId = tenantId });
});

app.MapPost("/api/users/{id}/generate-password", async (int id, HttpContext context, UserDbContext dbContext) =>
{
    var user = await dbContext.Users.FindAsync(id);

    if (user == null)
        return Results.NotFound(new { message = "Kullanıcı bulunamadı" });

    var caller = context.User;
    if (caller?.Identity == null || !caller.Identity.IsAuthenticated)
    {
        return Results.Unauthorized();
    }

    var callerTenantClaim = caller.FindFirst("tenantId")?.Value ?? "0";
    _ = int.TryParse(callerTenantClaim, out var callerTenantId);
    var isSuperAdmin = caller.IsInRole("SuperAdmin");

    if (!isSuperAdmin)
    {
        if (callerTenantId == 0 || user.TenantId != callerTenantId)
        {
            return Results.Forbid();
        }
    }

    var newPassword = PasswordGenerator.Generate();
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new
    {
        password = newPassword,
        message = "Parola başarıyla oluşturuldu"
    });
}).RequireAuthorization(policy => policy.RequireRole("Admin", "SuperAdmin"));

// Update user (Admin only)
app.MapPut("/api/users/{id}", async (int id, UpdateUserRequest request, UserDbContext dbContext) =>
{
    var user = await dbContext.Users.FindAsync(id);
    
    if (user == null)
        return Results.NotFound(new { message = "Kullanıcı bulunamadı" });
    
    // Tenant-scoped uniqueness for username
    var tenantId = user.TenantId;
    var usernameDuplicateSameTenant = await dbContext.Users.AnyAsync(u => 
        u.Id != id &&
        u.TenantId == tenantId &&
        u.Username == request.Username);
    if (usernameDuplicateSameTenant)
        return Results.BadRequest(new { message = "Aynı firmada bu kullanıcı adı zaten kullanılıyor" });
    
    // Global unique Email
    if (!string.IsNullOrWhiteSpace(request.Email))
    {
        var emailExists = await dbContext.Users.AnyAsync(u => u.Id != id && u.Email == request.Email);
        if (emailExists)
            return Results.BadRequest(new { message = "E-posta benzersiz olmalı (başka kullanıcıda kayıtlı)" });
    }
    
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

// Delete user (Admin only - hard delete)
app.MapDelete("/api/users/{id}", async (int id, HttpContext httpContext, UserDbContext dbContext) =>
{
    var user = await dbContext.Users.FindAsync(id);
    
    if (user == null)
        return Results.NotFound(new { message = "Kullanıcı bulunamadı" });
    
    // Kullanıcı kendi hesabını silemesin
    var currentUserIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (int.TryParse(currentUserIdClaim, out var currentUserId) && currentUserId == id)
    {
        return Results.BadRequest(new { message = "Kendi hesabınızı silemezsiniz." });
    }
    
    dbContext.Users.Remove(user);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Kullanıcı kalıcı olarak silindi" });
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

// Maintenance: Bulk fix avatars to profile-man.png where missing/invalid
app.MapPut("/api/users/fix-avatars", async (UserDbContext dbContext) =>
{
    var targets = await dbContext.Users
        .Where(u => string.IsNullOrWhiteSpace(u.Avatar) ||
                    u.Avatar.EndsWith("avatar-default.jpg") ||
                    u.Avatar.EndsWith("user-1.jpg") ||
                    u.Avatar.EndsWith("users/avatar-default.jpg") ||
                    u.Avatar.Contains("assets/images/users/"))
        .ToListAsync();
    
    int count = 0;
    foreach (var u in targets)
    {
        u.Avatar = "assets/images/profile-man.png";
        count++;
    }
    if (count > 0)
    {
        await dbContext.SaveChangesAsync();
    }
    return Results.Ok(new { updated = count });
});

// Get all roles
app.MapGet("/api/roles", async (UserDbContext dbContext) =>
{
    var roles = await dbContext.Roles.Where(r => r.Aktif).ToListAsync();
    return Results.Ok(roles);
}).RequireAuthorization();

app.MapControllers();

// Start the application (URL is set via ASPNETCORE_URLS or launchSettings.json)
app.Run();

static class PasswordGenerator
{
    private const string CharSet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%&*";
    private static readonly Random Random = new();

    public static string Generate(int length = 12)
    {
        return new string(Enumerable.Repeat(CharSet, length)
            .Select(s => s[Random.Next(s.Length)]).ToArray());
    }
}

// ============================================
// REQUEST/RESPONSE MODELS
// ============================================

public class UpdatePasswordRequest
{
    public string NewPassword { get; set; } = "";
}

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

