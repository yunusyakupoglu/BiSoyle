using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BiSoyle.Product.Service.Data;
using BiSoyle.Product.Service.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "De' Bakiim - Product Service API",
        Version = "v1",
        Description = "Multi-Tenant Ürün, Kategori ve Ölçü Birimi Yönetimi API<br/><strong>BY HOSTEAGLE INFORMATION TECHNOLOGIES</strong>",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HostEagle Information Technologies",
            Email = "info@hosteagle.com"
        }
    });
});

// Database
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Device Discovery Service
builder.Services.AddSingleton<DeviceDiscoveryService>();

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

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    try
    {
        db.Database.Migrate();
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

// Get all products
app.MapGet("/api/products", async (ProductDbContext dbContext, bool? aktif, int? tenantId) =>
{
    var query = dbContext.Products.AsQueryable();
    
    if (aktif.HasValue)
        query = query.Where(p => p.Aktif == aktif.Value);
    if (tenantId.HasValue)
        query = query.Where(p => p.TenantId == tenantId.Value);
    
    var products = await query.OrderBy(p => p.UrunAdi).ToListAsync();
    return Results.Ok(products);
});

// Get product by ID
app.MapGet("/api/products/{id}", async (int id, ProductDbContext dbContext) =>
{
    var product = await dbContext.Products.FindAsync(id);
    
    if (product == null)
        return Results.NotFound(new { message = "Ürün bulunamadı" });
    
    return Results.Ok(product);
});

// Create product
app.MapPost("/api/products", async (Product product, ProductDbContext dbContext) =>
{
    // Null check
    if (product == null)
    {
        return Results.BadRequest(new { detail = "Ürün verisi boş!" });
    }
    
    // Validation
    if (string.IsNullOrWhiteSpace(product.UrunAdi))
    {
        return Results.BadRequest(new { detail = "Ürün adı gerekli!" });
    }
    
    if (product.TenantId <= 0)
    {
        return Results.BadRequest(new { detail = "Geçersiz TenantId!" });
    }
    
    if (product.BirimFiyat < 0)
    {
        return Results.BadRequest(new { detail = "Birim fiyat negatif olamaz!" });
    }
    
    try
    {
        product.OlusturmaTarihi = DateTime.UtcNow;
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        
        return Results.Created($"/api/products/{product.Id}", product);
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
    {
        var innerMessage = ex.InnerException?.Message ?? ex.Message;
        if (innerMessage.Contains("duplicate") || innerMessage.Contains("unique"))
        {
            return Results.Conflict(new { detail = "Bu ürün adı zaten kullanılıyor!" });
        }
        return Results.Problem($"Veritabanı hatası: {innerMessage}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Product create error: {ex.Message}");
        return Results.Problem($"Ürün oluşturma hatası: {ex.Message}");
    }
});

// Update product
app.MapPut("/api/products/{id}", async (int id, Product updatedProduct, ProductDbContext dbContext) =>
{
    // Null check
    if (updatedProduct == null)
    {
        return Results.BadRequest(new { detail = "Ürün verisi boş!" });
    }
    
    // Validation
    if (string.IsNullOrWhiteSpace(updatedProduct.UrunAdi))
    {
        return Results.BadRequest(new { detail = "Ürün adı gerekli!" });
    }
    
    if (updatedProduct.BirimFiyat < 0)
    {
        return Results.BadRequest(new { detail = "Birim fiyat negatif olamaz!" });
    }
    
    try
    {
        var product = await dbContext.Products.FindAsync(id);
        
        if (product == null)
            return Results.NotFound(new { message = "Ürün bulunamadı" });
        
        product.UrunAdi = updatedProduct.UrunAdi;
        product.BirimFiyat = updatedProduct.BirimFiyat;
        product.OlcuBirimi = updatedProduct.OlcuBirimi ?? "Adet";
        product.StokMiktari = updatedProduct.StokMiktari;
        product.Aktif = updatedProduct.Aktif;
        product.GuncellemeTarihi = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(product);
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
    {
        var innerMessage = ex.InnerException?.Message ?? ex.Message;
        if (innerMessage.Contains("duplicate") || innerMessage.Contains("unique"))
        {
            return Results.Conflict(new { detail = "Bu ürün adı zaten kullanılıyor!" });
        }
        return Results.Problem($"Veritabanı hatası: {innerMessage}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Product update error: {ex.Message}");
        return Results.Problem($"Ürün güncelleme hatası: {ex.Message}");
    }
});

// Toggle product active status
app.MapPut("/api/products/{id}/toggle-active", async (int id, ProductDbContext dbContext) =>
{
    var product = await dbContext.Products.FindAsync(id);
    
    if (product == null)
        return Results.NotFound(new { message = "Ürün bulunamadı" });
    
    product.Aktif = !product.Aktif;
    product.GuncellemeTarihi = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = $"Ürün {(product.Aktif ? "aktif" : "pasif")} edildi", aktif = product.Aktif });
});

// Delete product (hard delete)
app.MapDelete("/api/products/{id}", async (int id, ProductDbContext dbContext) =>
{
    var product = await dbContext.Products.FindAsync(id);
    
    if (product == null)
        return Results.NotFound(new { message = "Ürün bulunamadı" });
    
    dbContext.Products.Remove(product);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Ürün kalıcı olarak silindi", id });
});

// Get all categories
app.MapGet("/api/categories", async (ProductDbContext dbContext, bool? aktif, int? tenantId) =>
{
    var query = dbContext.Categories.AsQueryable();
    
    if (aktif.HasValue)
        query = query.Where(c => c.Aktif == aktif.Value);
    if (tenantId.HasValue)
        query = query.Where(c => c.TenantId == tenantId.Value);
    
    var categories = await query.OrderBy(c => c.KategoriAdi).ToListAsync();
    return Results.Ok(categories);
});

// Get category by ID
app.MapGet("/api/categories/{id}", async (int id, ProductDbContext dbContext) =>
{
    var category = await dbContext.Categories.FindAsync(id);
    
    if (category == null)
        return Results.NotFound(new { message = "Kategori bulunamadı" });
    
    return Results.Ok(category);
});

// Create category
app.MapPost("/api/categories", async (Category category, ProductDbContext dbContext) =>
{
    dbContext.Categories.Add(category);
    await dbContext.SaveChangesAsync();
    
    return Results.Created($"/api/categories/{category.Id}", category);
});

// Update category
app.MapPut("/api/categories/{id}", async (int id, Category updatedCategory, ProductDbContext dbContext) =>
{
    var category = await dbContext.Categories.FindAsync(id);
    
    if (category == null)
        return Results.NotFound(new { message = "Kategori bulunamadı" });
    
    category.KategoriAdi = updatedCategory.KategoriAdi;
    category.Aciklama = updatedCategory.Aciklama;
    category.Aktif = updatedCategory.Aktif;
    
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(category);
});

// Toggle category active status
app.MapPut("/api/categories/{id}/toggle-active", async (int id, ProductDbContext dbContext) =>
{
    var category = await dbContext.Categories.FindAsync(id);
    
    if (category == null)
        return Results.NotFound(new { message = "Kategori bulunamadı" });
    
    category.Aktif = !category.Aktif;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = $"Kategori {(category.Aktif ? "aktif" : "pasif")} edildi", aktif = category.Aktif });
});

// Delete category (hard delete)
app.MapDelete("/api/categories/{id}", async (int id, ProductDbContext dbContext) =>
{
    var category = await dbContext.Categories.FindAsync(id);
    
    if (category == null)
        return Results.NotFound(new { message = "Kategori bulunamadı" });
    
    // Check if category has products
    var hasProducts = await dbContext.Products.AnyAsync(p => p.KategoriId == id);
    
    if (hasProducts)
        return Results.BadRequest(new { message = "Bu kategori kullanımda, silinemez. Pasif edebilirsiniz." });
    
    dbContext.Categories.Remove(category);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Kategori kalıcı olarak silindi", id });
});

// Get all unit of measures (global). Accept tenantId for API symmetry (ignored).
app.MapGet("/api/unit-of-measures", async (ProductDbContext dbContext, bool? aktif, int? tenantId) =>
{
    var query = dbContext.UnitOfMeasures.AsQueryable();
    
    if (aktif.HasValue)
        query = query.Where(u => u.Aktif == aktif.Value);
    
    var units = await query.OrderBy(u => u.BirimAdi).ToListAsync();
    return Results.Ok(units);
});

// Get unit of measure by ID
app.MapGet("/api/unit-of-measures/{id}", async (int id, ProductDbContext dbContext) =>
{
    var unit = await dbContext.UnitOfMeasures.FindAsync(id);
    
    if (unit == null)
        return Results.NotFound(new { message = "Ölçü birimi bulunamadı" });
    
    return Results.Ok(unit);
});

// Create unit of measure
app.MapPost("/api/unit-of-measures", async (UnitOfMeasure unit, ProductDbContext dbContext) =>
{
    unit.OlusturmaTarihi = DateTime.UtcNow;
    dbContext.UnitOfMeasures.Add(unit);
    await dbContext.SaveChangesAsync();
    
    return Results.Created($"/api/unit-of-measures/{unit.Id}", unit);
});

// Update unit of measure
app.MapPut("/api/unit-of-measures/{id}", async (int id, UnitOfMeasure updatedUnit, ProductDbContext dbContext) =>
{
    var unit = await dbContext.UnitOfMeasures.FindAsync(id);
    
    if (unit == null)
        return Results.NotFound(new { message = "Ölçü birimi bulunamadı" });
    
    unit.BirimAdi = updatedUnit.BirimAdi;
    unit.Kisaltma = updatedUnit.Kisaltma;
    unit.Aktif = updatedUnit.Aktif;
    
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(unit);
});

// Toggle unit of measure active status
app.MapPut("/api/unit-of-measures/{id}/toggle-active", async (int id, ProductDbContext dbContext) =>
{
    var unit = await dbContext.UnitOfMeasures.FindAsync(id);
    
    if (unit == null)
        return Results.NotFound(new { message = "Ölçü birimi bulunamadı" });
    
    unit.Aktif = !unit.Aktif;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = $"Ölçü birimi {(unit.Aktif ? "aktif" : "pasif")} edildi", aktif = unit.Aktif });
});

// Delete unit of measure (hard delete)
app.MapDelete("/api/unit-of-measures/{id}", async (int id, ProductDbContext dbContext) =>
{
    var unit = await dbContext.UnitOfMeasures.FindAsync(id);
    
    if (unit == null)
        return Results.NotFound(new { message = "Ölçü birimi bulunamadı" });
    
    // Check if unit is used
    var hasProducts = await dbContext.Products.AnyAsync(p => p.OlcuBirimi == unit.BirimAdi);
    
    if (hasProducts)
        return Results.BadRequest(new { message = "Bu ölçü birimi kullanımda, silinemez. Pasif edebilirsiniz." });
    
    dbContext.UnitOfMeasures.Remove(unit);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Ölçü birimi kalıcı olarak silindi", id });
});

// =====================================================
// DEVICE ENDPOINTS
// =====================================================

// Get all devices
app.MapGet("/api/devices", async (ProductDbContext dbContext, bool? aktif) =>
{
    var query = dbContext.Devices.AsQueryable();

    if (aktif.HasValue)
    {
        query = query.Where(d => d.Durum == (aktif.Value ? "aktif" : "pasif"));
    }

    var devices = await query.OrderBy(d => d.CihazAdi).ToListAsync();
    return Results.Ok(devices);
});

// Get device by ID
app.MapGet("/api/devices/{id}", async (int id, ProductDbContext dbContext) =>
{
    var device = await dbContext.Devices.FindAsync(id);
    return device is not null ? Results.Ok(device) : Results.NotFound(new { message = "Cihaz bulunamadı" });
});

// Create device
app.MapPost("/api/devices", async (Device device, ProductDbContext dbContext) =>
{
    var exists = await dbContext.Devices
        .AnyAsync(d => d.TenantId == device.TenantId && d.CihazAdi == device.CihazAdi);
    
    if (exists)
    {
        return Results.BadRequest(new { message = "Bu firma için aynı isimde bir cihaz zaten mevcut!" });
    }

    device.OlusturmaTarihi = DateTime.UtcNow;
    dbContext.Devices.Add(device);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/api/devices/{device.Id}", device);
});

// Update device
app.MapPut("/api/devices/{id}", async (int id, Device updatedDevice, ProductDbContext dbContext) =>
{
    var device = await dbContext.Devices.FindAsync(id);
    if (device is null) return Results.NotFound(new { message = "Cihaz bulunamadı" });

    var exists = await dbContext.Devices
        .AnyAsync(d => d.Id != id && d.TenantId == device.TenantId && d.CihazAdi == updatedDevice.CihazAdi);
    
    if (exists)
    {
        return Results.BadRequest(new { message = "Bu firma için aynı isimde başka bir cihaz mevcut!" });
    }

    device.CihazAdi = updatedDevice.CihazAdi;
    device.CihazTipi = updatedDevice.CihazTipi;
    device.Marka = updatedDevice.Marka;
    device.Model = updatedDevice.Model;
    device.BaglantiTipi = updatedDevice.BaglantiTipi;
    device.Durum = updatedDevice.Durum;

    await dbContext.SaveChangesAsync();
    return Results.Ok(device);
});

// Toggle device status
app.MapPut("/api/devices/{id}/toggle-status", async (int id, ProductDbContext dbContext) =>
{
    var device = await dbContext.Devices.FindAsync(id);
    if (device is null) return Results.NotFound(new { message = "Cihaz bulunamadı" });

    device.Durum = device.Durum == "aktif" ? "pasif" : "aktif";
    await dbContext.SaveChangesAsync();
    return Results.Ok(new { message = $"Cihaz {device.Durum} edildi", device });
});

// Delete device (hard delete)
app.MapDelete("/api/devices/{id}", async (int id, ProductDbContext dbContext) =>
{
    var device = await dbContext.Devices.FindAsync(id);
    if (device is null) return Results.NotFound(new { message = "Cihaz bulunamadı" });

    dbContext.Devices.Remove(device);
    await dbContext.SaveChangesAsync();
    return Results.Ok(new { message = "Cihaz kalıcı olarak silindi", id });
});

// Discover devices (Otomatik Tarama)
app.MapPost("/api/devices/discover", async (int tenantId, ProductDbContext dbContext, DeviceDiscoveryService discoveryService) =>
{
    try
    {
        var discoveredDevices = discoveryService.DiscoverAllDevices();
        var addedDevices = new List<Device>();
        
        foreach (var discovered in discoveredDevices)
        {
            // Check if device already exists
            var exists = await dbContext.Devices
                .AnyAsync(d => d.TenantId == tenantId && d.CihazAdi == discovered.CihazAdi);
            
            if (!exists)
            {
                var device = new Device
                {
                    TenantId = tenantId,
                    CihazAdi = discovered.CihazAdi,
                    CihazTipi = discovered.CihazTipi,
                    Marka = discovered.Marka,
                    Model = discovered.Model,
                    BaglantiTipi = discovered.BaglantiTipi,
                    Durum = "aktif",
                    OlusturmaTarihi = DateTime.UtcNow
                };
                
                dbContext.Devices.Add(device);
                addedDevices.Add(device);
            }
        }
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(new 
        { 
            message = $"{addedDevices.Count} yeni cihaz bulundu ve eklendi",
            discovered = discoveredDevices.Count,
            added = addedDevices.Count,
            devices = addedDevices
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = $"Cihaz tarama hatası: {ex.Message}" });
    }
});

// Test Bluetooth Device
app.MapPost("/api/devices/{id}/test", async (int id, ProductDbContext dbContext, DeviceDiscoveryService discoveryService) =>
{
    var device = await dbContext.Devices.FindAsync(id);
    if (device is null)
        return Results.NotFound(new { message = "Cihaz bulunamadı" });
    
    if (device.BaglantiTipi != "bluetooth")
    {
        return Results.BadRequest(new { message = "Bu endpoint sadece Bluetooth cihazları için kullanılabilir." });
    }
    
    var testResult = discoveryService.TestBluetoothDevice(device.CihazAdi);
    
    return Results.Ok(new
    {
        deviceId = id,
        deviceName = device.CihazAdi,
        testResult = testResult
    });
});

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5002";
var url = $"http://0.0.0.0:{port}";
app.Run(url);






