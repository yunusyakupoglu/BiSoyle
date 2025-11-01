using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BiSoyle.Tenant.Service.Data;

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

// Database
builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-migrate and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    try
    {
        db.Database.Migrate();
        
        // Seed subscription plans
        if (!db.SubscriptionPlans.Any())
        {
            db.SubscriptionPlans.AddRange(new[]
            {
                new SubscriptionPlan { PlanAdi = "Starter - 5 Çalışan", MaxKullaniciSayisi = 5, AylikUcret = 500.00m, Aktif = true },
                new SubscriptionPlan { PlanAdi = "Basic - 10 Çalışan", MaxKullaniciSayisi = 10, AylikUcret = 900.00m, Aktif = true },
                new SubscriptionPlan { PlanAdi = "Professional - 20 Çalışan", MaxKullaniciSayisi = 20, AylikUcret = 1600.00m, Aktif = true },
                new SubscriptionPlan { PlanAdi = "Enterprise - 50 Çalışan", MaxKullaniciSayisi = 50, AylikUcret = 3500.00m, Aktif = true }
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

// Get all tenants (SuperAdmin only)
app.MapGet("/api/tenants", async (TenantDbContext dbContext) =>
{
    var tenants = await dbContext.Tenants
        .Include(t => t.Subscriptions)
        .ThenInclude(s => s.Plan)
        .OrderBy(t => t.FirmaAdi)
        .ToListAsync();
    
    return Results.Ok(tenants);
});

// Get tenant by ID
app.MapGet("/api/tenants/{id}", async (int id, TenantDbContext dbContext) =>
{
    var tenant = await dbContext.Tenants
        .Include(t => t.Subscriptions)
        .ThenInclude(s => s.Plan)
        .FirstOrDefaultAsync(t => t.Id == id);
    
    if (tenant == null)
        return Results.NotFound(new { message = "Firma bulunamadı" });
    
    return Results.Ok(tenant);
});

// Create tenant (SuperAdmin)
app.MapPost("/api/tenants", async (CreateTenantRequest request, TenantDbContext dbContext) =>
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
    
    dbContext.Tenants.Add(tenant);
    await dbContext.SaveChangesAsync();
    
    return Results.Created($"/api/tenants/{tenant.Id}", tenant);
});

// Update tenant (SuperAdmin)
app.MapPut("/api/tenants/{id}", async (int id, UpdateTenantRequest request, TenantDbContext dbContext) =>
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

// Delete tenant (SuperAdmin - soft delete)
app.MapDelete("/api/tenants/{id}", async (int id, TenantDbContext dbContext) =>
{
    var tenant = await dbContext.Tenants.FindAsync(id);
    
    if (tenant == null)
        return Results.NotFound(new { message = "Firma bulunamadı" });
    
    // Soft delete - just deactivate
    tenant.Aktif = false;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Firma silindi (deaktif edildi)" });
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
    var plans = await dbContext.SubscriptionPlans
        .OrderBy(p => p.MaxKullaniciSayisi)
        .ToListAsync();
    
    return Results.Ok(plans);
});

// Create subscription plan (SuperAdmin)
app.MapPost("/api/subscription-plans", async (CreateSubscriptionPlanRequest request, TenantDbContext dbContext) =>
{
    var plan = new SubscriptionPlan
    {
        PlanAdi = request.PlanAdi,
        MaxKullaniciSayisi = request.MaxKullaniciSayisi,
        AylikUcret = request.AylikUcret,
        Aktif = true
    };
    
    dbContext.SubscriptionPlans.Add(plan);
    await dbContext.SaveChangesAsync();
    
    return Results.Created($"/api/subscription-plans/{plan.Id}", plan);
});

// Update subscription plan (SuperAdmin)
app.MapPut("/api/subscription-plans/{id}", async (int id, UpdateSubscriptionPlanRequest request, TenantDbContext dbContext) =>
{
    var plan = await dbContext.SubscriptionPlans.FindAsync(id);
    
    if (plan == null)
        return Results.NotFound(new { message = "Plan bulunamadı" });
    
    plan.PlanAdi = request.PlanAdi;
    plan.MaxKullaniciSayisi = request.MaxKullaniciSayisi;
    plan.AylikUcret = request.AylikUcret;
    
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(plan);
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

// Create subscription
app.MapPost("/api/subscriptions", async (CreateSubscriptionRequest request, TenantDbContext dbContext) =>
{
    var tenant = await dbContext.Tenants.FindAsync(request.TenantId);
    var plan = await dbContext.SubscriptionPlans.FindAsync(request.PlanId);
    
    if (tenant == null || plan == null)
        return Results.BadRequest(new { message = "Firma veya plan bulunamadı" });
    
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
    
    // Update tenant's active subscription
    tenant.AktifAbonelikId = subscription.Id;
    
    await dbContext.SaveChangesAsync();
    
    return Results.Created($"/api/subscriptions/{subscription.Id}", subscription);
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

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5005";
var url = $"http://0.0.0.0:{port}";
app.Run(url);

// Request Models
public class CreateTenantRequest
{
    public string FirmaAdi { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Telefon { get; set; }
    public string? Adres { get; set; }
    public string? VergiNo { get; set; }
}

public class UpdateTenantRequest
{
    public string FirmaAdi { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Telefon { get; set; }
    public string? Adres { get; set; }
    public string? VergiNo { get; set; }
}

public class CreateSubscriptionRequest
{
    public int TenantId { get; set; }
    public int PlanId { get; set; }
    public DateTime BaslangicTarihi { get; set; } = DateTime.UtcNow;
    public DateTime? BitisTarihi { get; set; }
}

public class CreateSubscriptionPlanRequest
{
    public string PlanAdi { get; set; } = "";
    public int MaxKullaniciSayisi { get; set; }
    public decimal AylikUcret { get; set; }
}

public class UpdateSubscriptionPlanRequest
{
    public string PlanAdi { get; set; } = "";
    public int MaxKullaniciSayisi { get; set; }
    public decimal AylikUcret { get; set; }
}

