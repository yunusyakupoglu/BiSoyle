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

app.MapGet("/api/transactions", () =>
{
    var transactions = new[]
    {
        new { id = 1, islem_kodu = "FS-202510270001", toplam_tutar = 1000.0, created_at = DateTime.Now },
        new { id = 2, islem_kodu = "FS-202510270002", toplam_tutar = 1500.0, created_at = DateTime.Now }
    };
    
    return Results.Ok(transactions);
});

app.MapControllers();

app.Run("http://localhost:5003");





