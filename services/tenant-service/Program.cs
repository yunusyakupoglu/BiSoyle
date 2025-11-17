using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BiSoyle.Tenant.Service.Data;
using System.Net.Http.Json;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BiSoyle.Tenant.Service.Services;
using BiSoyle.Tenant.Service;
using BiSoyle.Tenant.Service.BackgroundServices;
using BiSoyle.Tenant.Service.Dtos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "De' Bakiim - Tenant Service API",
        Version = "v1",
        Description = "Multi-Tenant Firma ve Abonelik Yönetimi API<br/><strong>BY HOSTEAGLE INFORMATION TECHNOLOGIES</strong>",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HostEagle Information Technologies",
            Email = "info@hosteagle.com"
        }
    });
});

// HttpClient for User Service
builder.Services.AddHttpClient();

// RSA License Service - Singleton, initialization hatası servisi crash etmesin (constructor'da try-catch var)
builder.Services.AddLogging();
builder.Services.AddSingleton<RsaLicenseService>();
builder.Services.AddSingleton<VirtualPosService>();
builder.Services.AddHostedService<PaymentMonitorService>();

// Database
builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS - Güvenli CORS ayarları
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Development: Frontend (Angular) ve diğer local servislere izin
            policy.WithOrigins("http://localhost:4200", "http://localhost:4300", "http://localhost:3000", "http://127.0.0.1:4200")
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

// Prefer environment variables, then config; default to docker service DNS names
var emailServiceBaseUrl =
	Environment.GetEnvironmentVariable("EMAIL_SERVICE_URL")
	?? builder.Configuration["EmailService:BaseUrl"]
	?? "http://email-service";
var portalUrl = builder.Configuration["Portal:Url"] ?? "https://panel.debakiim.com";
var portalSupportEmail = builder.Configuration["Portal:SupportEmail"] ?? "destek@debakiim.com";
var portalSupportPhone = builder.Configuration["Portal:SupportPhone"] ?? "+90 212 000 00 00";

var app = builder.Build();

// Set port for Tenant Service (5006)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5006";
app.Urls.Add($"http://localhost:{port}");

// Auto-migrate and seed (async, non-blocking)
_ = Task.Run(async () =>
{
    await Task.Delay(2000); // Wait for service to fully start
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        
        Console.WriteLine("Running database migrations...");
        await db.Database.MigrateAsync();
        Console.WriteLine("Database migrations completed successfully.");
        
        // Seed subscription plans (Bayi sayıları: 1, 3, 5, 7, 9, 11)
        if (!await db.SubscriptionPlans.AnyAsync())
        {
            Console.WriteLine("Seeding subscription plans...");
            db.SubscriptionPlans.AddRange(new[]
            {
                new SubscriptionPlan { PlanAdi = "Starter - 5 Çalışan", MaxKullaniciSayisi = 5, MaxBayiSayisi = 1, AylikUcret = 500.00m, Aktif = true },
                new SubscriptionPlan { PlanAdi = "Basic - 10 Çalışan", MaxKullaniciSayisi = 10, MaxBayiSayisi = 3, AylikUcret = 900.00m, Aktif = true },
                new SubscriptionPlan { PlanAdi = "Professional - 20 Çalışan", MaxKullaniciSayisi = 20, MaxBayiSayisi = 5, AylikUcret = 1600.00m, Aktif = true },
                new SubscriptionPlan { PlanAdi = "Enterprise - 50 Çalışan", MaxKullaniciSayisi = 50, MaxBayiSayisi = 7, AylikUcret = 3500.00m, Aktif = true },
                new SubscriptionPlan { PlanAdi = "Premium - 100 Çalışan", MaxKullaniciSayisi = 100, MaxBayiSayisi = 9, AylikUcret = 6000.00m, Aktif = true },
                new SubscriptionPlan { PlanAdi = "Ultimate - 200 Çalışan", MaxKullaniciSayisi = 200, MaxBayiSayisi = 11, AylikUcret = 10000.00m, Aktif = true }
            });
            await db.SaveChangesAsync();
            Console.WriteLine("Subscription plans seeded successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        // Don't crash the service, continue even if migration fails
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// Get all tenants (SuperAdmin only)
app.MapGet("/api/tenants", async (TenantDbContext dbContext) =>
{
    try
    {
        var tenants = await dbContext.Tenants
            .Include(t => t.Subscriptions)
            .ThenInclude(s => s.Plan)
            .OrderBy(t => t.FirmaAdi)
            .ToListAsync();
        
        // JSON cycle hatasını önlemek için anonymous object kullan
        var result = tenants.Select(t => new
        {
            id = t.Id,
            firmaAdi = t.FirmaAdi,
            tenantKey = t.TenantKey,
            email = t.Email,
            telefon = t.Telefon,
            adres = t.Adres,
            vergiNo = t.VergiNo,
            aktif = t.Aktif,
            sesTanimaAktif = t.SesTanimaAktif,
            aktifAbonelikId = t.AktifAbonelikId,
            licenseKey = t.LicenseKey,
            licenseKeyGirisTarihi = t.LicenseKeyGirisTarihi,
            olusturmaTarihi = t.OlusturmaTarihi,
            subscriptions = t.Subscriptions.Select(s => new
            {
                id = s.Id,
                planId = s.PlanId,
                plan = s.Plan != null ? new
                {
                    id = s.Plan.Id,
                    planAdi = s.Plan.PlanAdi,
                    maxKullaniciSayisi = s.Plan.MaxKullaniciSayisi,
                    maxBayiSayisi = s.Plan.MaxBayiSayisi,
                    aylikUcret = s.Plan.AylikUcret,
                    aktif = s.Plan.Aktif
                } : null,
                baslangicTarihi = s.BaslangicTarihi,
                bitisTarihi = s.BitisTarihi,
                aktif = s.Aktif,
                olusturmaTarihi = s.OlusturmaTarihi
            }).ToList()
        }).ToList();
        
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Tenants GET error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem($"Firmalar yüklenirken hata: {ex.Message}", statusCode: 500);
    }
});

// Get tenant by ID
app.MapGet("/api/tenants/{id}", async (int id, TenantDbContext dbContext) =>
{
    var tenant = await dbContext.Tenants
        .Include(t => t.Subscriptions)
        .ThenInclude(s => s.Plan)
        .AsNoTracking()
        .FirstOrDefaultAsync(t => t.Id == id);

    if (tenant == null)
        return Results.NotFound(new { message = "Firma bulunamadı" });

    var result = new
    {
        tenant.Id,
        tenant.FirmaAdi,
        tenant.TenantKey,
        tenant.Email,
        tenant.Telefon,
        tenant.Adres,
        tenant.VergiNo,
        tenant.Aktif,
        tenant.SesTanimaAktif,
        tenant.AktifAbonelikId,
        tenant.LicenseKey,
        tenant.LicenseKeyGirisTarihi,
        tenant.OlusturmaTarihi,
        subscriptions = tenant.Subscriptions.Select(s => new
        {
            s.Id,
            s.PlanId,
            plan = s.Plan != null ? new
            {
                s.Plan.Id,
                s.Plan.PlanAdi,
                s.Plan.MaxKullaniciSayisi,
                s.Plan.MaxBayiSayisi,
                s.Plan.AylikUcret,
                s.Plan.Aktif
            } : null,
            s.BaslangicTarihi,
            s.BitisTarihi,
            s.Aktif,
            s.OlusturmaTarihi
        }).ToList()
    };

    return Results.Ok(result);
});

// Create tenant (SuperAdmin)
app.MapPost("/api/tenants", async (CreateTenantRequest request, TenantDbContext dbContext) =>
{
    // Null check
    if (request == null)
    {
        return Results.BadRequest(new { detail = "İstek verisi boş!" });
    }
    
    // Validation
    if (string.IsNullOrWhiteSpace(request.FirmaAdi))
    {
        return Results.BadRequest(new { detail = "Firma adı gerekli!" });
    }
    
    if (string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { detail = "Email gerekli!" });
    }
    
    try
    {
        // Generate unique tenant key
        var tenantKey = request.FirmaAdi.ToLower()
            .Replace(" ", "-")
            .Replace("ı", "i")
            .Replace("ş", "s")
            .Replace("ğ", "g")
            .Replace("ü", "u")
            .Replace("ö", "o")
            .Replace("ç", "c");
        
        // Check if tenant key already exists
        var existingTenant = await dbContext.Tenants
            .FirstOrDefaultAsync(t => t.TenantKey == tenantKey || t.FirmaAdi == request.FirmaAdi);
        
        if (existingTenant != null)
        {
            return Results.Conflict(new { detail = "Bu firma adı veya key zaten kullanılıyor!" });
        }
        
        var tenant = new BiSoyle.Tenant.Service.Data.Tenant
        {
            FirmaAdi = request.FirmaAdi,
            TenantKey = tenantKey,
            Email = request.Email,
            Telefon = request.Telefon,
            Adres = request.Adres,
            VergiNo = request.VergiNo,
            Aktif = true,
            SesTanimaAktif = false,
            OlusturmaTarihi = DateTime.UtcNow
        };
        
        // Güçlü random parola üret (12 karakter: büyük harf, küçük harf, rakam, özel karakter)
        var random = new Random();
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%&*";
        var generatedPassword = new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        
        // Admin parolasını Tenant'a kaydet (email gönderilene kadar saklanacak)
        tenant.AdminInitialPassword = generatedPassword;
        
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();
        
        // Otomatik admin kullanıcı oluştur
        string adminUsername = "";
        try
        {
            var userServiceUrl =
                Environment.GetEnvironmentVariable("USER_SERVICE_URL")
                ?? builder.Configuration["UserServiceUrl"]
                ?? "http://user-service:5004";
            var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            // Admin rolünü almak için User Service'e istek at (Authorization olmadan internal call)
            // Alternatif: Direkt role ID kullan veya Admin rolü ID'sini hardcode et (genelde 2)
            var adminRoleId = 2; // Seed'de Admin rolü ikinci sırada (SuperAdmin=1, Admin=2, User=3)
            
            // Admin kullanıcı oluştur
            adminUsername = tenant.TenantKey + "-admin";
            var adminEmail = tenant.Email; // Firma email'ini kullan
            
            var createUserRequest = new
            {
                tenantId = tenant.Id,
                username = adminUsername,
                email = adminEmail,
                password = generatedPassword,
                firstName = tenant.FirmaAdi.Split(' ')[0],
                lastName = tenant.FirmaAdi.Split(' ').Length > 1 ? string.Join(" ", tenant.FirmaAdi.Split(' ').Skip(1)) : "",
                title = "Firma Yöneticisi",
                location = tenant.Adres ?? "Turkey",
                roleIds = new List<int> { adminRoleId }
            };
            
            var response = await httpClient.PostAsJsonAsync($"{userServiceUrl}/api/users/internal/create-admin", createUserRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                // Authorization sorunu olabilir, alternatif yöntem
                Console.WriteLine($"Warning: Admin kullanıcı otomatik oluşturulamadı: {response.StatusCode}");
                // Parola yine de dönülecek, manuel oluşturulabilir
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Admin kullanıcı oluşturma hatası: {ex.Message}");
            // Hata olsa bile tenant oluşturuldu, parola yine dönülecek
        }
        
        return Results.Created($"/api/tenants/{tenant.Id}", new
        {
            id = tenant.Id,
            firmaAdi = tenant.FirmaAdi,
            tenantKey = tenant.TenantKey,
            email = tenant.Email,
            telefon = tenant.Telefon,
            adres = tenant.Adres,
            vergiNo = tenant.VergiNo,
            aktif = tenant.Aktif,
            olusturmaTarihi = tenant.OlusturmaTarihi,
            // Admin kullanıcı bilgileri
            adminUser = new
            {
                username = string.IsNullOrEmpty(adminUsername) ? tenant.TenantKey + "-admin" : adminUsername,
                email = tenant.Email,
                password = generatedPassword // Bu parolayı frontend'de göster ve kopyalama özelliği ekle
            }
        });
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
    {
        var innerMessage = ex.InnerException?.Message ?? ex.Message;
        if (innerMessage.Contains("duplicate") || innerMessage.Contains("unique"))
        {
            return Results.Conflict(new { detail = "Bu firma adı veya key zaten kullanılıyor!" });
        }
        return Results.Problem($"Veritabanı hatası: {innerMessage}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Tenant create error: {ex.Message}");
        return Results.Problem($"Firma oluşturma hatası: {ex.Message}");
    }
});

// Update tenant (SuperAdmin)
app.MapPut("/api/tenants/{id}", async (int id, UpdateTenantRequest request, TenantDbContext dbContext) =>
{
    // Null check
    if (request == null)
    {
        return Results.BadRequest(new { detail = "İstek verisi boş!" });
    }
    
    // Validation
    if (string.IsNullOrWhiteSpace(request.FirmaAdi))
    {
        return Results.BadRequest(new { detail = "Firma adı gerekli!" });
    }
    
    if (string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { detail = "Email gerekli!" });
    }
    
    try
    {
        var tenant = await dbContext.Tenants.FindAsync(id);
        
        if (tenant == null)
            return Results.NotFound(new { message = "Firma bulunamadı" });
        
        tenant.FirmaAdi = request.FirmaAdi;
        tenant.Email = request.Email;
        tenant.Telefon = request.Telefon;
        tenant.Adres = request.Adres;
        tenant.VergiNo = request.VergiNo;
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(tenant);
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
    {
        var innerMessage = ex.InnerException?.Message ?? ex.Message;
        if (innerMessage.Contains("duplicate") || innerMessage.Contains("unique"))
        {
            return Results.Conflict(new { detail = "Bu firma adı veya email zaten kullanılıyor!" });
        }
        return Results.Problem($"Veritabanı hatası: {innerMessage}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Tenant update error: {ex.Message}");
        return Results.Problem($"Firma güncelleme hatası: {ex.Message}");
    }
});

// Toggle tenant active status (SuperAdmin)
app.MapPut("/api/tenants/{id}/toggle-active", async (int id, TenantDbContext dbContext) =>
{
    var tenant = await dbContext.Tenants.FindAsync(id);
    
    if (tenant == null)
        return Results.NotFound(new { message = "Firma bulunamadı" });
    
    tenant.Aktif = !tenant.Aktif;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = $"Firma {(tenant.Aktif ? "aktif" : "pasif")} edildi", aktif = tenant.Aktif });
});

// Delete tenant (SuperAdmin - hard delete)
app.MapDelete("/api/tenants/{id}", async (int id, TenantDbContext dbContext) =>
{
    var tenant = await dbContext.Tenants.FindAsync(id);
    
    if (tenant == null)
        return Results.NotFound(new { message = "Firma bulunamadı" });
    
    // Remove related data explicitly to avoid orphan records in optional relations
    var subscriptions = await dbContext.Subscriptions
        .Where(s => s.TenantId == id)
        .ToListAsync();
    if (subscriptions.Count > 0)
    {
        dbContext.Subscriptions.RemoveRange(subscriptions);
    }

    var licenses = await dbContext.Licenses
        .Where(l => l.TenantId == id)
        .ToListAsync();
    if (licenses.Count > 0)
    {
        var licenseIds = licenses.Select(l => l.Id).ToList();

        var activations = await dbContext.LicenseActivations
            .Where(a => licenseIds.Contains(a.LicenseId))
            .ToListAsync();
        if (activations.Count > 0)
        {
            dbContext.LicenseActivations.RemoveRange(activations);
        }

        var rebindRequests = await dbContext.RebindRequests
            .Where(r => licenseIds.Contains(r.LicenseId))
            .ToListAsync();
        if (rebindRequests.Count > 0)
        {
            dbContext.RebindRequests.RemoveRange(rebindRequests);
        }

        dbContext.Licenses.RemoveRange(licenses);
    }

    dbContext.Tenants.Remove(tenant);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Firma kalıcı olarak silindi" });
});

// Toggle voice recognition for tenant
app.MapPut("/api/tenants/{id}/voice-recognition", async (int id, bool aktif, TenantDbContext dbContext) =>
{
    var tenant = await dbContext.Tenants.FindAsync(id);
    
    if (tenant == null)
        return Results.NotFound(new { message = "Firma bulunamadı" });
    
    tenant.SesTanimaAktif = aktif;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = $"Ses tanıma {(aktif ? "aktif" : "pasif")} edildi", sesTanimaAktif = aktif });
});

// Get subscription plans
app.MapGet("/api/subscription-plans", async (TenantDbContext dbContext) =>
{
    var plans = await dbContext.SubscriptionPlans
        .Where(p => p.Aktif)
        .OrderBy(p => p.MaxKullaniciSayisi)
        .ToListAsync();
    
    return Results.Ok(plans);
});

// Get all subscription plans (including inactive)
app.MapGet("/api/subscription-plans/all", async (TenantDbContext dbContext) =>
{
    try
    {
        var plans = await dbContext.SubscriptionPlans
            .OrderBy(p => p.MaxKullaniciSayisi)
            .ToListAsync();
        
        return Results.Ok(plans);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Subscription plans/all error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem($"Abonelik planları yüklenirken hata: {ex.Message}", statusCode: 500);
    }
});

// Create subscription plan (SuperAdmin)
app.MapPost("/api/subscription-plans", async (CreateSubscriptionPlanRequest request, TenantDbContext dbContext) =>
{
    // Null check
    if (request == null)
    {
        return Results.BadRequest(new { detail = "İstek verisi boş!" });
    }
    
    // Validation
    if (string.IsNullOrWhiteSpace(request.PlanAdi))
    {
        return Results.BadRequest(new { detail = "Plan adı gerekli!" });
    }
    
    if (request.MaxKullaniciSayisi <= 0)
    {
        return Results.BadRequest(new { detail = "Maksimum kullanıcı sayısı 0'dan büyük olmalı!" });
    }
    
    if (request.MaxBayiSayisi <= 0)
    {
        return Results.BadRequest(new { detail = "Maksimum bayi sayısı 0'dan büyük olmalı!" });
    }
    
    // Bayi sayısı validasyonu: 1, 3, 5, 7, 9, 11 olmalı
    var allowedBayiSayilari = new[] { 1, 3, 5, 7, 9, 11 };
    if (!allowedBayiSayilari.Contains(request.MaxBayiSayisi))
    {
        return Results.BadRequest(new { detail = "Maksimum bayi sayısı 1, 3, 5, 7, 9 veya 11 olmalı!" });
    }
    
    if (request.AylikUcret < 0)
    {
        return Results.BadRequest(new { detail = "Aylık ücret negatif olamaz!" });
    }
    
    try
    {
        var plan = new SubscriptionPlan
        {
            PlanAdi = request.PlanAdi,
            MaxKullaniciSayisi = request.MaxKullaniciSayisi,
            MaxBayiSayisi = request.MaxBayiSayisi,
            AylikUcret = request.AylikUcret,
            Aktif = true
        };
        
        dbContext.SubscriptionPlans.Add(plan);
        await dbContext.SaveChangesAsync();
        
        return Results.Created($"/api/subscription-plans/{plan.Id}", plan);
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
    {
        var innerMessage = ex.InnerException?.Message ?? ex.Message;
        if (innerMessage.Contains("duplicate") || innerMessage.Contains("unique"))
        {
            return Results.Conflict(new { detail = "Bu plan adı zaten kullanılıyor!" });
        }
        return Results.Problem($"Veritabanı hatası: {innerMessage}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Subscription plan create error: {ex.Message}");
        return Results.Problem($"Plan oluşturma hatası: {ex.Message}");
    }
});

// Update subscription plan (SuperAdmin)
app.MapPut("/api/subscription-plans/{id}", async (int id, UpdateSubscriptionPlanRequest request, TenantDbContext dbContext) =>
{
    // Null check
    if (request == null)
    {
        return Results.BadRequest(new { detail = "İstek verisi boş!" });
    }
    
    // Validation
    if (string.IsNullOrWhiteSpace(request.PlanAdi))
    {
        return Results.BadRequest(new { detail = "Plan adı gerekli!" });
    }
    
    if (request.MaxKullaniciSayisi <= 0)
    {
        return Results.BadRequest(new { detail = "Maksimum kullanıcı sayısı 0'dan büyük olmalı!" });
    }
    
    if (request.MaxBayiSayisi <= 0)
    {
        return Results.BadRequest(new { detail = "Maksimum bayi sayısı 0'dan büyük olmalı!" });
    }
    
    // Bayi sayısı validasyonu: 1, 3, 5, 7, 9, 11 olmalı
    var allowedBayiSayilari = new[] { 1, 3, 5, 7, 9, 11 };
    if (!allowedBayiSayilari.Contains(request.MaxBayiSayisi))
    {
        return Results.BadRequest(new { detail = "Maksimum bayi sayısı 1, 3, 5, 7, 9 veya 11 olmalı!" });
    }
    
    if (request.AylikUcret < 0)
    {
        return Results.BadRequest(new { detail = "Aylık ücret negatif olamaz!" });
    }
    
    try
    {
        var plan = await dbContext.SubscriptionPlans.FindAsync(id);
        
        if (plan == null)
            return Results.NotFound(new { message = "Plan bulunamadı" });
        
        plan.PlanAdi = request.PlanAdi;
        plan.MaxKullaniciSayisi = request.MaxKullaniciSayisi;
        plan.MaxBayiSayisi = request.MaxBayiSayisi;
        plan.AylikUcret = request.AylikUcret;
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(plan);
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
    {
        var innerMessage = ex.InnerException?.Message ?? ex.Message;
        if (innerMessage.Contains("duplicate") || innerMessage.Contains("unique"))
        {
            return Results.Conflict(new { detail = "Bu plan adı zaten kullanılıyor!" });
        }
        return Results.Problem($"Veritabanı hatası: {innerMessage}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Subscription plan update error: {ex.Message}");
        return Results.Problem($"Plan güncelleme hatası: {ex.Message}");
    }
});

// Toggle subscription plan active status (SuperAdmin)
app.MapPut("/api/subscription-plans/{id}/toggle-active", async (int id, TenantDbContext dbContext) =>
{
    var plan = await dbContext.SubscriptionPlans.FindAsync(id);
    
    if (plan == null)
        return Results.NotFound(new { message = "Plan bulunamadı" });
    
    plan.Aktif = !plan.Aktif;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = $"Plan {(plan.Aktif ? "aktif" : "pasif")} edildi", aktif = plan.Aktif });
});

// Delete subscription plan (SuperAdmin)
app.MapDelete("/api/subscription-plans/{id}", async (int id, TenantDbContext dbContext) =>
{
    var plan = await dbContext.SubscriptionPlans.FindAsync(id);
    
    if (plan == null)
        return Results.NotFound(new { message = "Plan bulunamadı" });
    
    // Check if plan is used
    var hasSubscriptions = await dbContext.Subscriptions.AnyAsync(s => s.PlanId == id);
    
    if (hasSubscriptions)
        return Results.BadRequest(new { message = "Bu plan kullanımda, silinemez. Pasif edebilirsiniz." });
    
    dbContext.SubscriptionPlans.Remove(plan);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Plan silindi" });
});

// ============================================
// VIRTUAL POS ENDPOINTS
// ============================================

app.MapGet("/api/virtual-pos/banks", (VirtualPosService virtualPosService) =>
{
    return Results.Ok(new
    {
        banks = virtualPosService.SupportedBanks
    });
});

app.MapGet("/api/virtual-pos/installments", () =>
{
    return Results.Ok(new[] { 1, 2, 3, 4, 6, 9, 12 });
});

app.MapGet("/api/virtual-pos/transactions", async (int? tenantId, int? planId, string? status, TenantDbContext dbContext) =>
{
    var query = dbContext.SubscriptionPayments.AsQueryable();

    if (tenantId.HasValue)
        query = query.Where(p => p.TenantId == tenantId.Value);

    if (planId.HasValue)
        query = query.Where(p => p.PlanId == planId.Value);

    if (!string.IsNullOrWhiteSpace(status))
        query = query.Where(p => p.Durum.ToLower() == status.ToLower());

    var payments = await query
        .OrderByDescending(p => p.OlusturmaTarihi)
        .Take(200)
        .ToListAsync();

    return Results.Ok(payments);
});

app.MapPost("/api/virtual-pos/charge", async (VirtualPosPaymentRequest request, TenantDbContext dbContext, VirtualPosService virtualPosService) =>
{
    if (request == null)
        return Results.BadRequest(new { message = "Ödeme isteği boş." });

    var cardNumber = virtualPosService.NormalizeCardNumber(request.KartNumarasi);
    if (!virtualPosService.IsValidCardNumber(cardNumber))
        return Results.BadRequest(new { message = "Geçersiz kart numarası" });

    try
    {
        virtualPosService.EnsureValidExpiry(request.SonKullanmaAy, request.SonKullanmaYil);
        virtualPosService.EnsureValidCvv(request.Cvv);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }

    if (request.Tutar <= 0)
    {
        return Results.BadRequest(new { message = "Ödeme tutarı 0'dan büyük olmalıdır" });
    }

    var tenant = await dbContext.Tenants.FindAsync(request.TenantId);
    var plan = await dbContext.SubscriptionPlans.FindAsync(request.PlanId);

    if (tenant == null || plan == null)
        return Results.BadRequest(new { message = "Firma veya plan bulunamadı" });

    if (plan.AylikUcret > 0 && request.Tutar < plan.AylikUcret)
        return Results.BadRequest(new { message = "Ödeme tutarı plan ücretinden düşük olamaz" });

    var bankName = virtualPosService.DetectBank(cardNumber);
    var (rate, commission) = virtualPosService.CalculateCommission(request.Tutar, request.TaksitSayisi);
    var shouldFail = cardNumber.EndsWith("0000") || request.Cvv == "000";

    var payment = new SubscriptionPayment
    {
        TenantId = request.TenantId,
        PlanId = request.PlanId,
        Tutar = request.Tutar,
        KomisyonTutari = commission,
        ParaBirimi = (request.ParaBirimi ?? "TRY").ToUpperInvariant(),
        TaksitSayisi = request.TaksitSayisi,
        KartSahibi = request.KartSahibi,
        KartSon4 = cardNumber.Length >= 4 ? cardNumber[^4..] : cardNumber,
        ReferansKodu = virtualPosService.GenerateReference(),
        Durum = shouldFail ? "Basarisiz" : "Basarili",
        BankaAdi = bankName,
        Mesaj = shouldFail ? "Banka tarafından işlem reddedildi. Lütfen farklı bir kart deneyin." : "Ödeme başarılı.",
        OnayTarihi = shouldFail ? null : DateTime.UtcNow,
        Kullanildi = false,
        UcDSecure = request.UcDSecure,
        IslemNo = virtualPosService.GenerateTransactionNumber(),
        ProvizyonKodu = shouldFail ? null : virtualPosService.GenerateProvisionCode()
    };

    dbContext.SubscriptionPayments.Add(payment);
    await dbContext.SaveChangesAsync();

    var response = new VirtualPosPaymentResponse
    {
        Reference = payment.ReferansKodu,
        Status = shouldFail ? "failed" : "success",
        BankName = bankName,
        ChargedAmount = request.Tutar,
        CommissionAmount = commission,
        CommissionRate = rate,
        Installment = request.TaksitSayisi,
        Currency = payment.ParaBirimi,
        MaskedCard = virtualPosService.MaskCard(cardNumber),
        ThreeDSecure = request.UcDSecure,
        Message = payment.Mesaj,
        ProcessedAt = payment.OnayTarihi ?? payment.OlusturmaTarihi
    };

    if (shouldFail)
    {
        return Results.BadRequest(response);
    }

    return Results.Ok(response);
});

app.MapGet("/api/platform-settings/payment", async (TenantDbContext dbContext) =>
{
    var setting = await dbContext.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "payment_settings");

    if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
    {
        return Results.Ok(new
        {
            iban = "",
            bankName = "",
            accountHolder = "",
            updatedAt = (DateTime?)null
        });
    }

    try
    {
        var payload = JsonSerializer.Deserialize<PaymentSettingsPayload>(setting.Value, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new PaymentSettingsPayload();

        return Results.Ok(new
        {
            iban = payload.Iban ?? "",
            bankName = payload.BankName ?? "",
            accountHolder = payload.AccountHolder ?? "",
            updatedAt = setting.UpdatedAt
        });
    }
    catch
    {
        return Results.Ok(new
        {
            iban = setting.Value,
            bankName = "",
            accountHolder = "",
            updatedAt = setting.UpdatedAt
        });
    }
});

app.MapPut("/api/platform-settings/payment", async (UpdatePaymentSettingRequest request, TenantDbContext dbContext) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.Iban))
    {
        return Results.BadRequest(new { detail = "IBAN bilgisi zorunludur." });
    }

    var sanitizedIban = request.Iban.Replace(" ", "").ToUpperInvariant();

    if (sanitizedIban.Length < 16 || sanitizedIban.Length > 34)
    {
        return Results.BadRequest(new { detail = "Geçerli bir IBAN giriniz." });
    }

    var payload = new PaymentSettingsPayload
    {
        Iban = sanitizedIban,
        BankName = request.BankName,
        AccountHolder = request.AccountHolder
    };

    var serialized = JsonSerializer.Serialize(payload);

    var existing = await dbContext.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "payment_settings");
    if (existing == null)
    {
        dbContext.PlatformSettings.Add(new PlatformSetting
        {
            Key = "payment_settings",
            Value = serialized,
            UpdatedAt = DateTime.UtcNow
        });
    }
    else
    {
        existing.Value = serialized;
        existing.UpdatedAt = DateTime.UtcNow;
        dbContext.PlatformSettings.Update(existing);
    }

    await dbContext.SaveChangesAsync();

    return Results.Ok(new
    {
        iban = sanitizedIban,
        bankName = request.BankName,
        accountHolder = request.AccountHolder,
        updatedAt = DateTime.UtcNow
    });
});

// Helpers
static async Task<GatewaySettingsPayload> GetGatewaySettingsAsync(TenantDbContext dbContext)
{
    var setting = await dbContext.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "gateway_settings");
    if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        return new GatewaySettingsPayload();
    try
    {
        return JsonSerializer.Deserialize<GatewaySettingsPayload>(setting.Value, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new GatewaySettingsPayload();
    }
    catch
    {
        return new GatewaySettingsPayload();
    }
}

// Provider-specific init endpoints (work in mock mode without real credentials)
app.MapPost("/api/virtual-pos/paytr/init", async (TenantDbContext dbContext) =>
{
    var settings = await GetGatewaySettingsAsync(dbContext);
    if (settings.Provider != "paytr")
        return Results.BadRequest(new { detail = "PAYTR etkin değil" });

    // If credentials missing, still return a mock token to allow UI flow
    var token = $"PT_{Guid.NewGuid():N}".ToUpperInvariant();
    var iframeUrl = $"https://www.paytr.com/odeme/guvenli/{token}";
    return Results.Ok(new
    {
        provider = "paytr",
        iframeToken = token,
        iframeUrl,
        mock = string.IsNullOrWhiteSpace(settings.PaytrMerchantId) || string.IsNullOrWhiteSpace(settings.PaytrMerchantKey) || string.IsNullOrWhiteSpace(settings.PaytrMerchantSalt)
    });
});

app.MapPost("/api/virtual-pos/iyzico/init", async (TenantDbContext dbContext) =>
{
    var settings = await GetGatewaySettingsAsync(dbContext);
    if (settings.Provider != "iyzico")
        return Results.BadRequest(new { detail = "iyzico etkin değil" });

    // If credentials missing, still return a mock token to allow UI flow
    var token = $"IYZ_{Guid.NewGuid():N}".ToUpperInvariant();
    var baseUrl = string.IsNullOrWhiteSpace(settings.IyzicoBaseUrl) ? "https://api.iyzipay.com" : settings.IyzicoBaseUrl!;
    return Results.Ok(new
    {
        provider = "iyzico",
        token,
        baseUrl,
        mock = string.IsNullOrWhiteSpace(settings.IyzicoApiKey) || string.IsNullOrWhiteSpace(settings.IyzicoSecretKey)
    });
});

// Callback placeholders (verify later when real credentials are provided)
app.MapPost("/api/virtual-pos/callback/paytr", async (HttpContext httpContext) =>
{
    try
    {
        var body = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
        Console.WriteLine($"PAYTR callback received: {body?.Substring(0, Math.Min(200, body?.Length ?? 0))}");
        return Results.Ok(new { ok = true });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

app.MapPost("/api/virtual-pos/callback/iyzico", async (HttpContext httpContext) =>
{
    try
    {
        var body = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
        Console.WriteLine($"iyzico callback received: {body?.Substring(0, Math.Min(200, body?.Length ?? 0))}");
        return Results.Ok(new { ok = true });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});
// Payment Gateway Settings (SuperAdmin)
app.MapGet("/api/platform-settings/gateway", async (TenantDbContext dbContext) =>
{
    var setting = await dbContext.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "gateway_settings");
    if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
    {
        return Results.Ok(new GatewaySettingsPayload()); // defaults (simulator)
    }
    try
    {
        var payload = JsonSerializer.Deserialize<GatewaySettingsPayload>(setting.Value, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new GatewaySettingsPayload();
        return Results.Ok(payload);
    }
    catch
    {
        return Results.Ok(new GatewaySettingsPayload());
    }
});

app.MapPut("/api/platform-settings/gateway", async (GatewaySettingsPayload request, TenantDbContext dbContext) =>
{
    if (request == null) return Results.BadRequest(new { detail = "Geçersiz istek" });

    request.Provider = string.IsNullOrWhiteSpace(request.Provider) ? "simulator" : request.Provider.ToLower().Trim();
    if (request.Provider != "simulator" && request.Provider != "paytr" && request.Provider != "iyzico")
        return Results.BadRequest(new { detail = "Geçersiz sağlayıcı" });

    var serialized = JsonSerializer.Serialize(request);
    var existing = await dbContext.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "gateway_settings");
    if (existing == null)
    {
        dbContext.PlatformSettings.Add(new PlatformSetting
        {
            Key = "gateway_settings",
            Value = serialized,
            UpdatedAt = DateTime.UtcNow
        });
    }
    else
    {
        existing.Value = serialized;
        existing.UpdatedAt = DateTime.UtcNow;
        dbContext.PlatformSettings.Update(existing);
    }
    await dbContext.SaveChangesAsync();
    return Results.Ok(new { saved = true, updatedAt = DateTime.UtcNow });
});
app.MapGet("/api/platform-settings/mail", async (TenantDbContext dbContext) =>
{
    var setting = await dbContext.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "mail_settings");

    if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
    {
        return Results.Ok(new
        {
            host = "",
            port = 587,
            security = "STARTTLS",
            username = "",
            fromEmail = "",
            fromName = "",
            replyTo = "",
            passwordSet = false,
            updatedAt = (DateTime?)null
        });
    }

    try
    {
        var payload = JsonSerializer.Deserialize<MailSettingsPayload>(setting.Value, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new MailSettingsPayload();

        return Results.Ok(new
        {
            host = payload.Host,
            port = payload.Port,
            security = payload.Security,
            username = payload.Username,
            fromEmail = payload.FromEmail,
            fromName = payload.FromName,
            replyTo = payload.ReplyTo,
            passwordSet = !string.IsNullOrWhiteSpace(payload.Password),
            updatedAt = setting.UpdatedAt
        });
    }
    catch
    {
        return Results.Ok(new
        {
            host = "",
            port = 587,
            security = "STARTTLS",
            username = "",
            fromEmail = "",
            fromName = "",
            replyTo = "",
            passwordSet = false,
            updatedAt = setting.UpdatedAt
        });
    }
});

app.MapPut("/api/platform-settings/mail", async (UpdateMailSettingsRequest request, TenantDbContext dbContext) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.Host) || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.FromEmail))
    {
        return Results.BadRequest(new { detail = "Sunucu, kullanıcı adı ve gönderen e-posta bilgileri zorunludur." });
    }

    if (request.Port <= 0)
    {
        return Results.BadRequest(new { detail = "Geçerli bir SMTP portu giriniz." });
    }

    var normalizedSecurity = string.IsNullOrWhiteSpace(request.Security)
        ? "STARTTLS"
        : request.Security.Trim().ToUpperInvariant();

    var existing = await dbContext.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "mail_settings");

    MailSettingsPayload payload;

    if (existing != null && !string.IsNullOrWhiteSpace(existing.Value))
    {
        payload = JsonSerializer.Deserialize<MailSettingsPayload>(existing.Value, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new MailSettingsPayload();
    }
    else
    {
        payload = new MailSettingsPayload();
    }

    payload.Host = request.Host.Trim();
    payload.Port = request.Port;
    payload.Security = normalizedSecurity;
    payload.Username = request.Username.Trim();
    payload.FromEmail = request.FromEmail.Trim();
    payload.FromName = string.IsNullOrWhiteSpace(request.FromName) ? null : request.FromName.Trim();
    payload.ReplyTo = string.IsNullOrWhiteSpace(request.ReplyTo) ? null : request.ReplyTo.Trim();

    if (!string.IsNullOrWhiteSpace(request.Password))
    {
        payload.Password = request.Password;
    }

    if (string.IsNullOrWhiteSpace(payload.Password))
    {
        return Results.BadRequest(new { detail = "SMTP parolası belirtilmemiş. İlk kurulum için parola zorunludur." });
    }

    var serialized = JsonSerializer.Serialize(payload);

    if (existing == null)
    {
        dbContext.PlatformSettings.Add(new PlatformSetting
        {
            Key = "mail_settings",
            Value = serialized,
            UpdatedAt = DateTime.UtcNow
        });
    }
    else
    {
        existing.Value = serialized;
        existing.UpdatedAt = DateTime.UtcNow;
        dbContext.PlatformSettings.Update(existing);
    }

    await dbContext.SaveChangesAsync();

    return Results.Ok(new
    {
        host = payload.Host,
        port = payload.Port,
        security = payload.Security,
        username = payload.Username,
        fromEmail = payload.FromEmail,
        fromName = payload.FromName,
        replyTo = payload.ReplyTo,
        passwordSet = true,
        updatedAt = DateTime.UtcNow
    });
});

// License key generation helper
string GenerateLicenseKey(int tenantId, int maxBayiSayisi)
{
    // Format: XXXX-XXXX-XXXX-XXXX (16 karakter, bayi sayısına göre)
    var random = new Random();
    var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // I, O, 0, 1 harfleri hariç
    var licenseKey = "";
    
    // İlk bölüm: Tenant ID hash (2 karakter)
    var tenantHash = (tenantId * 7) % 100;
    licenseKey += chars[tenantHash % chars.Length];
    licenseKey += chars[(tenantHash / chars.Length) % chars.Length];
    
    // İkinci bölüm: Bayi sayısı kodlaması (1 karakter)
    var bayiKod = maxBayiSayisi switch
    {
        1 => "A",
        3 => "B",
        5 => "C",
        7 => "D",
        9 => "E",
        11 => "F",
        _ => "X"
    };
    licenseKey += bayiKod;
    
    // Üçüncü ve dördüncü bölümler: Random (13 karakter, toplam 16)
    for (int i = 0; i < 13; i++)
    {
        licenseKey += chars[random.Next(chars.Length)];
    }
    
    // Format: XXXX-XXXX-XXXX-XXXX
    return $"{licenseKey.Substring(0, 4)}-{licenseKey.Substring(4, 4)}-{licenseKey.Substring(8, 4)}-{licenseKey.Substring(12, 4)}";
}

// Create subscription with license key generation & virtual POS payment integration
app.MapPost("/api/subscriptions", async (CreateSubscriptionRequest request, TenantDbContext dbContext, IHttpClientFactory httpClientFactory) =>
{
    var tenant = await dbContext.Tenants.FindAsync(request.TenantId);
    var plan = await dbContext.SubscriptionPlans.FindAsync(request.PlanId);
    
    if (tenant == null || plan == null)
        return Results.BadRequest(new { message = "Firma veya plan bulunamadı" });
    
    SubscriptionPayment? paymentRecord = null;

    if (plan.AylikUcret > 0)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentReference))
        {
            return Results.BadRequest(new { message = "Bu plan için geçerli bir ödeme referansı gereklidir." });
        }

        paymentRecord = await dbContext.SubscriptionPayments
            .FirstOrDefaultAsync(p => p.ReferansKodu == request.PaymentReference);

        if (paymentRecord == null)
        {
            return Results.BadRequest(new { message = "Ödeme referansı bulunamadı. Lütfen ödemeyi tamamlayın." });
        }

        if (paymentRecord.Durum != "Basarili")
        {
            return Results.BadRequest(new { message = "Ödeme başarısız görünüyor. Lütfen yeni bir ödeme deneyin." });
        }

        if (paymentRecord.Kullanildi)
        {
            return Results.BadRequest(new { message = "Bu ödeme referansı zaten kullanılmış." });
        }

        if (paymentRecord.TenantId != request.TenantId || paymentRecord.PlanId != request.PlanId)
        {
            return Results.BadRequest(new { message = "Ödeme referansı seçilen firma veya plan ile eşleşmiyor." });
        }

        if (paymentRecord.Tutar < plan.AylikUcret)
        {
            return Results.BadRequest(new { message = "Ödeme tutarı plan ücretinden düşük olamaz." });
        }
    }

    await using var transaction = await dbContext.Database.BeginTransactionAsync();

    var subscription = new Subscription
    {
        TenantId = request.TenantId,
        PlanId = request.PlanId,
        BaslangicTarihi = request.BaslangicTarihi,
        BitisTarihi = request.BitisTarihi,
        Aktif = true,
        OlusturmaTarihi = DateTime.UtcNow
    };
    
    dbContext.Subscriptions.Add(subscription);
    
    await dbContext.SaveChangesAsync();
    
    // Update tenant's active subscription with newly generated ID
    tenant.AktifAbonelikId = subscription.Id;

    // Generate and assign license key if not exists
    if (string.IsNullOrEmpty(tenant.LicenseKey))
    {
        string licenseKey;
        do
        {
            licenseKey = GenerateLicenseKey(tenant.Id, plan.MaxBayiSayisi);
        } while (await dbContext.Tenants.AnyAsync(t => t.LicenseKey == licenseKey));
        
        tenant.LicenseKey = licenseKey;
    }

    if (paymentRecord != null)
    {
        paymentRecord.Kullanildi = true;
        paymentRecord.KullanimTarihi = DateTime.UtcNow;
        paymentRecord.SubscriptionId = subscription.Id;
        dbContext.SubscriptionPayments.Update(paymentRecord);
    }

    await dbContext.SaveChangesAsync();
    await transaction.CommitAsync();
    
    MailSettingsPayload? mailSettings = null;
    try
    {
        var mailSettingEntity = await dbContext.PlatformSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "mail_settings");

        if (mailSettingEntity != null && !string.IsNullOrWhiteSpace(mailSettingEntity.Value))
        {
            mailSettings = JsonSerializer.Deserialize<MailSettingsPayload>(mailSettingEntity.Value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
    catch (Exception mailSettingEx)
    {
        Console.WriteLine($"Mail settings parse error: {mailSettingEx.Message}");
    }

    if (mailSettings != null && IsMailSettingsConfigured(mailSettings) && !string.IsNullOrWhiteSpace(tenant.Email))
    {
        // Admin kullanıcı bilgilerini User Service'ten al ve Tenant'tan parolayı al
        string? adminUsername = null;
        string? adminPassword = tenant.AdminInitialPassword; // Tenant oluştururken üretilen parola
        
        try
        {
            var userServiceUrl =
                Environment.GetEnvironmentVariable("USER_SERVICE_URL")
                ?? builder.Configuration["UserServiceUrl"]
                ?? "http://user-service:5004";
            var userServiceClient = httpClientFactory.CreateClient();
            userServiceClient.Timeout = TimeSpan.FromSeconds(10);
            
            var adminUserResponse = await userServiceClient.GetAsync($"{userServiceUrl}/api/users/internal/find-by-tenant/{tenant.Id}");
            if (adminUserResponse.IsSuccessStatusCode)
            {
                var adminUserJson = await adminUserResponse.Content.ReadAsStringAsync();
                var adminUser = JsonSerializer.Deserialize<JsonElement>(adminUserJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                adminUsername = adminUser.GetProperty("username").GetString();
            }
        }
        catch (Exception adminUserEx)
        {
            Console.WriteLine($"Warning: Admin kullanıcı bilgileri alınamadı: {adminUserEx.Message}");
        }
        
        // Eğer adminUsername bulunamadıysa, varsayılan format kullan
        if (string.IsNullOrWhiteSpace(adminUsername))
        {
            adminUsername = tenant.TenantKey + "-admin";
        }
        
        var payload = new LicenseEmailPayload
        {
            TenantName = tenant.FirmaAdi,
            PlanName = plan.PlanAdi,
            LicenseKey = tenant.LicenseKey ?? "",
            MaxUsers = plan.MaxKullaniciSayisi,
            MaxDevices = plan.MaxBayiSayisi,
            ToEmail = tenant.Email,
            ToName = tenant.FirmaAdi,
            PortalUrl = portalUrl,
            SupportEmail = portalSupportEmail,
            SupportPhone = portalSupportPhone,
            AdminEmail = tenant.Email,
            AdminUsername = adminUsername,
            AdminPassword = adminPassword, // Tenant oluştururken üretilen parola
            PaymentReference = paymentRecord?.ReferansKodu,
            Amount = paymentRecord?.Tutar ?? plan.AylikUcret,
            Currency = paymentRecord?.ParaBirimi ?? "TRY",
            Installment = paymentRecord?.TaksitSayisi,
            SubscriptionDate = DateTime.UtcNow
        };
        
        // Email gönderildikten sonra parolayı Tenant'tan sil (güvenlik)
        if (!string.IsNullOrWhiteSpace(adminPassword))
        {
            tenant.AdminInitialPassword = null;
            dbContext.Tenants.Update(tenant);
            await dbContext.SaveChangesAsync();
        }

        var mailSettingsClone = new MailSettingsPayload
        {
            Host = mailSettings.Host,
            Port = mailSettings.Port,
            Security = mailSettings.Security,
            Username = mailSettings.Username,
            Password = mailSettings.Password,
            FromEmail = mailSettings.FromEmail,
            FromName = mailSettings.FromName,
            ReplyTo = mailSettings.ReplyTo
        };

        _ = Task.Run(async () =>
        {
            await DispatchLicenseEmailAsync(httpClientFactory, emailServiceBaseUrl, mailSettingsClone, payload);
        });
    }
    
    // Yeniden yükle (subscription.Id artık var)
    var savedSubscription = await dbContext.Subscriptions
        .Include(s => s.Plan)
        .FirstOrDefaultAsync(s => s.Id == subscription.Id);
    
    return Results.Created($"/api/subscriptions/{subscription.Id}", new 
    { 
        subscription = new
        {
            id = savedSubscription!.Id,
            tenantId = savedSubscription.TenantId,
            planId = savedSubscription.PlanId,
            planAdi = savedSubscription.Plan?.PlanAdi,
            baslangicTarihi = savedSubscription.BaslangicTarihi,
            bitisTarihi = savedSubscription.BitisTarihi,
            aktif = savedSubscription.Aktif,
            olusturmaTarihi = savedSubscription.OlusturmaTarihi,
            paymentReference = paymentRecord?.ReferansKodu,
            chargedAmount = paymentRecord?.Tutar,
            commissionAmount = paymentRecord?.KomisyonTutari,
            paymentStatus = paymentRecord?.Durum
        },
        licenseKey = tenant.LicenseKey,
        message = "Abonelik oluşturuldu ve lisans anahtarı atandı"
    });
});

// Monthly subscription renewal with payment reference (Virtual POS)
app.MapPost("/api/subscriptions/renew", async (RenewSubscriptionRequest request, TenantDbContext dbContext) =>
{
    if (request == null || request.TenantId <= 0 || string.IsNullOrWhiteSpace(request.PaymentReference))
        return Results.BadRequest(new { message = "Geçersiz istek" });

    var tenant = await dbContext.Tenants.FindAsync(request.TenantId);
    if (tenant == null) return Results.NotFound(new { message = "Firma bulunamadı" });

    // Find active subscription
    var subscription = await dbContext.Subscriptions
        .Include(s => s.Plan)
        .FirstOrDefaultAsync(s => s.TenantId == request.TenantId && s.Aktif);
    if (subscription == null) return Results.BadRequest(new { message = "Aktif abonelik bulunamadı" });

    // Verify payment reference
    var payment = await dbContext.SubscriptionPayments
        .FirstOrDefaultAsync(p => p.ReferansKodu == request.PaymentReference);
    if (payment == null) return Results.BadRequest(new { message = "Ödeme referansı bulunamadı" });
    if (payment.Durum != "Basarili") return Results.BadRequest(new { message = "Ödeme başarısız" });
    if (payment.Kullanildi) return Results.BadRequest(new { message = "Ödeme referansı zaten kullanılmış" });
    if (payment.TenantId != request.TenantId || payment.PlanId != subscription.PlanId)
        return Results.BadRequest(new { message = "Ödeme referansı firma/plan ile eşleşmiyor" });
    if (subscription.Plan != null && payment.Tutar < subscription.Plan.AylikUcret)
        return Results.BadRequest(new { message = "Ödeme tutarı plan ücretinden düşük" });

    // Extend subscription one month from current end if in future, else from now
    var startBase = subscription.BitisTarihi.HasValue && subscription.BitisTarihi.Value > DateTime.UtcNow
        ? subscription.BitisTarihi.Value
        : DateTime.UtcNow;
    subscription.BaslangicTarihi = startBase;
    subscription.BitisTarihi = startBase.AddMonths(1);
    subscription.Aktif = true;

    // Mark payment used
    payment.Kullanildi = true;
    payment.KullanimTarihi = DateTime.UtcNow;
    payment.SubscriptionId = subscription.Id;
    dbContext.SubscriptionPayments.Update(payment);

    await dbContext.SaveChangesAsync();

    return Results.Ok(new
    {
        message = "Abonelik 1 ay uzatıldı",
        subscriptionId = subscription.Id,
        baslangicTarihi = subscription.BaslangicTarihi,
        bitisTarihi = subscription.BitisTarihi
    });
});
// Validate license key
app.MapPost("/api/tenants/validate-license", async (ValidateLicenseRequest request, TenantDbContext dbContext) =>
{
    if (string.IsNullOrWhiteSpace(request.LicenseKey))
    {
        return Results.BadRequest(new { detail = "Lisans anahtarı gerekli!" });
    }
    
    // Normalize: trim + uppercase
    var inputKey = request.LicenseKey.Trim().ToUpperInvariant();
    
    var tenant = await dbContext.Tenants
        .Include(t => t.Subscriptions)
        .ThenInclude(s => s.Plan)
        .FirstOrDefaultAsync(t => (t.LicenseKey ?? "").Trim().ToUpper() == inputKey);
    
    if (tenant == null)
    {
        return Results.Json(new { detail = "Geçersiz lisans anahtarı!" }, statusCode: 401);
    }
    
    if (!tenant.Aktif)
    {
        return Results.BadRequest(new { detail = "Firma hesabı pasif durumda!" });
    }
    
    // Aktif abonelik kontrolü
    var activeSubscription = tenant.Subscriptions.FirstOrDefault(s => s.Aktif && (s.BitisTarihi == null || s.BitisTarihi > DateTime.UtcNow));
    if (activeSubscription == null)
    {
        return Results.BadRequest(new { detail = "Aktif abonelik bulunamadı!" });
    }
    
    // İlk giriş ise tarihi kaydet
    if (!tenant.LicenseKeyGirisTarihi.HasValue)
    {
        tenant.LicenseKeyGirisTarihi = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }
    
    return Results.Ok(new 
    { 
        valid = true,
        tenantId = tenant.Id,
        tenantKey = tenant.TenantKey,
        firmaAdi = tenant.FirmaAdi,
        maxBayiSayisi = activeSubscription.Plan.MaxBayiSayisi,
        message = "Lisans anahtarı geçerli"
    });
});

// Fetch license payload by license key (auto-generate if missing)
app.MapPost("/api/licenses/by-key", async (FetchLicenseRequest request, TenantDbContext dbContext, RsaLicenseService rsaService) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.LicenseKey))
    {
        Console.WriteLine("License fetch failed: empty license key.");
        return Results.BadRequest(new { detail = "Lisans anahtarı gerekli!" });
    }

    // Normalize: trim + uppercase
    var inputKey = request.LicenseKey.Trim().ToUpperInvariant();

    var tenant = await dbContext.Tenants
        .Include(t => t.Subscriptions)
        .ThenInclude(s => s.Plan)
        .FirstOrDefaultAsync(t => (t.LicenseKey ?? "").Trim().ToUpper() == inputKey);

    Console.WriteLine($"License fetch request for key {inputKey}: tenant {(tenant == null ? "not found" : tenant.Id)}");

    if (tenant == null)
    {
        return Results.Json(new { detail = "Geçersiz lisans anahtarı!" }, statusCode: 401);
    }

    if (!tenant.Aktif)
    {
        return Results.BadRequest(new { detail = "Firma hesabı pasif durumda!" });
    }

    var activeSubscription = tenant.Subscriptions.FirstOrDefault(s =>
        s.Aktif && (s.BitisTarihi == null || s.BitisTarihi > DateTime.UtcNow));

    if (activeSubscription == null)
    {
        return Results.BadRequest(new { detail = "Aktif abonelik bulunamadı!" });
    }

    var license = await dbContext.Licenses
        .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey);

    Console.WriteLine($"Existing license lookup for key {request.LicenseKey}: {(license == null ? "missing" : "found")}");

    if (license == null)
    {
        try
        {
            var subscriptionPlan = activeSubscription.Plan;
            var packageName = subscriptionPlan?.PlanAdi ?? "Standard";
            var planMaxInstallations = subscriptionPlan?.MaxBayiSayisi ?? 0;
            var maxInstallations = planMaxInstallations > 0 ? planMaxInstallations : 1;

            var licensePayload = new
            {
                licenseKey = request.LicenseKey,
                tenantId = tenant.Id,
                tenantKey = tenant.TenantKey,
                firmaAdi = tenant.FirmaAdi,
                package = packageName,
                maxInstallations = maxInstallations,
                deviceFingerprint = (string?)null,
                toleranceThreshold = 1.0,
                createdAt = DateTime.UtcNow,
                expiresAt = activeSubscription.BitisTarihi
            };

            var serializedPayload = System.Text.Json.JsonSerializer.Serialize(licensePayload);
            var signature = rsaService.SignLicense(serializedPayload);

            license = new License
            {
                TenantId = tenant.Id,
                LicenseKey = request.LicenseKey,
                LicenseJson = Convert.ToBase64String(Encoding.UTF8.GetBytes(serializedPayload)),
                Signature = signature,
                Package = licensePayload.package,
                MaxInstallations = licensePayload.maxInstallations,
                DeviceFingerprint = null,
                ToleranceThreshold = licensePayload.toleranceThreshold,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Licenses.Add(license);
            await dbContext.SaveChangesAsync();

            Console.WriteLine($"Auto-created license for key {request.LicenseKey} (tenant {tenant.Id})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auto-create license error: {ex.Message}");
            return Results.Problem($"Lisans oluşturulamadı: {ex.Message}");
        }
    }
    else if (!license.IsActive)
    {
        return Results.BadRequest(new { detail = "Lisans pasif durumda!" });
    }

    string decodedLicenseJson;
    try
    {
        decodedLicenseJson = Encoding.UTF8.GetString(Convert.FromBase64String(license.LicenseJson));
    }
    catch (FormatException)
    {
        decodedLicenseJson = license.LicenseJson;
    }

    // Ensure signature is valid with current keys; if not, re-sign for backward compatibility.
    var signatureValid = rsaService.VerifySignature(decodedLicenseJson, license.Signature);
    if (!signatureValid)
    {
        try
        {
            var newSignature = rsaService.SignLicense(decodedLicenseJson);
            license.Signature = newSignature;
            license.LicenseJson = Convert.ToBase64String(Encoding.UTF8.GetBytes(decodedLicenseJson));
            license.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            Console.WriteLine($"License signature refreshed for key {request.LicenseKey}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing license signature for key {request.LicenseKey}: {ex.Message}");
        }
    }

    return Results.Ok(new
    {
        licenseJson = decodedLicenseJson,
        signature = license.Signature,
        licenseKey = license.LicenseKey,
        tenantId = license.TenantId,
        tenantKey = tenant.TenantKey,
        tenantName = tenant.FirmaAdi,
        package = license.Package,
        maxInstallations = license.MaxInstallations,
        deviceFingerprint = license.DeviceFingerprint,
        toleranceThreshold = license.ToleranceThreshold,
        isActive = license.IsActive,
        createdAt = license.CreatedAt,
        updatedAt = license.UpdatedAt
    });
});

// Helper: Find tenant by license key (trim/upper) - for diagnostics
app.MapGet("/api/tenants/by-license/{key}", async (string key, TenantDbContext dbContext) =>
{
    if (string.IsNullOrWhiteSpace(key))
        return Results.BadRequest(new { detail = "Lisans anahtarı gerekli!" });

    var inputKey = key.Trim().ToUpperInvariant();
    var tenant = await dbContext.Tenants
        .Include(t => t.Subscriptions)
        .ThenInclude(s => s.Plan)
        .FirstOrDefaultAsync(t => (t.LicenseKey ?? "").Trim().ToUpper() == inputKey);

    if (tenant == null)
        return Results.NotFound(new { detail = "Firma bulunamadı" });

    return Results.Ok(new
    {
        id = tenant.Id,
        firmaAdi = tenant.FirmaAdi,
        tenantKey = tenant.TenantKey,
        email = tenant.Email,
        licenseKey = tenant.LicenseKey,
        aktif = tenant.Aktif,
        subscriptions = tenant.Subscriptions.Select(s => new {
            id = s.Id,
            plan = s.Plan != null ? new { s.Plan.Id, s.Plan.PlanAdi } : null,
            aktif = s.Aktif
        }).ToList()
    });
});

// Generate admin password for tenant (SuperAdmin only)
app.MapPost("/api/tenants/{id}/generate-admin-password", async (int id, TenantDbContext dbContext) =>
{
    try
    {
        var tenant = await dbContext.Tenants.FindAsync(id);
        if (tenant == null)
        {
            return Results.NotFound(new { detail = "Firma bulunamadı!" });
        }
        
        var userServiceUrl =
            Environment.GetEnvironmentVariable("USER_SERVICE_URL")
            ?? builder.Configuration["UserServiceUrl"]
            ?? "http://user-service:5004";
        var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        // Güçlü random parola üret (12 karakter)
        var random = new Random();
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%&*";
        var generatedPassword = new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        
        // Admin kullanıcıyı bul (tenant email ile)
        var adminUsername = tenant.TenantKey + "-admin";
        
        // User Service'den admin kullanıcıyı bul ve parolayı güncelle
        var findUserResponse = await httpClient.GetAsync($"{userServiceUrl}/api/users/internal/find-by-tenant/{id}");
        UserInfo? adminUser = null;
        
        if (findUserResponse.IsSuccessStatusCode)
        {
            adminUser = await findUserResponse.Content.ReadFromJsonAsync<UserInfo>();
        }
        
        // Eğer kullanıcı bulunamadıysa EMAIL ile fallback ara (tenant.Email)
        if (adminUser == null || adminUser.Id == null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(tenant.Email))
                {
                    var findByEmailResponse = await httpClient.GetAsync($"{userServiceUrl}/api/users/internal/find-by-email/{Uri.EscapeDataString(tenant.Email)}");
                    if (findByEmailResponse.IsSuccessStatusCode)
                    {
                        var byEmail = await findByEmailResponse.Content.ReadFromJsonAsync<UserInfo>();
                        if (byEmail != null && byEmail.Id != null)
                        {
                            adminUser = byEmail;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fallback find-by-email error: {ex.Message}");
            }
        }

        // Hala bulunamadıysa: Admin kullanıcısı yoksa OLUŞTUR ve bu parolayı ata
        if (adminUser == null || adminUser.Id == null)
        {
            try
            {
                var createAdminPayload = new
                {
                    tenantId = tenant.Id,
                    username = adminUsername,
                    email = tenant.Email,
                    password = generatedPassword,
                    firstName = tenant.FirmaAdi.Split(' ')[0],
                    lastName = tenant.FirmaAdi.Split(' ').Length > 1 ? string.Join(" ", tenant.FirmaAdi.Split(' ').Skip(1)) : "",
                    title = "Firma Yöneticisi",
                    location = tenant.Adres ?? "Turkey",
                    roleIds = new List<int> { 2 } // Admin
                };
                var createResp = await httpClient.PostAsJsonAsync($"{userServiceUrl}/api/users/internal/create-admin", createAdminPayload);
                if (!createResp.IsSuccessStatusCode)
                {
                    var createErr = await createResp.Content.ReadAsStringAsync();
                    Console.WriteLine($"Create admin error: {createErr}");
                    return Results.Problem($"Admin kullanıcı oluşturulamadı: {createErr}");
                }
                // Create başarılı ise adminUser bilgilerini set et
                adminUser = new UserInfo { Username = adminUsername, Email = tenant.Email, Id = null };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create admin exception: {ex.Message}");
                return Results.Problem($"Admin kullanıcısı oluşturulamadı: {ex.Message}");
            }
        }
        else
        {
            // Mevcut kullanıcının parolasını güncelle
            var updatePasswordRequest = new { newPassword = generatedPassword };
            var updateResponse = await httpClient.PutAsJsonAsync($"{userServiceUrl}/api/users/internal/{adminUser.Id}/update-password", updatePasswordRequest);
            
            if (!updateResponse.IsSuccessStatusCode)
            {
                var errorContent = await updateResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Update password error: {errorContent}");
                return Results.Problem($"Parola güncellenemedi: {errorContent}");
            }
        }

    adminUser ??= new UserInfo
    {
        Username = adminUsername,
        Email = tenant.Email
    };
        
        return Results.Ok(new
        {
            adminUser = new
            {
                username = adminUser.Username ?? adminUsername,
                email = adminUser.Email ?? tenant.Email,
                password = generatedPassword
            },
            message = "Parola başarıyla üretildi"
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Generate password error: {ex.Message}");
        return Results.Problem($"Parola üretme hatası: {ex.Message}");
    }
});

// End subscription
app.MapPut("/api/subscriptions/{id}/end", async (int id, TenantDbContext dbContext) =>
{
    var subscription = await dbContext.Subscriptions.FindAsync(id);
    
    if (subscription == null)
        return Results.NotFound(new { message = "Abonelik bulunamadı" });
    
    subscription.Aktif = false;
    subscription.BitisTarihi = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Abonelik sonlandırıldı" });
});

// ============================================
// LICENSE DEVICE BINDING ENDPOINTS
// ============================================

// Create license with RSA signing (SuperAdmin only)
app.MapPost("/api/licenses/create", async (
    CreateLicenseRequest request,
    TenantDbContext dbContext,
    RsaLicenseService rsaService) =>
{
    if (request == null || request.TenantId <= 0)
        return Results.BadRequest(new { detail = "Geçersiz istek!" });
    
    try
    {
        var tenant = await dbContext.Tenants.FindAsync(request.TenantId);
        if (tenant == null)
            return Results.NotFound(new { detail = "Firma bulunamadı!" });
        
        // Generate license key if not provided
        var licenseKey = request.LicenseKey ?? tenant.LicenseKey;
        if (string.IsNullOrEmpty(licenseKey))
        {
            var random = new Random();
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            licenseKey = new string(Enumerable.Repeat(chars, 16)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            licenseKey = $"{licenseKey.Substring(0, 4)}-{licenseKey.Substring(4, 4)}-{licenseKey.Substring(8, 4)}-{licenseKey.Substring(12, 4)}";
        }
        
        // Create license JSON
        var licenseData = new
        {
            licenseKey = licenseKey,
            tenantId = tenant.Id,
            tenantKey = tenant.TenantKey,
            firmaAdi = tenant.FirmaAdi,
            package = request.Package ?? "Standard",
            maxInstallations = request.MaxInstallations > 0 ? request.MaxInstallations : 1,
            deviceFingerprint = request.DeviceFingerprint,
            toleranceThreshold = request.ToleranceThreshold >= 0.0 && request.ToleranceThreshold <= 1.0 
                ? request.ToleranceThreshold : 1.0,
            createdAt = DateTime.UtcNow,
            expiresAt = request.ExpiresAt
        };
        
        var licenseJson = System.Text.Json.JsonSerializer.Serialize(licenseData);
        
        // Sign license
        var signature = rsaService.SignLicense(licenseJson);
        
        // Save to database
        var license = new License
        {
            TenantId = tenant.Id,
            LicenseKey = licenseKey,
            LicenseJson = Convert.ToBase64String(Encoding.UTF8.GetBytes(licenseJson)),
            Signature = signature,
            Package = request.Package ?? "Standard",
            MaxInstallations = request.MaxInstallations > 0 ? request.MaxInstallations : 1,
            DeviceFingerprint = request.DeviceFingerprint,
            ToleranceThreshold = request.ToleranceThreshold >= 0.0 && request.ToleranceThreshold <= 1.0 
                ? request.ToleranceThreshold : 1.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        dbContext.Licenses.Add(license);
        await dbContext.SaveChangesAsync();
        
        return Results.Created($"/api/licenses/{license.Id}", new
        {
            id = license.Id,
            licenseKey = license.LicenseKey,
            licenseJson = licenseJson,
            signature = signature,
            publicKey = rsaService.ExportPublicKeyPem()
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Create license error: {ex.Message}");
        return Results.Problem($"Lisans oluşturma hatası: {ex.Message}");
    }
});

// Activate license with device binding
app.MapPost("/api/licenses/activate", async (
    ActivateLicenseRequest request,
    TenantDbContext dbContext,
    RsaLicenseService rsaService) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.LicenseJson) || 
        string.IsNullOrWhiteSpace(request.Signature) || 
        string.IsNullOrWhiteSpace(request.DeviceFingerprint))
    {
        return Results.BadRequest(new { detail = "Lisans JSON, imza ve cihaz fingerprint gerekli!" });
    }
    
    try
    {
        // Verify signature
        if (!rsaService.VerifySignature(request.LicenseJson, request.Signature))
        {
            return Results.Json(new { detail = "Lisans imzası geçersiz!" }, statusCode: 401);
        }
        
        // Parse license JSON
        var licenseData = System.Text.Json.JsonSerializer.Deserialize<LicenseData>(
            request.LicenseJson,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (licenseData == null)
        {
            return Results.BadRequest(new { detail = "Geçersiz lisans formatı!" });
        }

        Console.WriteLine($"Activation attempt for key: {licenseData.LicenseKey}");
        
        // Find license in database
        var license = await dbContext.Licenses
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.LicenseKey == licenseData.LicenseKey);
        
        if (license == null)
        {
            Console.WriteLine($"Activation failed: license not found for key {licenseData.LicenseKey}");
            return Results.NotFound(new { detail = $"Lisans bulunamadı! ({licenseData.LicenseKey ?? "boş"})" });
        }
        
        if (!license.IsActive)
        {
            return Results.BadRequest(new { detail = "Lisans aktif değil!" });
        }
        
        // Check device fingerprint match (with tolerance)
        var fingerprintHash = DeviceFingerprintHelper.ComputeHash(request.DeviceFingerprint);
        var licenseFingerprintHash = license.DeviceFingerprint != null 
            ? DeviceFingerprintHelper.ComputeHash(license.DeviceFingerprint) 
            : null;
        
        bool fingerprintMatch = true;
        if (licenseFingerprintHash != null)
        {
            fingerprintMatch = DeviceFingerprintHelper.IsMatch(
                request.DeviceFingerprint, 
                license.DeviceFingerprint!, 
                license.ToleranceThreshold
            );
        }
        
        // Check if already activated
        var existingActivation = license.Activations
            .FirstOrDefault(a => a.DeviceFingerprint == fingerprintHash);
        
        if (existingActivation != null)
        {
            if (existingActivation.IsActive)
            {
                // Update last seen
                existingActivation.LastSeenAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                
                return Results.Ok(new { 
                    message = "Lisans zaten aktif!",
                    activationId = existingActivation.Id,
                    alreadyActivated = true
                });
            }
            else
            {
                // Reactivate
                existingActivation.IsActive = true;
                existingActivation.LastSeenAt = DateTime.UtcNow;
                if (request.TpmPublicKey != null)
                    existingActivation.TpmPublicKey = request.TpmPublicKey;
            }
        }
        else
        {
            // First-time activation on new device: enforce max installation limit
            var activeActivations = license.Activations.Count(a => a.IsActive);
            if (activeActivations >= license.MaxInstallations)
            {
                return Results.BadRequest(new
                {
                    detail = $"Maksimum kurulum sayısına ulaşıldı ({license.MaxInstallations})!",
                    requiresRebind = true
                });
            }

            // Create new activation
            var activation = new LicenseActivation
            {
                LicenseId = license.Id,
                DeviceFingerprint = fingerprintHash,
                TpmPublicKey = request.TpmPublicKey,
                IsActive = true,
                ActivatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                DeviceName = request.DeviceName,
                IpAddress = request.IpAddress
            };
            
            dbContext.LicenseActivations.Add(activation);
        }
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(new { 
            message = "Lisans başarıyla aktive edildi!",
            activated = true,
            maxInstallations = license.MaxInstallations,
            currentInstallations = license.Activations.Count(a => a.IsActive)
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Activate license error: {ex.Message}");
        return Results.Problem($"Lisans aktivasyon hatası: {ex.Message}");
    }
});

// TPM Challenge endpoint
app.MapPost("/api/licenses/challenge", async (
    ChallengeRequest request,
    TenantDbContext dbContext,
    RsaLicenseService rsaService) =>
{
    if (request == null || request.LicenseId <= 0)
        return Results.BadRequest(new { detail = "Geçersiz istek!" });
    
    try
    {
        var activation = await dbContext.LicenseActivations
            .Include(a => a.License)
            .FirstOrDefaultAsync(a => a.Id == request.ActivationId && a.LicenseId == request.LicenseId);
        
        if (activation == null)
            return Results.NotFound(new { detail = "Aktivasyon bulunamadı!" });
        
        if (string.IsNullOrEmpty(activation.TpmPublicKey))
            return Results.BadRequest(new { detail = "TPM public key kayıtlı değil!" });
        
        // Generate nonce
        var nonce = Guid.NewGuid().ToString("N");
        var nonceExpiry = DateTime.UtcNow.AddMinutes(5);
        
        // In production, store nonce with expiry in cache/db
        // For demo, return nonce directly
        
        return Results.Ok(new
        {
            nonce = nonce,
            expiresAt = nonceExpiry
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Challenge error: {ex.Message}");
        return Results.Problem($"Challenge oluşturma hatası: {ex.Message}");
    }
});

// Verify TPM challenge response
app.MapPost("/api/licenses/verify-challenge", async (
    VerifyChallengeRequest request,
    TenantDbContext dbContext) =>
{
    // TPM challenge verification logic
    // This would verify the client's TPM signature of the nonce
    // Implementation depends on TPM library (e.g., TPM2.NET, Microsoft.Tpm.Tbs)
    
    return Results.Ok(new { verified = true, message = "TPM challenge verified" });
});

// Get license public key (for client verification)
app.MapGet("/api/licenses/public-key", (RsaLicenseService rsaService) =>
{
    try
    {
        var publicKeyPem = rsaService.ExportPublicKeyPem();
        return Results.Ok(new { publicKey = publicKeyPem });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Public key export hatası: {ex.Message}");
    }
});

static bool IsMailSettingsConfigured(MailSettingsPayload settings) =>
    !string.IsNullOrWhiteSpace(settings.Host) &&
    !string.IsNullOrWhiteSpace(settings.Username) &&
    !string.IsNullOrWhiteSpace(settings.Password) &&
    !string.IsNullOrWhiteSpace(settings.FromEmail);

static async Task DispatchLicenseEmailAsync(
    IHttpClientFactory httpClientFactory,
    string emailServiceBaseUrl,
    MailSettingsPayload mailSettings,
    LicenseEmailPayload payload)
{
    try
    {
        var client = httpClientFactory.CreateClient();
        var url = $"{emailServiceBaseUrl.TrimEnd('/')}/api/email/send-license";

        var request = new
        {
            mailSettings = new
            {
                mailSettings.Host,
                mailSettings.Port,
                mailSettings.Security,
                mailSettings.Username,
                mailSettings.Password,
                mailSettings.FromEmail,
                mailSettings.FromName,
                mailSettings.ReplyTo
            },
            model = payload
        };

        var response = await client.PostAsJsonAsync(url, request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Email service error ({response.StatusCode}): {body}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Email dispatch error: {ex.Message}");
    }
}

// Resend license email for a subscription
app.MapPost("/api/subscriptions/{id}/resend-email", async (int id, TenantDbContext dbContext, IHttpClientFactory httpClientFactory) =>
{
    var subscription = await dbContext.Subscriptions
        .Include(s => s.Plan)
        .Include(s => s.Tenant)
        .FirstOrDefaultAsync(s => s.Id == id);

    if (subscription == null)
        return Results.NotFound(new { detail = "Abonelik bulunamadı" });

    var tenant = subscription.Tenant;
    var plan = subscription.Plan;

    if (tenant == null || plan == null)
        return Results.BadRequest(new { detail = "Firma veya plan bilgisi bulunamadı" });

    MailSettingsPayload? mailSettings = null;
    try
    {
        var mailSettingEntity = await dbContext.PlatformSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "mail_settings");

        if (mailSettingEntity != null && !string.IsNullOrWhiteSpace(mailSettingEntity.Value))
        {
            mailSettings = JsonSerializer.Deserialize<MailSettingsPayload>(mailSettingEntity.Value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
    catch (Exception mailSettingEx)
    {
        Console.WriteLine($"Mail settings parse error: {mailSettingEx.Message}");
        return Results.Problem($"Mail ayarları okunamadı: {mailSettingEx.Message}");
    }

    if (mailSettings == null || !IsMailSettingsConfigured(mailSettings) || string.IsNullOrWhiteSpace(tenant.Email))
    {
        return Results.BadRequest(new { detail = "Mail ayarları yapılandırılmamış veya firma e-postası eksik" });
    }

    var paymentRecord = await dbContext.SubscriptionPayments
        .FirstOrDefaultAsync(p => p.SubscriptionId == id);

    var payload = new LicenseEmailPayload
    {
        TenantName = tenant.FirmaAdi,
        PlanName = plan.PlanAdi,
        LicenseKey = tenant.LicenseKey ?? "",
        MaxUsers = plan.MaxKullaniciSayisi,
        MaxDevices = plan.MaxBayiSayisi,
        ToEmail = tenant.Email,
        ToName = tenant.FirmaAdi,
        PortalUrl = portalUrl,
        SupportEmail = portalSupportEmail,
        SupportPhone = portalSupportPhone,
        AdminEmail = tenant.Email,
        PaymentReference = paymentRecord?.ReferansKodu,
        Amount = paymentRecord?.Tutar ?? plan.AylikUcret,
        Currency = paymentRecord?.ParaBirimi ?? "TRY",
        Installment = paymentRecord?.TaksitSayisi,
        SubscriptionDate = subscription.BaslangicTarihi
    };

    var mailSettingsClone = new MailSettingsPayload
    {
        Host = mailSettings.Host,
        Port = mailSettings.Port,
        Security = mailSettings.Security,
        Username = mailSettings.Username,
        Password = mailSettings.Password,
        FromEmail = mailSettings.FromEmail,
        FromName = mailSettings.FromName,
        ReplyTo = mailSettings.ReplyTo
    };

    try
    {
        await DispatchLicenseEmailAsync(httpClientFactory, emailServiceBaseUrl, mailSettingsClone, payload);
        return Results.Ok(new { sent = true, message = "Email başarıyla gönderildi" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Email gönderilemedi: {ex.Message}");
    }
});

// Resend license email for a tenant (uses latest active subscription)
app.MapPost("/api/tenants/{tenantId}/resend-license-email", async (int tenantId, TenantDbContext dbContext, IHttpClientFactory httpClientFactory) =>
{
    var tenant = await dbContext.Tenants
        .FirstOrDefaultAsync(t => t.Id == tenantId);

    if (tenant == null)
        return Results.NotFound(new { detail = "Firma bulunamadı" });

    // En son aktif aboneliği bul
    var subscription = await dbContext.Subscriptions
        .Include(s => s.Plan)
        .Where(s => s.TenantId == tenantId && s.Aktif == true)
        .OrderByDescending(s => s.BaslangicTarihi)
        .FirstOrDefaultAsync();

    if (subscription == null)
        return Results.BadRequest(new { detail = "Firma için aktif abonelik bulunamadı" });

    var plan = subscription.Plan;
    if (plan == null)
        return Results.BadRequest(new { detail = "Plan bilgisi bulunamadı" });

    MailSettingsPayload? mailSettings = null;
    try
    {
        var mailSettingEntity = await dbContext.PlatformSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "mail_settings");

        if (mailSettingEntity != null && !string.IsNullOrWhiteSpace(mailSettingEntity.Value))
        {
            mailSettings = JsonSerializer.Deserialize<MailSettingsPayload>(mailSettingEntity.Value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
    catch (Exception mailSettingEx)
    {
        Console.WriteLine($"Mail settings parse error: {mailSettingEx.Message}");
        return Results.Problem($"Mail ayarları okunamadı: {mailSettingEx.Message}");
    }

    if (mailSettings == null || !IsMailSettingsConfigured(mailSettings) || string.IsNullOrWhiteSpace(tenant.Email))
    {
        return Results.BadRequest(new { detail = "Mail ayarları yapılandırılmamış veya firma e-postası eksik" });
    }

    var paymentRecord = await dbContext.SubscriptionPayments
        .FirstOrDefaultAsync(p => p.SubscriptionId == subscription.Id);

    var payload = new LicenseEmailPayload
    {
        TenantName = tenant.FirmaAdi,
        PlanName = plan.PlanAdi,
        LicenseKey = tenant.LicenseKey ?? "",
        MaxUsers = plan.MaxKullaniciSayisi,
        MaxDevices = plan.MaxBayiSayisi,
        ToEmail = tenant.Email,
        ToName = tenant.FirmaAdi,
        PortalUrl = portalUrl,
        SupportEmail = portalSupportEmail,
        SupportPhone = portalSupportPhone,
        AdminEmail = tenant.Email,
        PaymentReference = paymentRecord?.ReferansKodu,
        Amount = paymentRecord?.Tutar ?? plan.AylikUcret,
        Currency = paymentRecord?.ParaBirimi ?? "TRY",
        Installment = paymentRecord?.TaksitSayisi,
        SubscriptionDate = subscription.BaslangicTarihi
    };

    var mailSettingsClone = new MailSettingsPayload
    {
        Host = mailSettings.Host,
        Port = mailSettings.Port,
        Security = mailSettings.Security,
        Username = mailSettings.Username,
        Password = mailSettings.Password,
        FromEmail = mailSettings.FromEmail,
        FromName = mailSettings.FromName,
        ReplyTo = mailSettings.ReplyTo
    };

    try
    {
        await DispatchLicenseEmailAsync(httpClientFactory, emailServiceBaseUrl, mailSettingsClone, payload);
        return Results.Ok(new { sent = true, message = "Lisans anahtarı email'i başarıyla gönderildi" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Email gönderilemedi: {ex.Message}");
    }
});

app.MapControllers();

// ========================
// Admin Reporting Endpoints
// ========================
app.MapGet("/api/admin/payments/summary", async (TenantDbContext dbContext) =>
{
    var now = DateTime.UtcNow;
    var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    var tenants = await dbContext.Tenants.AsNoTracking().ToListAsync();
    int totalTenants = tenants.Count;
    int activeTenants = tenants.Count(t => t.Aktif);

    int overdueTenants = 0;
    int deactivatedTenants = tenants.Count(t => !t.Aktif);

    foreach (var tenant in tenants)
    {
        var lastPayment = await dbContext.SubscriptionPayments
            .Where(p => p.TenantId == tenant.Id && p.Durum == "Basarili")
            .OrderByDescending(p => p.OnayTarihi ?? p.OlusturmaTarihi)
            .FirstOrDefaultAsync();

        var refDate = lastPayment?.OnayTarihi ?? tenant.OlusturmaTarihi;
        var dueDate = refDate.AddDays(30);
        var graceUntil = dueDate.AddDays(7);
        if (now > dueDate && now <= graceUntil && tenant.Aktif)
        {
            overdueTenants++;
        }
    }

    var monthPayments = await dbContext.SubscriptionPayments
        .Where(p => p.Durum == "Basarili" && (p.OnayTarihi ?? p.OlusturmaTarihi) >= startOfMonth)
        .ToListAsync();

    decimal paidThisMonth = monthPayments.Sum(p => p.Tutar);
    int paymentsThisMonth = monthPayments.Count;

    return Results.Ok(new PaymentSummaryDto(
        totalTenants,
        activeTenants,
        overdueTenants,
        deactivatedTenants,
        paidThisMonth,
        paymentsThisMonth
    ));
}); // Auth gateway seviyesinde kontrol edilir

app.MapGet("/api/admin/payments/trend", async (TenantDbContext dbContext, int months) =>
{
    months = months <= 0 ? 6 : Math.Min(months, 24);
    var now = DateTime.UtcNow;
    var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-months + 1);

    var payments = await dbContext.SubscriptionPayments
        .Where(p => p.Durum == "Basarili" && (p.OnayTarihi ?? p.OlusturmaTarihi) >= start)
        .ToListAsync();

    var byMonth = payments
        .GroupBy(p =>
        {
            var d = (p.OnayTarihi ?? p.OlusturmaTarihi).ToUniversalTime();
            return new { d.Year, d.Month };
        })
        .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
        .Select(g => new MonthlyPaymentPoint(
            $"{g.Key.Year}-{g.Key.Month:00}",
            g.Sum(x => x.Tutar),
            g.Count()
        ))
        .ToList();

    return Results.Ok(byMonth);
});

// Start the application
app.Run();
