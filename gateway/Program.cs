using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "De' Bakiim - API Gateway",
        Version = "v1",
        Description = "Multi-Tenant POS System - Central API Gateway<br/><strong>BY HOSTEAGLE INFORMATION TECHNOLOGIES</strong><br/><br/>Bu gateway tüm microservice'leri proxy'ler.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HostEagle Information Technologies",
            Email = "info@hosteagle.com"
        }
    });
});

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

// Service URLs (support both Docker and local development)
var receiptServiceUrl = Environment.GetEnvironmentVariable("RECEIPT_SERVICE_URL") ?? "http://localhost:5001";
var productServiceUrl = Environment.GetEnvironmentVariable("PRODUCT_SERVICE_URL") ?? "http://localhost:5002";
var transactionServiceUrl = Environment.GetEnvironmentVariable("TRANSACTION_SERVICE_URL") ?? "http://localhost:5003";
var userServiceUrl = Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? "http://localhost:5004";
var tenantServiceUrl = Environment.GetEnvironmentVariable("TENANT_SERVICE_URL") ?? "http://localhost:5005";

// Configure the HTTP request pipeline
// Swagger'ı her zaman aktif et (Development ve Production'da)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "De' Bakiim API Gateway v1");
    c.RoutePrefix = "swagger"; // Swagger UI'ı /swagger adresinde aç
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// API Gateway routes
app.MapPost("/api/v1/receipt/print", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{receiptServiceUrl}/api/receipt/print")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// =====================================================
// PRODUCT SERVICE ROUTES
// =====================================================

// Products CRUD
app.MapGet("/api/v1/products", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{productServiceUrl}/api/products");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/products/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{productServiceUrl}/api/products/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/products", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{productServiceUrl}/api/products")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/products/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Put, $"{productServiceUrl}/api/products/{id}")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/products/{id}/toggle-active", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.PutAsync($"{productServiceUrl}/api/products/{id}/toggle-active", null);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapDelete("/api/v1/products/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{productServiceUrl}/api/products/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Categories CRUD
app.MapGet("/api/v1/categories", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{productServiceUrl}/api/categories");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/categories/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{productServiceUrl}/api/categories/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/categories", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{productServiceUrl}/api/categories")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/categories/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Put, $"{productServiceUrl}/api/categories/{id}")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/categories/{id}/toggle-active", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.PutAsync($"{productServiceUrl}/api/categories/{id}/toggle-active", null);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapDelete("/api/v1/categories/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{productServiceUrl}/api/categories/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Unit of Measures CRUD
app.MapGet("/api/v1/unit-of-measures", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{productServiceUrl}/api/unit-of-measures");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/unit-of-measures/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{productServiceUrl}/api/unit-of-measures/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/unit-of-measures", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{productServiceUrl}/api/unit-of-measures")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/unit-of-measures/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Put, $"{productServiceUrl}/api/unit-of-measures/{id}")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/unit-of-measures/{id}/toggle-active", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.PutAsync($"{productServiceUrl}/api/unit-of-measures/{id}/toggle-active", null);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapDelete("/api/v1/unit-of-measures/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{productServiceUrl}/api/unit-of-measures/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Devices CRUD
app.MapGet("/api/v1/devices", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{productServiceUrl}/api/devices");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/devices/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{productServiceUrl}/api/devices/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/devices", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{productServiceUrl}/api/devices")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/devices/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Put, $"{productServiceUrl}/api/devices/{id}")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/devices/{id}/toggle-status", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.PutAsync($"{productServiceUrl}/api/devices/{id}/toggle-status", null);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapDelete("/api/v1/devices/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{productServiceUrl}/api/devices/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/devices/discover", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.PostAsync($"{productServiceUrl}/api/devices/discover?tenantId=1", null);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/devices/{id}/test", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.PostAsync($"{productServiceUrl}/api/devices/{id}/test", null);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// =====================================================
// TRANSACTION SERVICE ROUTES
// =====================================================

app.MapGet("/api/v1/transactions", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{transactionServiceUrl}/api/transactions");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// User Service routes
app.MapPost("/api/v1/auth/login", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{userServiceUrl}/api/auth/login")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/auth/register", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{userServiceUrl}/api/auth/register")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/auth/me", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, $"{userServiceUrl}/api/auth/me");
    request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// =====================================================
// USER SERVICE ROUTES
// =====================================================

app.MapGet("/api/v1/users", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, $"{userServiceUrl}/api/users");
    request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/users/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, $"{userServiceUrl}/api/users/{id}");
    request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/users", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{userServiceUrl}/api/users")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/users/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Put, $"{userServiceUrl}/api/users/{id}")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/users/{id}/roles", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Put, $"{userServiceUrl}/api/users/{id}/roles")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/users/{id}/toggle-active", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Put, $"{userServiceUrl}/api/users/{id}/toggle-active");
    request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapDelete("/api/v1/users/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Delete, $"{userServiceUrl}/api/users/{id}");
    request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/roles", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, $"{userServiceUrl}/api/roles");
    request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
var url = $"http://0.0.0.0:{port}";
app.Run(url);


