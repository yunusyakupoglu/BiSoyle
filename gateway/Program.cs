using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using BiSoyle.Gateway.Hubs;
using System.Linq;

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

// CORS - Güvenli CORS ayarları
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // Development ve Production'da aynı şekilde çalışsın (SignalR için credentials gerekli)
        var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(',') 
            ?? new[] { "http://localhost:4200", "http://localhost:4300", "http://127.0.0.1:4200" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// HttpClient for API calls
builder.Services.AddHttpClient();

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Set port for Gateway (5000)
var gatewayPort = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://localhost:{gatewayPort}");

// Service URLs (support both Docker and local development)
var receiptServiceUrl = Environment.GetEnvironmentVariable("RECEIPT_SERVICE_URL") ?? "http://localhost:5001";
var productServiceUrl = Environment.GetEnvironmentVariable("PRODUCT_SERVICE_URL") ?? "http://localhost:5002";
var transactionServiceUrl = Environment.GetEnvironmentVariable("TRANSACTION_SERVICE_URL") ?? "http://localhost:5003";
var userServiceUrl = Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? "http://localhost:5004";
var tenantServiceUrl = Environment.GetEnvironmentVariable("TENANT_SERVICE_URL") ?? "http://localhost:5006";
var taskServiceUrl = Environment.GetEnvironmentVariable("TASK_SERVICE_URL") ?? "http://localhost:5007";
var announcementServiceUrl = Environment.GetEnvironmentVariable("ANNOUNCEMENT_SERVICE_URL") ?? "http://localhost:5008";
var ticketServiceUrl = Environment.GetEnvironmentVariable("TICKET_SERVICE_URL") ?? "http://localhost:5005";

// Configure the HTTP request pipeline
// CORS'u en başa al (OPTIONS istekleri için)
app.UseCors("AllowAll");

// Swagger'ı her zaman aktif et (Development ve Production'da)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "De' Bakiim API Gateway v1");
    c.RoutePrefix = "swagger"; // Swagger UI'ı /swagger adresinde aç
});

app.UseAuthentication();
app.UseAuthorization();

// API Gateway routes
app.MapPost("/api/v1/receipt/print", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        
        // Null check
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { detail = "İstek gövdesi boş!" });
        }
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"{receiptServiceUrl}/api/receipt/print")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        
        // Authorization header'ı kontrol et ve ekle
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (TaskCanceledException)
    {
        return Results.Problem("İstek zaman aşımına uğradı", statusCode: 504);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Servis hatası: {ex.Message}", statusCode: 502);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// =====================================================
// PRODUCT SERVICE ROUTES
// =====================================================

// Products CRUD
app.MapGet("/api/v1/products", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    var response = await client.GetAsync($"{productServiceUrl}/api/products{query}");
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
    var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    var response = await client.GetAsync($"{productServiceUrl}/api/categories{query}");
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
    var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    var response = await client.GetAsync($"{productServiceUrl}/api/unit-of-measures{query}");
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
    var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    var request = new HttpRequestMessage(HttpMethod.Get, $"{transactionServiceUrl}/api/transactions{query}");
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/transactions/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{transactionServiceUrl}/api/transactions/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/transactions", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{transactionServiceUrl}/api/transactions")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapDelete("/api/v1/transactions/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{transactionServiceUrl}/api/transactions/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/transactions/sales-by-employee", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var request = new HttpRequestMessage(HttpMethod.Get, $"{transactionServiceUrl}/api/transactions/sales-by-employee{query}");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Transaction service error: {ex.Message}");
        return Results.Json(new { salesByEmployee = new object[0] }, statusCode: 200);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Transaction service error: {ex.Message}");
        return Results.Json(new { salesByEmployee = new object[0] }, statusCode: 200);
    }
});

// Expense routes
app.MapGet("/api/v1/expenses", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    var response = await client.GetAsync($"{transactionServiceUrl}/api/expenses{query}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/expenses/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{transactionServiceUrl}/api/expenses/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/expenses", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{transactionServiceUrl}/api/expenses")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/expenses/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Put, $"{transactionServiceUrl}/api/expenses/{id}")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapDelete("/api/v1/expenses/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{transactionServiceUrl}/api/expenses/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// =====================================================
// TASK SERVICE ROUTES
// =====================================================

// Get all tasks
app.MapGet("/api/v1/tasks", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    var request = new HttpRequestMessage(HttpMethod.Get, $"{taskServiceUrl}/api/tasks{query}");
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Get task by ID
app.MapGet("/api/v1/tasks/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{taskServiceUrl}/api/tasks/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Create task
app.MapPost("/api/v1/tasks", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{taskServiceUrl}/api/tasks")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Update task
app.MapPut("/api/v1/tasks/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Put, $"{taskServiceUrl}/api/tasks/{id}")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Update task status
app.MapPatch("/api/v1/tasks/{id}/status", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Patch, $"{taskServiceUrl}/api/tasks/{id}/status")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Delete task
app.MapDelete("/api/v1/tasks/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{taskServiceUrl}/api/tasks/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Get task comments
app.MapGet("/api/v1/tasks/{taskId}/comments", async (int taskId, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{taskServiceUrl}/api/tasks/{taskId}/comments");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Add comment to task
app.MapPost("/api/v1/tasks/{taskId}/comments", async (int taskId, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{taskServiceUrl}/api/tasks/{taskId}/comments")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Delete comment
app.MapDelete("/api/v1/tasks/{taskId}/comments/{commentId}", async (int taskId, int commentId, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{taskServiceUrl}/api/tasks/{taskId}/comments/{commentId}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// =====================================================
// NOTIFICATION ROUTES
// =====================================================

// Get notifications
app.MapGet("/api/v1/notifications", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var request = new HttpRequestMessage(HttpMethod.Get, $"{taskServiceUrl}/api/notifications{query}");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Notification service error: {ex.Message}");
        return Results.Json(new { notifications = new object[0] }, statusCode: 200);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Notification service error: {ex.Message}");
        return Results.Json(new { notifications = new object[0] }, statusCode: 200);
    }
});

// Get unread notification count
app.MapGet("/api/v1/notifications/unread-count", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var request = new HttpRequestMessage(HttpMethod.Get, $"{taskServiceUrl}/api/notifications/unread-count{query}");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Notification unread-count service error: {ex.Message}");
        return Results.Json(new { count = 0 }, statusCode: 200);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Notification unread-count service error: {ex.Message}");
        return Results.Json(new { count = 0 }, statusCode: 200);
    }
});

// Mark notification as read
app.MapPatch("/api/v1/notifications/{id}/read", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Patch, $"{taskServiceUrl}/api/notifications/{id}/read");
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Mark all notifications as read
app.MapPatch("/api/v1/notifications/read-all", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Patch, $"{taskServiceUrl}/api/notifications/read-all")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Delete notification
app.MapDelete("/api/v1/notifications/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{taskServiceUrl}/api/notifications/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// =====================================================
// ANNOUNCEMENT SERVICE ROUTES
// =====================================================

// Get all announcements (genel route önce gelmeli - query string'li istekler için)
app.MapGet("/api/v1/announcements", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var request = new HttpRequestMessage(HttpMethod.Get, $"{announcementServiceUrl}/api/announcements{query}");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Announcement service error: {ex.Message}");
        return Results.Json(new { announcements = new object[0] }, statusCode: 200);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Announcement service error: {ex.Message}");
        return Results.Json(new { announcements = new object[0] }, statusCode: 200);
    }
});

// Get announcement by ID (spesifik route sonra gelmeli)
app.MapGet("/api/v1/announcements/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{announcementServiceUrl}/api/announcements/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Create announcement (SuperAdmin only)
app.MapPost("/api/v1/announcements", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{announcementServiceUrl}/api/announcements")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Update announcement
app.MapPut("/api/v1/announcements/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Put, $"{announcementServiceUrl}/api/announcements/{id}")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Delete announcement
app.MapDelete("/api/v1/announcements/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{announcementServiceUrl}/api/announcements/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Get unread announcement count
app.MapGet("/api/v1/announcements/unread-count", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var request = new HttpRequestMessage(HttpMethod.Get, $"{announcementServiceUrl}/api/announcements/unread-count{query}");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Announcement unread-count service error: {ex.Message}");
        return Results.Json(new { count = 0 }, statusCode: 200);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Announcement unread-count service error: {ex.Message}");
        return Results.Json(new { count = 0 }, statusCode: 200);
    }
});

// Mark announcement as read
app.MapPost("/api/v1/announcements/{id}/mark-read", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{announcementServiceUrl}/api/announcements/{id}/mark-read")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// =====================================================
// TICKET SERVICE ROUTES
// =====================================================

// Get all tickets (genel route önce gelmeli - query string'li istekler için)
app.MapGet("/api/v1/tickets", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var url = $"{ticketServiceUrl}/api/tickets{query}";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway: Error forwarding ticket request: {ex.Message}");
        return Results.Problem($"Ticket servisine istek iletilemedi: {ex.Message}");
    }
});

// Get notifications (ticket notifications) - {id} route'undan ÖNCE gelmeli
app.MapGet("/api/v1/tickets/notifications", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    var request = new HttpRequestMessage(HttpMethod.Get, $"{ticketServiceUrl}/api/notifications{query}");
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Get unread count (ticket notifications) - {id} route'undan ÖNCE gelmeli
app.MapGet("/api/v1/tickets/notifications/unread-count", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    var request = new HttpRequestMessage(HttpMethod.Get, $"{ticketServiceUrl}/api/notifications/unread-count{query}");
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Mark all notifications as read
app.MapPatch("/api/v1/tickets/notifications/read-all", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Patch, $"{ticketServiceUrl}/api/notifications/read-all")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Get ticket by ID (spesifik route sonra gelmeli)
app.MapGet("/api/v1/tickets/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{ticketServiceUrl}/api/tickets/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Create ticket
app.MapPost("/api/v1/tickets", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{ticketServiceUrl}/api/tickets")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Update ticket
app.MapPut("/api/v1/tickets/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Put, $"{ticketServiceUrl}/api/tickets/{id}")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Delete ticket
app.MapDelete("/api/v1/tickets/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{ticketServiceUrl}/api/tickets/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Get ticket comments
app.MapGet("/api/v1/tickets/{ticketId}/comments", async (int ticketId, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{ticketServiceUrl}/api/tickets/{ticketId}/comments");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Add comment to ticket
app.MapPost("/api/v1/tickets/{ticketId}/comments", async (int ticketId, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{ticketServiceUrl}/api/tickets/{ticketId}/comments")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Delete comment
app.MapDelete("/api/v1/tickets/{ticketId}/comments/{commentId}", async (int ticketId, int commentId, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{ticketServiceUrl}/api/tickets/{ticketId}/comments/{commentId}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Upload file for ticket
app.MapPost("/api/v1/tickets/{ticketId}/upload", async (int ticketId, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    
    if (!context.Request.HasFormContentType)
        return Results.BadRequest(new { detail = "Form data bekleniyor" });
    
    var form = await context.Request.ReadFormAsync();
    var formContent = new MultipartFormDataContent();
    
    foreach (var file in form.Files)
    {
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        formContent.Add(fileContent, "file", file.FileName);
    }
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{ticketServiceUrl}/api/tickets/{ticketId}/upload")
    {
        Content = formContent
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Upload file for comment
app.MapPost("/api/v1/tickets/{ticketId}/comments/{commentId}/upload", async (int ticketId, int commentId, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    
    if (!context.Request.HasFormContentType)
        return Results.BadRequest(new { detail = "Form data bekleniyor" });
    
    var form = await context.Request.ReadFormAsync();
    var formContent = new MultipartFormDataContent();
    
    foreach (var file in form.Files)
    {
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        formContent.Add(fileContent, "file", file.FileName);
    }
    
    var request = new HttpRequestMessage(HttpMethod.Post, $"{ticketServiceUrl}/api/tickets/{ticketId}/comments/{commentId}/upload")
    {
        Content = formContent
    };
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Download file
app.MapGet("/api/v1/tickets/attachments/{id}/download", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.GetAsync($"{ticketServiceUrl}/api/tickets/attachments/{id}/download");
    
    if (response.IsSuccessStatusCode)
    {
        var fileBytes = await response.Content.ReadAsByteArrayAsync();
        var fileName = response.Content.Headers.ContentDisposition?.FileName ?? $"attachment_{id}";
        return Results.File(fileBytes, "application/octet-stream", fileName);
    }
    
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Mark notification as read
app.MapPatch("/api/v1/tickets/notifications/{id}/read", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{ticketServiceUrl}/api/notifications/{id}/read");
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Delete notification
app.MapDelete("/api/v1/tickets/notifications/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var response = await client.DeleteAsync($"{ticketServiceUrl}/api/notifications/{id}");
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// User Service routes
app.MapPost("/api/v1/auth/login", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        // Enable buffering to allow body to be read multiple times
        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"{userServiceUrl}/api/auth/login")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Auth login error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem($"Authentication servisi hatası: {ex.Message}", statusCode: 500);
    }
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
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/users/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, $"{userServiceUrl}/api/users/{id}");
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
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
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
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
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
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
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Maintenance: bulk fix avatars
app.MapPut("/api/v1/users/fix-avatars", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Put, $"{userServiceUrl}/api/users/fix-avatars");
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/users/{id}/toggle-active", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Put, $"{userServiceUrl}/api/users/{id}/toggle-active");
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapDelete("/api/v1/users/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Delete, $"{userServiceUrl}/api/users/{id}");
    
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/users/{id}/generate-password", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{userServiceUrl}/api/users/{id}/generate-password");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }

        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Generate password error: {ex.Message}");
        return Results.Problem($"Parola üretilirken hata: {ex.Message}", statusCode: 500);
    }
});

app.MapGet("/api/v1/roles", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{userServiceUrl}/api/roles");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// =====================================================
// TENANT SERVICE ROUTES
// =====================================================

// Get all tenants
app.MapGet("/api/v1/tenants", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/tenants");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (TaskCanceledException)
    {
        return Results.Problem("İstek zaman aşımına uğradı", statusCode: 504);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Servis hatası: {ex.Message}", statusCode: 502);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

app.MapGet("/api/v1/virtual-pos/banks", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/virtual-pos/banks");

    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }

    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();

    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/virtual-pos/installments", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/virtual-pos/installments");

    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }

    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();

    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/virtual-pos/transactions", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/virtual-pos/transactions{queryString}");

    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }

    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();

    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/virtual-pos/charge", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

    var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/virtual-pos/charge")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };

    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }

    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();

    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Provider-specific init and callbacks
app.MapPost("/api/v1/virtual-pos/paytr/init", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/virtual-pos/paytr/init");
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/virtual-pos/iyzico/init", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/virtual-pos/iyzico/init");
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/virtual-pos/callback/paytr", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/virtual-pos/callback/paytr")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPost("/api/v1/virtual-pos/callback/iyzico", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/virtual-pos/callback/iyzico")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});
app.MapGet("/api/v1/platform-settings/payment", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/platform-settings/payment");

    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }

    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();

    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/platform-settings/payment", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

    var request = new HttpRequestMessage(HttpMethod.Put, $"{tenantServiceUrl}/api/platform-settings/payment")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };

    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }

    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();

    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Payment Gateway settings
app.MapGet("/api/v1/platform-settings/gateway", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/platform-settings/gateway");
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/platform-settings/gateway", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var request = new HttpRequestMessage(HttpMethod.Put, $"{tenantServiceUrl}/api/platform-settings/gateway")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});
app.MapGet("/api/v1/platform-settings/mail", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/platform-settings/mail");

    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }

    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();

    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/v1/platform-settings/mail", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

    var request = new HttpRequestMessage(HttpMethod.Put, $"{tenantServiceUrl}/api/platform-settings/mail")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };

    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }

    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();

    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Get tenant by ID
app.MapGet("/api/v1/tenants/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/tenants/{id}");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Create tenant
app.MapPost("/api/v1/tenants", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { detail = "İstek gövdesi boş!" });
        }
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/tenants")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (TaskCanceledException)
    {
        return Results.Problem("İstek zaman aşımına uğradı", statusCode: 504);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Servis hatası: {ex.Message}", statusCode: 502);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Update tenant
app.MapPut("/api/v1/tenants/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { detail = "İstek gövdesi boş!" });
        }
        
        var request = new HttpRequestMessage(HttpMethod.Put, $"{tenantServiceUrl}/api/tenants/{id}")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Toggle tenant active status
app.MapPut("/api/v1/tenants/{id}/toggle-active", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Put, $"{tenantServiceUrl}/api/tenants/{id}/toggle-active");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Delete tenant
app.MapDelete("/api/v1/tenants/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{tenantServiceUrl}/api/tenants/{id}");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Generate admin password for tenant
app.MapPost("/api/v1/tenants/{id}/generate-admin-password", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/tenants/{id}/generate-admin-password");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        // Enable buffering to allow body to be read multiple times
        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Tenant service error ({response.StatusCode}): {result}");
        }
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Generate admin password error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem($"Parola üretilirken hata: {ex.Message}", statusCode: 500);
    }
});

// Resend license email for tenant
app.MapPost("/api/v1/tenants/{id}/resend-license-email", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/tenants/{id}/resend-license-email");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Diagnostics: Find tenant by license key (proxy)
app.MapGet("/api/v1/tenants/by-license/{key}", async (string key, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/tenants/by-license/{Uri.EscapeDataString(key)}");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Get subscription plans (active only)
app.MapGet("/api/v1/subscription-plans", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/subscription-plans");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Get all subscription plans (including inactive)
app.MapGet("/api/v1/subscription-plans/all", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/subscription-plans/all");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Create subscription plan
app.MapPost("/api/v1/subscription-plans", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { detail = "İstek gövdesi boş!" });
        }
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/subscription-plans")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Update subscription plan
app.MapPut("/api/v1/subscription-plans/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { detail = "İstek gövdesi boş!" });
        }
        
        var request = new HttpRequestMessage(HttpMethod.Put, $"{tenantServiceUrl}/api/subscription-plans/{id}")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Toggle subscription plan active status
app.MapPut("/api/v1/subscription-plans/{id}/toggle-active", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Put, $"{tenantServiceUrl}/api/subscription-plans/{id}/toggle-active");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Delete subscription plan
app.MapDelete("/api/v1/subscription-plans/{id}", async (int id, HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{tenantServiceUrl}/api/subscription-plans/{id}");
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Create subscription (with license key generation)
app.MapPost("/api/v1/subscriptions", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { detail = "İstek gövdesi boş!" });
        }
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/subscriptions")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Validate license key
app.MapPost("/api/v1/tenants/validate-license", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { detail = "İstek gövdesi boş!" });
        }
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/tenants/validate-license")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// =====================================================
// DASHBOARD ROUTES
// =====================================================

// Get dashboard statistics
app.MapGet("/api/v1/dashboard/stats", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        
        // Get tenantId from user claims (if available)
        var tenantId = context.User?.FindFirst("tenantId")?.Value;
        var isSuperAdmin = context.User?.IsInRole("SuperAdmin") ?? false;
        
        // Get today's date range (UTC)
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var todayEnd = DateTime.SpecifyKind(today.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        
        // ISO 8601 format for API calls
        var todayStr = today.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var todayEndStr = todayEnd.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        
        // Get transactions for today
        string transactionJson = "[]";
        try
        {
            var transactionUrl = $"{transactionServiceUrl}/api/transactions?baslangic={Uri.EscapeDataString(todayStr)}&bitis={Uri.EscapeDataString(todayEndStr)}";
            if (!string.IsNullOrEmpty(tenantId) && !isSuperAdmin)
            {
                transactionUrl += $"&tenantId={tenantId}";
            }
            
            var transactionRequest = new HttpRequestMessage(HttpMethod.Get, transactionUrl);
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                transactionRequest.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
            }
            
            var transactionResponse = await client.SendAsync(transactionRequest);
            transactionJson = await transactionResponse.Content.ReadAsStringAsync();
            
            if (!transactionResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Transaction service error ({transactionResponse.StatusCode}): {transactionJson}");
                transactionJson = "[]";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Transaction service error: {ex.Message}");
            transactionJson = "[]";
        }
        
        // Get products count
        var productUrl = $"{productServiceUrl}/api/products?aktif=true";
        if (!string.IsNullOrEmpty(tenantId) && !isSuperAdmin)
        {
            productUrl += $"&tenantId={tenantId}";
        }
        var productRequest = new HttpRequestMessage(HttpMethod.Get, productUrl);
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            productRequest.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var productResponse = await client.SendAsync(productRequest);
        var productJson = await productResponse.Content.ReadAsStringAsync();
        
        if (!productResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"Product service error ({productResponse.StatusCode}): {productJson}");
        }
        
        // Get categories count
        var categoryUrl = $"{productServiceUrl}/api/categories?aktif=true";
        if (!string.IsNullOrEmpty(tenantId) && !isSuperAdmin)
        {
            categoryUrl += $"&tenantId={tenantId}";
        }
        var categoryRequest = new HttpRequestMessage(HttpMethod.Get, categoryUrl);
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            categoryRequest.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var categoryResponse = await client.SendAsync(categoryRequest);
        var categoryJson = await categoryResponse.Content.ReadAsStringAsync();
        
        if (!categoryResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"Category service error ({categoryResponse.StatusCode}): {categoryJson}");
        }
        
        // Get expenses for today
        string expenseJson = "[]";
        try
        {
            var expenseUrl = $"{transactionServiceUrl}/api/expenses?baslangic={Uri.EscapeDataString(todayStr)}&bitis={Uri.EscapeDataString(todayEndStr)}";
            if (!string.IsNullOrEmpty(tenantId) && !isSuperAdmin)
            {
                expenseUrl += $"&tenantId={tenantId}";
            }
            var expenseRequest = new HttpRequestMessage(HttpMethod.Get, expenseUrl);
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                expenseRequest.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
            }
            
            var expenseResponse = await client.SendAsync(expenseRequest);
            expenseJson = await expenseResponse.Content.ReadAsStringAsync();
            
            if (!expenseResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Expense service error ({expenseResponse.StatusCode}): {expenseJson}");
                expenseJson = "[]";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Expense service error: {ex.Message}");
            expenseJson = "[]";
        }
        
        // Parse data
        var allTransactions = JsonSerializer.Deserialize<List<TransactionDto>>(transactionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<TransactionDto>();
        var allProducts = JsonSerializer.Deserialize<List<ProductDto>>(productJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ProductDto>();
        var allCategories = JsonSerializer.Deserialize<List<CategoryDto>>(categoryJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<CategoryDto>();
        var allExpenses = JsonSerializer.Deserialize<List<ExpenseDto>>(expenseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ExpenseDto>();
        
        // Filter by tenantId if not SuperAdmin
        var transactions = allTransactions;
        var products = allProducts;
        var categories = allCategories;
        var expenses = allExpenses;
        
        if (!string.IsNullOrEmpty(tenantId) && !isSuperAdmin && int.TryParse(tenantId, out int tenantIdInt))
        {
            transactions = allTransactions.Where(t => t.TenantId == tenantIdInt).ToList();
            products = allProducts.Where(p => p.TenantId == tenantIdInt).ToList();
            categories = allCategories.Where(c => c.TenantId == tenantIdInt).ToList();
            expenses = allExpenses.Where(e => e.TenantId == tenantIdInt).ToList();
        }
        
        // Calculate statistics
        // Satışlar: Sadece SATIS tipindeki transaction'lar
        var todaySales = transactions.Where(t => t.IslemTipi == "SATIS").Sum(t => (double)t.ToplamTutar);
        // Giderler: Expense tablosundan
        var todayExpenses = expenses.Sum(e => (double)e.Tutar);
        // Kar = Satışlar - Giderler
        var profit = todaySales - todayExpenses;
        
        // Get last 7 days sales data
        var last7Days = new List<DailySalesDto>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.SpecifyKind(today.AddDays(-i), DateTimeKind.Utc);
            var dateStart = date;
            var dateEnd = DateTime.SpecifyKind(date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            
            // ISO 8601 format for API calls
            var dateStartStr = dateStart.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var dateEndStr = dateEnd.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            
            var dayTransactionUrl = $"{transactionServiceUrl}/api/transactions?baslangic={Uri.EscapeDataString(dateStartStr)}&bitis={Uri.EscapeDataString(dateEndStr)}";
            if (!string.IsNullOrEmpty(tenantId) && !isSuperAdmin)
            {
                dayTransactionUrl += $"&tenantId={tenantId}";
            }
            
            var dayRequest = new HttpRequestMessage(HttpMethod.Get, dayTransactionUrl);
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                dayRequest.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
            }
            
            string dayJson = "[]";
            try
            {
                var dayResponse = await client.SendAsync(dayRequest);
                dayJson = await dayResponse.Content.ReadAsStringAsync();
                if (!dayResponse.IsSuccessStatusCode)
                {
                    dayJson = "[]";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Day transaction service error: {ex.Message}");
                dayJson = "[]";
            }
            var allDayTransactions = JsonSerializer.Deserialize<List<TransactionDto>>(dayJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<TransactionDto>();
            
            // Get expenses for this day
            string dayExpenseJson = "[]";
            try
            {
                var dayExpenseUrl = $"{transactionServiceUrl}/api/expenses?baslangic={Uri.EscapeDataString(dateStartStr)}&bitis={Uri.EscapeDataString(dateEndStr)}";
                if (!string.IsNullOrEmpty(tenantId) && !isSuperAdmin)
                {
                    dayExpenseUrl += $"&tenantId={tenantId}";
                }
                var dayExpenseRequest = new HttpRequestMessage(HttpMethod.Get, dayExpenseUrl);
                if (context.Request.Headers.ContainsKey("Authorization"))
                {
                    dayExpenseRequest.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
                }
                var dayExpenseResponse = await client.SendAsync(dayExpenseRequest);
                dayExpenseJson = await dayExpenseResponse.Content.ReadAsStringAsync();
                if (!dayExpenseResponse.IsSuccessStatusCode)
                {
                    dayExpenseJson = "[]";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Day expense service error: {ex.Message}");
                dayExpenseJson = "[]";
            }
            var allDayExpenses = JsonSerializer.Deserialize<List<ExpenseDto>>(dayExpenseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ExpenseDto>();
            
            // Filter by tenantId if not SuperAdmin
            var dayTransactions = allDayTransactions;
            var dayExpenses = allDayExpenses;
            if (!string.IsNullOrEmpty(tenantId) && !isSuperAdmin && int.TryParse(tenantId, out int tenantIdInt2))
            {
                dayTransactions = allDayTransactions.Where(t => t.TenantId == tenantIdInt2).ToList();
                dayExpenses = allDayExpenses.Where(e => e.TenantId == tenantIdInt2).ToList();
            }
            
            var daySales = dayTransactions.Where(t => t.IslemTipi == "SATIS").Sum(t => (double)t.ToplamTutar);
            var dayExpensesTotal = dayExpenses.Sum(e => (double)e.Tutar);
            
            // Turkish month names
            var turkishMonths = new[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };
            var monthName = turkishMonths[date.Month - 1];
            
            last7Days.Add(new DailySalesDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                DateLabel = $"{date.Day} {monthName}",
                Sales = daySales,
                Expenses = dayExpensesTotal,
                Profit = daySales - dayExpensesTotal
            });
        }
        
        var result = new
        {
            todaySales = todaySales,
            todayExpenses = todayExpenses,
            todayProfit = profit,
            totalProducts = products.Count,
            totalCategories = categories.Count,
            dailySales = last7Days
        };
        
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Dashboard stats error: {ex.Message}");
        return Results.Problem($"Dashboard istatistikleri alınırken hata: {ex.Message}", statusCode: 500);
    }
});

// =====================================================
// LICENSE DEVICE BINDING ROUTES
// =====================================================

// Create license
app.MapPost("/api/v1/licenses/create", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { detail = "İstek gövdesi boş!" });
        }
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/licenses/create")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Activate license
app.MapPost("/api/v1/licenses/activate", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { detail = "İstek gövdesi boş!" });
        }
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/licenses/activate")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }
        
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Fetch license payload by key
app.MapPost("/api/v1/licenses/by-key", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { detail = "İstek gövdesi boş!" });
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/licenses/by-key")
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };

        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
        }

        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// Get license public key
app.MapGet("/api/v1/licenses/public-key", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var response = await client.GetAsync($"{tenantServiceUrl}/api/licenses/public-key");
        var result = await response.Content.ReadAsStringAsync();
        
        return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Gateway error: {ex.Message}");
        return Results.Problem($"Beklenmeyen hata: {ex.Message}", statusCode: 500);
    }
});

// =====================================================
// ADMIN - PAYMENTS REPORTING (SuperAdmin)
// =====================================================
app.MapGet("/api/v1/admin/payments/summary", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/admin/payments/summary");
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapGet("/api/v1/admin/payments/trend", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    var request = new HttpRequestMessage(HttpMethod.Get, $"{tenantServiceUrl}/api/admin/payments/trend{queryString}");
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

// Monthly subscription renewal proxy
app.MapPost("/api/v1/subscriptions/renew", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    var request = new HttpRequestMessage(HttpMethod.Post, $"{tenantServiceUrl}/api/subscriptions/renew")
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };
    if (context.Request.Headers.ContainsKey("Authorization"))
    {
        request.Headers.Add("Authorization", context.Request.Headers["Authorization"].ToString());
    }
    var response = await client.SendAsync(request);
    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
});

app.MapControllers();

// SignalR Hub
app.MapHub<LogHub>("/hub/logs");

// Log endpoint - servislerden log göndermek için
app.MapPost("/api/v1/logs", async (HttpContext context, IHubContext<LogHub> hubContext) =>
{
    try
    {
        // Enable buffering to allow body to be read multiple times
        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { detail = "İstek gövdesi boş!" });
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        
        LogData? logData = null;
        try
        {
            logData = JsonSerializer.Deserialize<LogData>(body, options);
        }
        catch (JsonException jsonEx)
        {
            Console.WriteLine($"JSON parse error: {jsonEx.Message}, Body: {body?.Substring(0, Math.Min(200, body?.Length ?? 0))}");
            return Results.BadRequest(new { detail = $"Geçersiz JSON formatı: {jsonEx.Message}" });
        }
        
        if (logData == null)
        {
            return Results.BadRequest(new { detail = "Geçersiz log formatı!" });
        }

        // Property'leri al (case-insensitive deserialization zaten aktif)
        var serviceName = logData.ServiceName ?? "Unknown";
        var level = logData.Level ?? "Information";
        var message = logData.Message ?? "";
        var exception = logData.Exception;

        // Debug için log yaz (sadece ilk 50 karakter)
        var messagePreview = message?.Length > 50 ? message.Substring(0, 50) + "..." : message ?? "";
        Console.WriteLine($"Received log: ServiceName={serviceName}, Level={level}, Message={messagePreview}");

        // SignalR ile tüm bağlı client'lara gönder
        try
        {
            await hubContext.Clients.All.SendAsync("logReceived", new
            {
                serviceName = serviceName,
                level = level,
                message = message ?? "",
                exception = exception,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception signalREx)
        {
            Console.WriteLine($"SignalR error: {signalREx.Message}");
            // SignalR hatası olsa bile 200 döndür (log kaydedildi sayılır)
        }

        return Results.Ok(new { ok = true });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Log endpoint error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem($"Log kaydedilirken hata: {ex.Message}", statusCode: 500);
    }
});

// Start the application (URL is set via ASPNETCORE_URLS or launchSettings.json)
app.Run();

// Log Data Model
public class LogData
{
    public string? ServiceName { get; set; }
    public string? Level { get; set; }
    public string? Message { get; set; }
    public string? Exception { get; set; }
}

// Helper DTOs
public class TransactionDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string IslemTipi { get; set; } = "";
    public decimal ToplamTutar { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string UrunAdi { get; set; } = "";
    public bool Aktif { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string KategoriAdi { get; set; } = "";
    public bool Aktif { get; set; }
}

public class DailySalesDto
{
    public string Date { get; set; } = "";
    public string DateLabel { get; set; } = "";
    public double Sales { get; set; }
    public double Expenses { get; set; }
    public double Profit { get; set; }
}

public class ExpenseDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public string GiderAdi { get; set; } = "";
    public decimal Tutar { get; set; }
    public string? Kategori { get; set; }
    public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
}


