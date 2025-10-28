using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// HttpClient for API calls
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// API Gateway routes
app.MapPost("/api/v1/receipt/print", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5001/api/receipt/print")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/products", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync("http://localhost:5002/api/products");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json");
});

app.MapGet("/api/v1/transactions", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync("http://localhost:5003/api/transactions");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json");
});

app.MapControllers();

app.Run("http://localhost:5000");


