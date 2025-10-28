using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using BiSoyle.Receipt.Service.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.MapPost("/api/receipt/print", async (HttpContext context, IHubContext<ReceiptHub> hubContext) =>
{
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var data = JsonSerializer.Deserialize<ReceiptRequest>(body);
    
    if (data == null || data.Items == null || data.Items.Count == 0)
    {
        return Results.BadRequest(new { detail = "Fişte ürün bulunmuyor!" });
    }
    
    var islem_kodu = $"FS-{DateTime.Now:yyyyMMddHHmmss}";
    
    // Calculate totals
    double toplam_tutar = 0;
    var urun_detaylari = new List<object>();
    
    foreach (var item in data.Items)
    {
        var subtotal = item.Quantity * item.Price;
        toplam_tutar += subtotal;
        
        urun_detaylari.Add(new
        {
            urun_id = item.UrunId,
            urun_adi = item.Product,
            miktar = item.Quantity,
            birim_fiyat = item.Price,
            olcu_birimi = item.Unit,
            subtotal = subtotal
        });
    }
    
    // HIZLANDIRMA: Response hemen dön
    _ = Task.Run(async () =>
    {
        // Async işlemler burada
        // 1. Database'e kaydet
        // 2. PDF oluştur
        // 3. RabbitMQ'ya mesaj gönder
        // 4. SignalR ile real-time bildirim
        
        await Task.Delay(100); // Simüle et
        await hubContext.Clients.All.SendAsync("receiptComplete", islem_kodu, $"data/receipts/receipt_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    });
    
    var pdf_path = "data/receipts/receipt_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";
    
    return Results.Ok(new
    {
        ok = true,
        message = $"Fiş oluşturuldu (İşlem Kodu: {islem_kodu})",
        type = "pdf",
        path = pdf_path,
        islem_kodu = islem_kodu,
        toplam_tutar = toplam_tutar
    });
});

app.MapControllers();

app.Run("http://localhost:5001");

// Models
public class ReceiptRequest
{
    public List<ReceiptItem> Items { get; set; } = new();
}

public class ReceiptItem
{
    public int Quantity { get; set; }
    public string Unit { get; set; } = "";
    public string Product { get; set; } = "";
    public double Price { get; set; }
    public int UrunId { get; set; }
}

