using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BiSoyle.Transaction.Service.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "De' Bakiim - Transaction Service API",
        Version = "v1",
        Description = "Multi-Tenant İşlem ve Satış Yönetimi API<br/><strong>BY HOSTEAGLE INFORMATION TECHNOLOGIES</strong>",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HostEagle Information Technologies",
            Email = "info@hosteagle.com"
        }
    });
});

// Database
builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
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

// Get all transactions
app.MapGet("/api/transactions", async (TransactionDbContext dbContext, string? islemTipi, DateTime? baslangic, DateTime? bitis) =>
{
    var query = dbContext.Transactions.Include(t => t.Items).AsQueryable();
    
    if (!string.IsNullOrEmpty(islemTipi))
        query = query.Where(t => t.IslemTipi == islemTipi);
    
    if (baslangic.HasValue)
        query = query.Where(t => t.OlusturmaTarihi >= baslangic.Value);
    
    if (bitis.HasValue)
        query = query.Where(t => t.OlusturmaTarihi <= bitis.Value);
    
    var transactions = await query.OrderByDescending(t => t.OlusturmaTarihi).ToListAsync();
    return Results.Ok(transactions);
});

// Get transaction by ID
app.MapGet("/api/transactions/{id}", async (int id, TransactionDbContext dbContext) =>
{
    var transaction = await dbContext.Transactions
        .Include(t => t.Items)
        .FirstOrDefaultAsync(t => t.Id == id);
    
    if (transaction == null)
        return Results.NotFound(new { message = "İşlem bulunamadı" });
    
    return Results.Ok(transaction);
});

// Create transaction
app.MapPost("/api/transactions", async (Transaction transaction, TransactionDbContext dbContext) =>
{
    transaction.OlusturmaTarihi = DateTime.UtcNow;
    
    // Calculate total if items exist
    if (transaction.Items != null && transaction.Items.Count > 0)
    {
        transaction.ToplamTutar = transaction.Items.Sum(i => i.Subtotal);
    }
    
    dbContext.Transactions.Add(transaction);
    await dbContext.SaveChangesAsync();
    
    return Results.Created($"/api/transactions/{transaction.Id}", transaction);
});

// Get transaction summary/statistics
app.MapGet("/api/transactions/summary", async (TransactionDbContext dbContext, DateTime? baslangic, DateTime? bitis) =>
{
    var query = dbContext.Transactions.AsQueryable();
    
    if (baslangic.HasValue)
        query = query.Where(t => t.OlusturmaTarihi >= baslangic.Value);
    
    if (bitis.HasValue)
        query = query.Where(t => t.OlusturmaTarihi <= bitis.Value);
    
    var summary = new
    {
        toplam_islem_sayisi = await query.CountAsync(),
        toplam_tutar = await query.SumAsync(t => (decimal?)t.ToplamTutar) ?? 0,
        ortalama_tutar = await query.AverageAsync(t => (decimal?)t.ToplamTutar) ?? 0,
        islem_tipi_dagilimlari = await query
            .GroupBy(t => t.IslemTipi)
            .Select(g => new { islem_tipi = g.Key, adet = g.Count(), toplam = g.Sum(t => t.ToplamTutar) })
            .ToListAsync()
    };
    
    return Results.Ok(summary);
});

// Delete transaction
app.MapDelete("/api/transactions/{id}", async (int id, TransactionDbContext dbContext) =>
{
    var transaction = await dbContext.Transactions
        .Include(t => t.Items)
        .FirstOrDefaultAsync(t => t.Id == id);
    
    if (transaction == null)
        return Results.NotFound(new { message = "İşlem bulunamadı" });
    
    dbContext.Transactions.Remove(transaction);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "İşlem silindi" });
});

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5003";
var url = $"http://0.0.0.0:{port}";
app.Run(url);






