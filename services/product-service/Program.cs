using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.MapGet("/api/products", () =>
{
    // Sample products
    var products = new[]
    {
        new { id = 1, urun_adi = "Çikolatalı kruvasan", birim_fiyat = 15.0 },
        new { id = 2, urun_adi = "Cevizli baklava", birim_fiyat = 500.0 },
        new { id = 3, urun_adi = "Patatesli poğaça", birim_fiyat = 10.0 },
        new { id = 4, urun_adi = "Beyaz peynir", birim_fiyat = 100.0 },
        new { id = 5, urun_adi = "Sucuk", birim_fiyat = 150.0 }
    };
    
    return Results.Ok(products);
});

app.MapControllers();

app.Run("http://localhost:5002");






