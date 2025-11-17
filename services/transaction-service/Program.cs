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

// JSON Options to handle circular references
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
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
    
    // Expenses tablosunu manuel olarak oluştur (eğer yoksa) - Migration'dan bağımsız
    try
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }
        try
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS expenses (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INTEGER NOT NULL,
                        ""UserId"" INTEGER NOT NULL,
                        ""GiderAdi"" VARCHAR(200) NOT NULL,
                        ""Tutar"" DECIMAL(18,2) NOT NULL,
                        ""Kategori"" VARCHAR(100),
                        ""Aciklama"" VARCHAR(500),
                        ""OlusturmaTarihi"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_expenses_TenantId"" ON expenses (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_expenses_OlusturmaTarihi"" ON expenses (""OlusturmaTarihi"");
                ";
                command.ExecuteNonQuery();
                Console.WriteLine("Expenses table created successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Expenses table creation error (may already exist): {ex.Message}");
        }
        finally
        {
            if (connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Expenses table setup error: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// Get all transactions
app.MapGet("/api/transactions", async (TransactionDbContext dbContext, string? islemTipi, DateTime? baslangic, DateTime? bitis, int? tenantId, int? userId) =>
{
    try
    {
        var query = dbContext.Transactions.Include(t => t.Items).AsQueryable();
        
        // Validation: Tarih aralığı kontrolü
        if (baslangic.HasValue && bitis.HasValue && baslangic.Value > bitis.Value)
        {
            return Results.BadRequest(new { detail = "Başlangıç tarihi bitiş tarihinden sonra olamaz!" });
        }
        
        if (!string.IsNullOrWhiteSpace(islemTipi))
            query = query.Where(t => t.IslemTipi == islemTipi);
        
        // Normalize to UTC to satisfy PostgreSQL timestamptz
        DateTime? startUtc = baslangic.HasValue
            ? DateTime.SpecifyKind(baslangic.Value, DateTimeKind.Utc)
            : null;
        DateTime? endUtc = bitis.HasValue
            ? DateTime.SpecifyKind(bitis.Value, DateTimeKind.Utc)
            : null;

        if (startUtc.HasValue)
            query = query.Where(t => t.OlusturmaTarihi >= startUtc.Value);
        
        if (endUtc.HasValue)
            query = query.Where(t => t.OlusturmaTarihi <= endUtc.Value);
        
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);
        
        // UserId filtresi - User rolü için sadece kendi işlemlerini göster
        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(t => t.UserId == userId.Value);
        }
        
        var transactions = await query.OrderByDescending(t => t.OlusturmaTarihi).ToListAsync();
        return Results.Ok(transactions);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Transactions get error: {ex.Message}");
        return Results.Problem($"İşlemler getirme hatası: {ex.Message}");
    }
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
    // Null check: Transaction null olabilir
    if (transaction == null)
    {
        return Results.BadRequest(new { detail = "İşlem verisi boş!" });
    }
    
    // Null check: Gerekli alanlar
    if (transaction.TenantId <= 0)
    {
        return Results.BadRequest(new { detail = "Geçersiz TenantId!" });
    }
    
    if (transaction.UserId <= 0)
    {
        return Results.BadRequest(new { detail = "Geçersiz UserId!" });
    }
    
    if (string.IsNullOrWhiteSpace(transaction.IslemKodu))
    {
        return Results.BadRequest(new { detail = "İşlem kodu gerekli!" });
    }
    
    transaction.OlusturmaTarihi = DateTime.UtcNow;
    
    // Calculate total if items exist - null check iyileştirildi
    if (transaction.Items != null && transaction.Items.Count > 0)
    {
        transaction.ToplamTutar = transaction.Items
            .Where(i => i != null) // Null item kontrolü
            .Sum(i => i.Subtotal);
    }
    else
    {
        transaction.ToplamTutar = 0;
    }
    
    try
    {
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();
        
        return Results.Created($"/api/transactions/{transaction.Id}", transaction);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Transaction save error: {ex.Message}");
        return Results.Problem($"Veritabanı hatası: {ex.Message}");
    }
});

// Get transaction summary/statistics
app.MapGet("/api/transactions/summary", async (TransactionDbContext dbContext, DateTime? baslangic, DateTime? bitis) =>
{
    var query = dbContext.Transactions.AsQueryable();
    
    DateTime? startUtc = baslangic.HasValue
        ? DateTime.SpecifyKind(baslangic.Value, DateTimeKind.Utc)
        : null;
    DateTime? endUtc = bitis.HasValue
        ? DateTime.SpecifyKind(bitis.Value, DateTimeKind.Utc)
        : null;

    if (startUtc.HasValue)
        query = query.Where(t => t.OlusturmaTarihi >= startUtc.Value);
    
    if (endUtc.HasValue)
        query = query.Where(t => t.OlusturmaTarihi <= endUtc.Value);
    
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

// Get sales by employee (çalışan bazlı satış istatistikleri)
app.MapGet("/api/transactions/sales-by-employee", async (TransactionDbContext dbContext, int? tenantId, DateTime? baslangic, DateTime? bitis) =>
{
    var query = dbContext.Transactions
        .Where(t => t.IslemTipi == "SATIS")
        .AsQueryable();
    
    // TenantId filtresi
    if (tenantId.HasValue && tenantId.Value > 0)
    {
        query = query.Where(t => t.TenantId == tenantId.Value);
    }
    
    // Tarih filtresi
    DateTime? startUtc = baslangic.HasValue
        ? DateTime.SpecifyKind(baslangic.Value, DateTimeKind.Utc)
        : null;
    DateTime? endUtc = bitis.HasValue
        ? DateTime.SpecifyKind(bitis.Value, DateTimeKind.Utc)
        : null;

    if (startUtc.HasValue)
        query = query.Where(t => t.OlusturmaTarihi >= startUtc.Value);
    
    if (endUtc.HasValue)
        query = query.Where(t => t.OlusturmaTarihi <= endUtc.Value);
    
    // Çalışan bazlı grupla
    var salesByEmployee = await query
        .GroupBy(t => t.UserId)
        .Select(g => new
        {
            userId = g.Key,
            transactionCount = g.Count(),
            totalSales = g.Sum(t => (decimal?)t.ToplamTutar) ?? 0,
            averageSale = g.Average(t => (decimal?)t.ToplamTutar) ?? 0
        })
        .OrderByDescending(x => x.totalSales)
        .ToListAsync();
    
    return Results.Ok(salesByEmployee);
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

// =====================================================
// EXPENSE ENDPOINTS
// =====================================================

// Get all expenses
app.MapGet("/api/expenses", async (TransactionDbContext dbContext, int? tenantId, DateTime? baslangic, DateTime? bitis) =>
{
    try
    {
        var query = dbContext.Expenses.AsQueryable();
        
        // TenantId filtresi
        if (tenantId.HasValue && tenantId.Value > 0)
        {
            query = query.Where(e => e.TenantId == tenantId.Value);
        }
        
        // Tarih filtresi
        DateTime? startUtc = baslangic.HasValue
            ? DateTime.SpecifyKind(baslangic.Value, DateTimeKind.Utc)
            : null;
        DateTime? endUtc = bitis.HasValue
            ? DateTime.SpecifyKind(bitis.Value, DateTimeKind.Utc)
            : null;

        if (startUtc.HasValue)
            query = query.Where(e => e.OlusturmaTarihi >= startUtc.Value);
        
        if (endUtc.HasValue)
            query = query.Where(e => e.OlusturmaTarihi <= endUtc.Value);
        
        var expenses = await query.OrderByDescending(e => e.OlusturmaTarihi).ToListAsync();
        return Results.Ok(expenses);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Expenses get error: {ex.Message}");
        return Results.Problem($"Giderler getirme hatası: {ex.Message}");
    }
});

// Get expense by id
app.MapGet("/api/expenses/{id}", async (int id, TransactionDbContext dbContext) =>
{
    var expense = await dbContext.Expenses.FindAsync(id);
    if (expense == null)
        return Results.NotFound(new { message = "Gider bulunamadı" });
    
    return Results.Ok(expense);
});

// Create expense
app.MapPost("/api/expenses", async (HttpContext context, TransactionDbContext dbContext) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var expense = System.Text.Json.JsonSerializer.Deserialize<Expense>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (expense == null)
            return Results.BadRequest(new { detail = "Geçersiz gider verisi" });
        
        if (string.IsNullOrWhiteSpace(expense.GiderAdi))
            return Results.BadRequest(new { detail = "Gider adı gereklidir" });
        
        if (expense.Tutar <= 0)
            return Results.BadRequest(new { detail = "Gider tutarı 0'dan büyük olmalıdır" });
        
        expense.OlusturmaTarihi = DateTime.UtcNow;
        dbContext.Expenses.Add(expense);
        await dbContext.SaveChangesAsync();
        
        return Results.Created($"/api/expenses/{expense.Id}", expense);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Expense save error: {ex.Message}");
        return Results.Problem($"Gider kaydetme hatası: {ex.Message}");
    }
});

// Update expense
app.MapPut("/api/expenses/{id}", async (int id, HttpContext context, TransactionDbContext dbContext) =>
{
    try
    {
        var existingExpense = await dbContext.Expenses.FindAsync(id);
        if (existingExpense == null)
            return Results.NotFound(new { message = "Gider bulunamadı" });
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var expense = System.Text.Json.JsonSerializer.Deserialize<Expense>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (expense == null)
            return Results.BadRequest(new { detail = "Geçersiz gider verisi" });
        
        if (string.IsNullOrWhiteSpace(expense.GiderAdi))
            return Results.BadRequest(new { detail = "Gider adı gereklidir" });
        
        if (expense.Tutar <= 0)
            return Results.BadRequest(new { detail = "Gider tutarı 0'dan büyük olmalıdır" });
        
        existingExpense.GiderAdi = expense.GiderAdi;
        existingExpense.Tutar = expense.Tutar;
        existingExpense.Kategori = expense.Kategori;
        existingExpense.Aciklama = expense.Aciklama;
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(existingExpense);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Expense update error: {ex.Message}");
        return Results.Problem($"Gider güncelleme hatası: {ex.Message}");
    }
});

// Delete expense
app.MapDelete("/api/expenses/{id}", async (int id, TransactionDbContext dbContext) =>
{
    var expense = await dbContext.Expenses.FindAsync(id);
    if (expense == null)
        return Results.NotFound(new { message = "Gider bulunamadı" });
    
    dbContext.Expenses.Remove(expense);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Gider silindi" });
});

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5003";
var url = $"http://0.0.0.0:{port}";
app.Run(url);






