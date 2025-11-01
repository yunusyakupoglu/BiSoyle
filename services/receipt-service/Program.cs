using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using BiSoyle.Receipt.Service.Hubs;
using BiSoyle.Receipt.Service.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "De' Bakiim - Receipt Service API",
        Version = "v1",
        Description = "Multi-Tenant Fiş Yazdırma ve Yönetimi API<br/><strong>BY HOSTEAGLE INFORMATION TECHNOLOGIES</strong>",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HostEagle Information Technologies",
            Email = "info@hosteagle.com"
        }
    });
});

// Database
builder.Services.AddDbContext<ReceiptDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReceiptDbContext>();
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
app.UseAuthorization();

// SignalR Hub
app.MapHub<ReceiptHub>("/hub/receipt");

// Receipt endpoints
app.MapPost("/api/receipt/print", async (HttpContext context, IHubContext<ReceiptHub> hubContext, ReceiptDbContext dbContext) =>
{
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var data = JsonSerializer.Deserialize<ReceiptRequest>(body);
    
    if (data == null || data.Items == null || data.Items.Count == 0)
    {
        return Results.BadRequest(new { detail = "Fişte ürün bulunmuyor!" });
    }
    
    var islem_kodu = $"FS-{DateTime.Now:yyyyMMddHHmmss}";
    
    // Calculate totals and create receipt
    double toplam_tutar = 0;
    var receipt = new Receipt
    {
        TenantId = data.TenantId,
        UserId = data.UserId,
        IslemKodu = islem_kodu,
        OlusturmaTarihi = DateTime.UtcNow,
        Items = new List<ReceiptItem>()
    };
    
    foreach (var item in data.Items)
    {
        var subtotal = item.Quantity * item.Price;
        toplam_tutar += subtotal;
        
        receipt.Items.Add(new ReceiptItem
        {
            UrunId = item.UrunId,
            UrunAdi = item.Product,
            Miktar = item.Quantity,
            BirimFiyat = item.Price,
            OlcuBirimi = item.Unit,
            Subtotal = subtotal
        });
    }
    
    receipt.ToplamTutar = toplam_tutar;
    var pdf_path = $"data/receipts/receipt_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
    receipt.PdfPath = pdf_path;
    
    // Save to database
    try
    {
        dbContext.Receipts.Add(receipt);
        await dbContext.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database save error: {ex.Message}");
        return Results.Problem($"Veritabanı hatası: {ex.Message}");
    }
    
    // Background tasks
    _ = Task.Run(async () =>
    {
        // 1. PDF oluştur
        // 2. RabbitMQ'ya mesaj gönder
        // 3. SignalR ile real-time bildirim
        
        await Task.Delay(100);
        await hubContext.Clients.All.SendAsync("receiptComplete", islem_kodu, pdf_path);
    });
    
    return Results.Ok(new
    {
        ok = true,
        message = $"Fiş oluşturuldu ve veritabanına kaydedildi (İşlem Kodu: {islem_kodu})",
        type = "pdf",
        path = pdf_path,
        islem_kodu = islem_kodu,
        toplam_tutar = toplam_tutar,
        receipt_id = receipt.Id
    });
});

// Get all receipts
app.MapGet("/api/receipts", async (ReceiptDbContext dbContext) =>
{
    var receipts = await dbContext.Receipts
        .Include(r => r.Items)
        .OrderByDescending(r => r.OlusturmaTarihi)
        .ToListAsync();
    
    return Results.Ok(receipts);
});

// Get receipt by ID
app.MapGet("/api/receipts/{id}", async (int id, ReceiptDbContext dbContext) =>
{
    var receipt = await dbContext.Receipts
        .Include(r => r.Items)
        .FirstOrDefaultAsync(r => r.Id == id);
    
    if (receipt == null)
        return Results.NotFound(new { message = "Fiş bulunamadı" });
    
    return Results.Ok(receipt);
});

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5001";
var url = $"http://0.0.0.0:{port}";
app.Run(url);

// Request Models
public class ReceiptRequest
{
    public List<ReceiptRequestItem> Items { get; set; } = new();
    public int TenantId { get; set; } = 1;
    public int UserId { get; set; } = 1;
}

public class ReceiptRequestItem
{
    public int Quantity { get; set; }
    public string Unit { get; set; } = "";
    public string Product { get; set; } = "";
    public double Price { get; set; }
    public int UrunId { get; set; }
}

