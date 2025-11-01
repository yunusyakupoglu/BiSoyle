using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using BiSoyle.Receipt.Service.Hubs;
using BiSoyle.Receipt.Service.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

// Initialize QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

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
    
    // Create corresponding transaction in Transaction Service
    try
    {
        using var httpClient = new HttpClient();
        var transactionData = new
        {
            TenantId = data.TenantId,
            UserId = data.UserId,
            IslemKodu = islem_kodu,
            IslemTipi = "SATIS",
            OdemeTipi = "NAKIT",
            ToplamTutar = toplam_tutar,
            Items = data.Items.Select(item => new
            {
                UrunId = item.UrunId,
                UrunAdi = item.Product,
                Miktar = item.Quantity,
                BirimFiyat = item.Price,
                Subtotal = item.Quantity * item.Price
            }).ToList()
        };
        
        var transactionJson = JsonSerializer.Serialize(transactionData);
        var content = new StringContent(transactionJson, System.Text.Encoding.UTF8, "application/json");
        var transactionUrl = Environment.GetEnvironmentVariable("TRANSACTION_SERVICE_URL") ?? "http://localhost:5003";
        var response = await httpClient.PostAsync($"{transactionUrl}/api/transactions", content);
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Transaction created successfully for receipt {islem_kodu}");
        }
        else
        {
            Console.WriteLine($"Failed to create transaction: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating transaction: {ex.Message}");
        // Don't fail the receipt creation if transaction creation fails
    }
    
    // Create PDF in background
    _ = Task.Run(() =>
    {
        try
        {
            var pdfDirectory = Path.GetDirectoryName(pdf_path);
            if (pdfDirectory != null && !Directory.Exists(pdfDirectory))
            {
                Directory.CreateDirectory(pdfDirectory);
            }
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Custom(8, page_height: 25)); // 80mm width (thermal printer)
                    page.Margin(0.5f, Unit.Centimetre);
                    
                    page.Content().Column(column =>
                    {
                        // Header
                        column.Item().AlignCenter().Text(islem_kodu).FontSize(12).Bold();
                        column.Item().PaddingVertical(0.2f, Unit.Centimetre);
                        
                        // Company Info
                        column.Item().AlignCenter().Text("Bi' Soyle POS").FontSize(10);
                        column.Item().AlignCenter().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).FontSize(8);
                        column.Item().PaddingVertical(0.2f, Unit.Centimetre);
                        
                        // Divider
                        column.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1);
                        column.Item().PaddingVertical(0.2f, Unit.Centimetre);
                        
                        // Items
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4); // Product name
                                columns.RelativeColumn(1);  // Qty
                                columns.RelativeColumn(2);  // Price
                                columns.RelativeColumn(2);  // Total
                            });
                            
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Ürün").FontSize(8);
                                header.Cell().Element(CellStyle).AlignRight().Text("Ad").FontSize(8);
                                header.Cell().Element(CellStyle).AlignRight().Text("Fiyat").FontSize(8);
                                header.Cell().Element(CellStyle).AlignRight().Text("Toplam").FontSize(8);
                            });
                            
                            foreach (var item in receipt.Items)
                            {
                                table.Cell().Element(CellStyle).Text(item.UrunAdi).FontSize(8);
                                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Miktar} {item.OlcuBirimi}").FontSize(8);
                                table.Cell().Element(CellStyle).AlignRight().Text($"{item.BirimFiyat:F2} TL").FontSize(8);
                                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Subtotal:F2} TL").FontSize(8);
                            }
                            
                            void CellStyle(Action<IContainer> style)
                            {
                                style.PaddingVertical(0.1f, Unit.Centimetre).BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2);
                            }
                        });
                        
                        column.Item().PaddingVertical(0.2f, Unit.Centimetre);
                        
                        // Divider
                        column.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1);
                        column.Item().PaddingVertical(0.2f, Unit.Centimetre);
                        
                        // Total
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().AlignRight().Text("TOPLAM:").FontSize(10).Bold();
                            row.ConstantItem(80).AlignRight().Text($"{toplam_tutar:F2} TL").FontSize(10).Bold();
                        });
                        
                        column.Item().PaddingVertical(0.3f, Unit.Centimetre);
                        
                        // Footer
                        column.Item().AlignCenter().Text("Teşekkür Ederiz!").FontSize(10).Italic();
                        column.Item().AlignCenter().Text("İyi Günler Dileriz").FontSize(8);
                    });
                });
            });
            
            document.GeneratePdf(pdf_path);
            Console.WriteLine($"PDF created: {pdf_path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PDF creation error: {ex.Message}");
        }
    });
    
    // Background tasks
    _ = Task.Run(async () =>
    {
        await Task.Delay(500); // Wait for PDF generation
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

