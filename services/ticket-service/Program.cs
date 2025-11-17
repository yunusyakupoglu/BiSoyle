using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BiSoyle.Ticket.Service.Data;
using System.Linq;
using System.Net.Http;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BiSoyle - Ticket Service API",
        Version = "v1",
        Description = "Multi-Tenant Ticket Management API<br/><strong>BY HOSTEAGLE INFORMATION TECHNOLOGIES</strong>",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HostEagle Information Technologies",
            Email = "info@hosteagle.com"
        }
    });
});

// Database
builder.Services.AddDbContext<TicketDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// HttpClient for calling Tenant Service
builder.Services.AddHttpClient();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:4200", "http://localhost:3000", "http://localhost:4300", "http://127.0.0.1:4200", "http://127.0.0.1:4300")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(',') 
                ?? new[] { "http://localhost:4200" };
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// JSON Serializer Options for Minimal APIs
var jsonOptions = new System.Text.Json.JsonSerializerOptions
{
    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
    WriteIndented = true
};

var app = builder.Build();

// Set port for Ticket Service (5005)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5005";
app.Urls.Add($"http://localhost:{port}");

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration error: {ex.Message}");
    }
    
    // Tables oluştur (eğer yoksa)
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
                    CREATE TABLE IF NOT EXISTS tickets (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INTEGER NOT NULL,
                        ""CreatedByUserId"" INTEGER NOT NULL,
                        ""Title"" VARCHAR(200) NOT NULL,
                        ""Description"" VARCHAR(2000) NOT NULL,
                        ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Open',
                        ""Priority"" VARCHAR(50) NOT NULL DEFAULT 'Medium',
                        ""IsStarred"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""IsImportant"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""ResolvedDate"" TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_tickets_TenantId"" ON tickets (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_tickets_CreatedByUserId"" ON tickets (""CreatedByUserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_tickets_Status"" ON tickets (""Status"");
                    CREATE INDEX IF NOT EXISTS ""IX_tickets_CreatedDate"" ON tickets (""CreatedDate"");
                    
                    CREATE TABLE IF NOT EXISTS ticket_comments (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TicketId"" INTEGER NOT NULL,
                        ""UserId"" INTEGER NOT NULL,
                        ""Comment"" VARCHAR(2000) NOT NULL,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT ""FK_ticket_comments_tickets"" FOREIGN KEY (""TicketId"") REFERENCES tickets(""Id"") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_ticket_comments_TicketId"" ON ticket_comments (""TicketId"");
                    CREATE INDEX IF NOT EXISTS ""IX_ticket_comments_UserId"" ON ticket_comments (""UserId"");
                    
                    CREATE TABLE IF NOT EXISTS ticket_attachments (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TicketId"" INTEGER NOT NULL,
                        ""CommentId"" INTEGER,
                        ""FileName"" VARCHAR(255) NOT NULL,
                        ""FilePath"" VARCHAR(500) NOT NULL,
                        ""FileType"" VARCHAR(50),
                        ""FileSize"" BIGINT NOT NULL,
                        ""UploadedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT ""FK_ticket_attachments_tickets"" FOREIGN KEY (""TicketId"") REFERENCES tickets(""Id"") ON DELETE CASCADE,
                        CONSTRAINT ""FK_ticket_attachments_comments"" FOREIGN KEY (""CommentId"") REFERENCES ticket_comments(""Id"") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_ticket_attachments_TicketId"" ON ticket_attachments (""TicketId"");
                    CREATE INDEX IF NOT EXISTS ""IX_ticket_attachments_CommentId"" ON ticket_attachments (""CommentId"");
                    
                    CREATE TABLE IF NOT EXISTS notifications (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INTEGER NOT NULL,
                        ""UserId"" INTEGER NOT NULL,
                        ""Type"" VARCHAR(50) NOT NULL,
                        ""Title"" VARCHAR(200) NOT NULL,
                        ""Message"" VARCHAR(500),
                        ""RelatedTicketId"" INTEGER,
                        ""IsRead"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_UserId"" ON notifications (""UserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_TenantId"" ON notifications (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_RelatedTicketId"" ON notifications (""RelatedTicketId"");
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_IsRead"" ON notifications (""IsRead"");
                ";
                command.ExecuteNonQuery();
                Console.WriteLine("Ticket tables created successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ticket tables creation error (may already exist): {ex.Message}");
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
        Console.WriteLine($"Ticket tables setup error: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// File uploads directory
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// =====================================================
// TICKET ENDPOINTS
// =====================================================

// Get all tickets
app.MapGet("/api/tickets", async (TicketDbContext dbContext, int? tenantId, int? createdByUserId, string? status, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        Console.WriteLine($"Ticket Service: GET /api/tickets - tenantId={tenantId}, createdByUserId={createdByUserId}, status={status}");
        
        var query = dbContext.Tickets.AsQueryable();
        
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);
        
        if (createdByUserId.HasValue)
            query = query.Where(t => t.CreatedByUserId == createdByUserId.Value);
        
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);
        
        var tickets = await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
        Console.WriteLine($"Ticket Service: Found {tickets.Count} tickets");
        
        // Tenant bilgilerini al (eğer tenant ID'leri varsa)
        var tenantIds = tickets.Where(t => t.TenantId > 0).Select(t => t.TenantId).Distinct().ToList();
        var tenantNames = new Dictionary<int, string>();
        
        if (tenantIds.Any())
        {
            try
            {
                var tenantServiceUrl = Environment.GetEnvironmentVariable("TENANT_SERVICE_URL") ?? "http://localhost:5006";
                var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                foreach (var tid in tenantIds)
                {
                    try
                    {
                        var response = await client.GetAsync($"{tenantServiceUrl}/api/tenants/{tid}");
                        if (response.IsSuccessStatusCode)
                        {
                            var tenantJson = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Tenant {tid} response: {tenantJson}");
                            var tenantDoc = System.Text.Json.JsonDocument.Parse(tenantJson);
                            
                            // Önce "FirmaAdi" field'ını dene (Tenant Service'in döndürdüğü field)
                            if (tenantDoc.RootElement.TryGetProperty("firmaAdi", out var firmaAdiElement))
                            {
                                tenantNames[tid] = firmaAdiElement.GetString() ?? $"Tenant {tid}";
                                Console.WriteLine($"Tenant {tid} firmaAdi found: {tenantNames[tid]}");
                            }
                            // Sonra "FirmaAdi" (PascalCase) field'ını dene
                            else if (tenantDoc.RootElement.TryGetProperty("FirmaAdi", out var FirmaAdiElement))
                            {
                                tenantNames[tid] = FirmaAdiElement.GetString() ?? $"Tenant {tid}";
                                Console.WriteLine($"Tenant {tid} FirmaAdi found: {tenantNames[tid]}");
                            }
                            // Sonra "name" field'ını dene (camelCase)
                            else if (tenantDoc.RootElement.TryGetProperty("name", out var nameElement))
                            {
                                tenantNames[tid] = nameElement.GetString() ?? $"Tenant {tid}";
                                Console.WriteLine($"Tenant {tid} name found: {tenantNames[tid]}");
                            }
                            // Sonra "Name" field'ını dene (PascalCase)
                            else if (tenantDoc.RootElement.TryGetProperty("Name", out var nameElement2))
                            {
                                tenantNames[tid] = nameElement2.GetString() ?? $"Tenant {tid}";
                                Console.WriteLine($"Tenant {tid} Name found: {tenantNames[tid]}");
                            }
                            // Sonra "companyName" field'ını dene
                            else if (tenantDoc.RootElement.TryGetProperty("companyName", out var companyNameElement))
                            {
                                tenantNames[tid] = companyNameElement.GetString() ?? $"Tenant {tid}";
                                Console.WriteLine($"Tenant {tid} companyName found: {tenantNames[tid]}");
                            }
                            else
                            {
                                Console.WriteLine($"Tenant {tid} - name field not found. Available properties:");
                                foreach (var prop in tenantDoc.RootElement.EnumerateObject())
                                {
                                    Console.WriteLine($"  - {prop.Name}");
                                }
                                tenantNames[tid] = $"Tenant {tid}";
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Tenant {tid} request failed with status: {response.StatusCode}");
                            tenantNames[tid] = $"Tenant {tid}";
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Tenant {tid} bilgisi alınamadı: {ex.Message}");
                        tenantNames[tid] = $"Tenant {tid}";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tenant bilgileri alınırken hata: {ex.Message}");
            }
        }
        
        // Circular reference'ı önlemek için anonim objelere dönüştür
        var ticketDtos = tickets.Select(t => new
        {
            t.Id,
            t.TenantId,
            TenantName = t.TenantId > 0 && tenantNames.ContainsKey(t.TenantId) ? tenantNames[t.TenantId] : (t.TenantId == 0 ? "SuperAdmin" : $"Tenant {t.TenantId}"),
            t.CreatedByUserId,
            t.Title,
            t.Description,
            t.Status,
            t.Priority,
            t.IsStarred,
            t.IsImportant,
            t.CreatedDate,
            t.UpdatedDate,
            t.ResolvedDate
        }).ToList();
        
        Console.WriteLine($"Ticket Service: Returning {ticketDtos.Count} ticket DTOs");
        if (ticketDtos.Any())
        {
            var firstTicket = ticketDtos.First();
            Console.WriteLine($"First ticket - ID: {firstTicket.Id}, TenantId: {firstTicket.TenantId}, TenantName: {firstTicket.TenantName}");
        }
        Console.WriteLine($"Tenant names dictionary count: {tenantNames.Count}");
        foreach (var kvp in tenantNames)
        {
            Console.WriteLine($"  Tenant {kvp.Key}: {kvp.Value}");
        }
        return Results.Ok(ticketDtos);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Tickets get error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        return Results.Problem($"Ticket'lar getirme hatası: {ex.Message}");
    }
});

// Get ticket by ID
app.MapGet("/api/tickets/{id}", async (int id, TicketDbContext dbContext, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var ticket = await dbContext.Tickets
            .Include(t => t.Comments)
                .ThenInclude(c => c.Attachments)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (ticket == null)
            return Results.NotFound(new { message = "Ticket bulunamadı" });
        
        // Tenant adını al
        string tenantName = "SuperAdmin";
        if (ticket.TenantId > 0)
        {
            try
            {
                var tenantServiceUrl = Environment.GetEnvironmentVariable("TENANT_SERVICE_URL") ?? "http://localhost:5006";
                var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                var response = await client.GetAsync($"{tenantServiceUrl}/api/tenants/{ticket.TenantId}");
                if (response.IsSuccessStatusCode)
                {
                    var tenantJson = await response.Content.ReadAsStringAsync();
                    var tenantDoc = System.Text.Json.JsonDocument.Parse(tenantJson);
                    // Önce "FirmaAdi" field'ını dene (Tenant Service'in döndürdüğü field)
                    if (tenantDoc.RootElement.TryGetProperty("firmaAdi", out var firmaAdiElement))
                    {
                        tenantName = firmaAdiElement.GetString() ?? $"Tenant {ticket.TenantId}";
                    }
                    else if (tenantDoc.RootElement.TryGetProperty("FirmaAdi", out var FirmaAdiElement))
                    {
                        tenantName = FirmaAdiElement.GetString() ?? $"Tenant {ticket.TenantId}";
                    }
                    else if (tenantDoc.RootElement.TryGetProperty("name", out var nameElement))
                    {
                        tenantName = nameElement.GetString() ?? $"Tenant {ticket.TenantId}";
                    }
                    else if (tenantDoc.RootElement.TryGetProperty("Name", out var NameElement))
                    {
                        tenantName = NameElement.GetString() ?? $"Tenant {ticket.TenantId}";
                    }
                    else if (tenantDoc.RootElement.TryGetProperty("companyName", out var companyNameElement))
                    {
                        tenantName = companyNameElement.GetString() ?? $"Tenant {ticket.TenantId}";
                    }
                    else
                    {
                        tenantName = $"Tenant {ticket.TenantId}";
                    }
                }
                else
                {
                    tenantName = $"Tenant {ticket.TenantId}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tenant bilgisi alınamadı: {ex.Message}");
                tenantName = $"Tenant {ticket.TenantId}";
            }
        }
        
            // Yorumlardaki kullanıcı adlarını al
            var userIds = ticket.Comments?.Select(c => c.UserId).Distinct().ToList() ?? new List<int>();
            var userNames = new Dictionary<int, string>();
            
            if (userIds.Any())
            {
                try
                {
                    var userServiceUrl = Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? "http://localhost:5004";
                    var client = httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    
                    foreach (var uid in userIds)
                    {
                        try
                        {
                            var response = await client.GetAsync($"{userServiceUrl}/api/users/internal/{uid}");
                            if (response.IsSuccessStatusCode)
                            {
                                var userJson = await response.Content.ReadAsStringAsync();
                                var userDoc = System.Text.Json.JsonDocument.Parse(userJson);
                                
                                string userName = $"User {uid}";
                                if (userDoc.RootElement.TryGetProperty("firstName", out var firstNameElement) && 
                                    userDoc.RootElement.TryGetProperty("lastName", out var lastNameElement))
                                {
                                    var firstName = firstNameElement.GetString() ?? "";
                                    var lastName = lastNameElement.GetString() ?? "";
                                    userName = $"{firstName} {lastName}".Trim();
                                    if (string.IsNullOrEmpty(userName))
                                    {
                                        // firstName ve lastName boşsa username'i kullan
                                        if (userDoc.RootElement.TryGetProperty("username", out var usernameElement))
                                        {
                                            userName = usernameElement.GetString() ?? $"User {uid}";
                                        }
                                    }
                                }
                                else if (userDoc.RootElement.TryGetProperty("username", out var usernameElement2))
                                {
                                    userName = usernameElement2.GetString() ?? $"User {uid}";
                                }
                                
                                userNames[uid] = userName;
                            }
                            else
                            {
                                userNames[uid] = $"User {uid}";
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"User {uid} bilgisi alınamadı: {ex.Message}");
                            userNames[uid] = $"User {uid}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Kullanıcı bilgileri alınırken hata: {ex.Message}");
                }
            }
            
            // Circular reference'ı önlemek için anonim objeye dönüştür
            var ticketDto = new
            {
                ticket.Id,
                ticket.TenantId,
                TenantName = tenantName,
                ticket.CreatedByUserId,
                ticket.Title,
                ticket.Description,
                ticket.Status,
                ticket.Priority,
                ticket.IsStarred,
                ticket.IsImportant,
                ticket.CreatedDate,
                ticket.UpdatedDate,
                ticket.ResolvedDate,
                Comments = ticket.Comments?.Select(c => new
                {
                    c.Id,
                    c.TicketId,
                    c.UserId,
                    UserName = userNames.ContainsKey(c.UserId) ? userNames[c.UserId] : $"User {c.UserId}",
                    c.Comment,
                    c.CreatedDate,
                    Attachments = c.Attachments?.Select(a => new
                    {
                        a.Id,
                        a.TicketId,
                        a.CommentId,
                        a.FileName,
                        a.FilePath,
                        a.FileType,
                        a.FileSize,
                        a.UploadedDate
                    }).ToList()
                }).ToList(),
                Attachments = ticket.Attachments?.Select(a => new
                {
                    a.Id,
                    a.TicketId,
                    a.CommentId,
                    a.FileName,
                    a.FilePath,
                    a.FileType,
                    a.FileSize,
                    a.UploadedDate
                }).ToList()
            };
        
        return Results.Ok(ticketDto);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ticket get error: {ex.Message}");
        return Results.Problem($"Ticket getirme hatası: {ex.Message}");
    }
});

// Create ticket (Admin only)
app.MapPost("/api/tickets", async (HttpContext context, TicketDbContext dbContext, IServiceProvider serviceProvider) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var ticket = System.Text.Json.JsonSerializer.Deserialize<BiSoyle.Ticket.Service.Data.Ticket>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (ticket == null)
            return Results.BadRequest(new { detail = "Geçersiz ticket verisi" });
        
        if (string.IsNullOrWhiteSpace(ticket.Title))
            return Results.BadRequest(new { detail = "Ticket başlığı gereklidir" });
        
        if (string.IsNullOrWhiteSpace(ticket.Description))
            return Results.BadRequest(new { detail = "Ticket açıklaması gereklidir" });
        
        if (ticket.TenantId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz TenantId" });
        
        if (ticket.CreatedByUserId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz CreatedByUserId" });
        
        ticket.Status = "Open";
        ticket.CreatedDate = DateTime.UtcNow;
        ticket.UpdatedDate = DateTime.UtcNow;
        
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();
        
        Console.WriteLine($"Ticket Service: Created ticket with ID {ticket.Id}");
        
        // SuperAdmin'lere bildirim gönder
        _ = Task.Run(async () =>
        {
            try
            {
                await SendTicketCreatedNotifications(ticket, serviceProvider);
            }
            catch (Exception notifEx)
            {
                Console.WriteLine($"Notification creation error: {notifEx.Message}");
            }
        });
        
        // Circular reference'ı önlemek için anonim objeye dönüştür
        var ticketDto = new
        {
            ticket.Id,
            ticket.TenantId,
            ticket.CreatedByUserId,
            ticket.Title,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            ticket.CreatedDate,
            ticket.UpdatedDate,
            ticket.ResolvedDate
        };
        
        return Results.Created($"/api/tickets/{ticket.Id}", ticketDto);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ticket save error: {ex.Message}");
        return Results.Problem($"Ticket kaydetme hatası: {ex.Message}");
    }
});

// Update ticket
app.MapPut("/api/tickets/{id}", async (int id, HttpContext context, TicketDbContext dbContext) =>
{
    try
    {
        var existingTicket = await dbContext.Tickets.FindAsync(id);
        if (existingTicket == null)
            return Results.NotFound(new { message = "Ticket bulunamadı" });
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var ticket = System.Text.Json.JsonSerializer.Deserialize<BiSoyle.Ticket.Service.Data.Ticket>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (ticket == null)
            return Results.BadRequest(new { detail = "Geçersiz ticket verisi" });
        
        existingTicket.Title = ticket.Title;
        existingTicket.Description = ticket.Description;
        existingTicket.Status = ticket.Status;
        existingTicket.Priority = ticket.Priority;
        // IsStarred ve IsImportant alanlarını güncelle (eğer gönderilmişse)
        existingTicket.IsStarred = ticket.IsStarred;
        existingTicket.IsImportant = ticket.IsImportant;
        existingTicket.UpdatedDate = DateTime.UtcNow;
        
        if (ticket.Status == "Resolved" || ticket.Status == "Closed")
        {
            existingTicket.ResolvedDate = DateTime.UtcNow;
        }
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(existingTicket);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ticket update error: {ex.Message}");
        return Results.Problem($"Ticket güncelleme hatası: {ex.Message}");
    }
});

// Delete ticket
app.MapDelete("/api/tickets/{id}", async (int id, TicketDbContext dbContext) =>
{
    var ticket = await dbContext.Tickets.FindAsync(id);
    if (ticket == null)
        return Results.NotFound(new { message = "Ticket bulunamadı" });
    
    dbContext.Tickets.Remove(ticket);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Ticket silindi" });
});

// =====================================================
// TICKET COMMENT ENDPOINTS
// =====================================================

// Get ticket comments
app.MapGet("/api/tickets/{ticketId}/comments", async (int ticketId, TicketDbContext dbContext) =>
{
    try
    {
        var comments = await dbContext.TicketComments
            .Include(c => c.Attachments)
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedDate)
            .ToListAsync();
        
        // Circular reference'ı önlemek için anonim objelere dönüştür
        var commentDtos = comments.Select(c => new
        {
            c.Id,
            c.TicketId,
            c.UserId,
            c.Comment,
            c.CreatedDate,
            Attachments = c.Attachments?.Select(a => new
            {
                a.Id,
                a.TicketId,
                a.CommentId,
                a.FileName,
                a.FilePath,
                a.FileType,
                a.FileSize,
                a.UploadedDate
            }).ToList()
        }).ToList();
        
        return Results.Ok(commentDtos);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Comments get error: {ex.Message}");
        return Results.Problem($"Yorumlar getirme hatası: {ex.Message}");
    }
});

// Add comment to ticket
app.MapPost("/api/tickets/{ticketId}/comments", async (int ticketId, HttpContext context, TicketDbContext dbContext, IServiceProvider serviceProvider) =>
{
    try
    {
        Console.WriteLine($"Ticket Service: POST /api/tickets/{ticketId}/comments");
        
        var ticket = await dbContext.Tickets.FindAsync(ticketId);
        if (ticket == null)
        {
            Console.WriteLine($"Ticket Service: Ticket {ticketId} not found");
            return Results.NotFound(new { message = "Ticket bulunamadı" });
        }
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Console.WriteLine($"Ticket Service: Request body: {body}");
        
        var commentData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (commentData == null)
        {
            Console.WriteLine("Ticket Service: commentData is null");
            return Results.BadRequest(new { detail = "Geçersiz veri" });
        }
        
        if (!commentData.ContainsKey("comment"))
        {
            Console.WriteLine("Ticket Service: comment key not found");
            return Results.BadRequest(new { detail = "comment gereklidir" });
        }
        
        if (!commentData.ContainsKey("userId"))
        {
            Console.WriteLine("Ticket Service: userId key not found");
            return Results.BadRequest(new { detail = "userId gereklidir" });
        }
        
        // userId'yi güvenli şekilde parse et
        int userId = 0;
        try
        {
            var userIdValue = commentData["userId"];
            if (userIdValue == null)
            {
                Console.WriteLine("Ticket Service: userId is null");
                return Results.BadRequest(new { detail = "userId gereklidir" });
            }
            
            if (userIdValue is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    userId = jsonElement.GetInt32();
                }
                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var userIdStr = jsonElement.GetString();
                    if (!int.TryParse(userIdStr, out userId))
                    {
                        Console.WriteLine($"Ticket Service: userId parse failed: {userIdStr}");
                        return Results.BadRequest(new { detail = "Geçersiz userId formatı" });
                    }
                }
                else
                {
                    Console.WriteLine($"Ticket Service: userId unexpected type: {jsonElement.ValueKind}");
                    return Results.BadRequest(new { detail = "Geçersiz userId formatı" });
                }
            }
            else
            {
                var userIdStr = userIdValue?.ToString();
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out userId))
                {
                    Console.WriteLine($"Ticket Service: userId parse failed: {userIdStr}");
                    return Results.BadRequest(new { detail = "Geçersiz userId formatı" });
                }
            }
        }
        catch (Exception parseEx)
        {
            Console.WriteLine($"Ticket Service: userId parse error: {parseEx.Message}");
            Console.WriteLine($"Ticket Service: Stack trace: {parseEx.StackTrace}");
            return Results.BadRequest(new { detail = $"Geçersiz userId formatı: {parseEx.Message}" });
        }
        
        if (userId <= 0)
        {
            Console.WriteLine($"Ticket Service: Invalid userId: {userId}");
            return Results.BadRequest(new { detail = "Geçersiz userId (0'dan büyük olmalı)" });
        }
        
        // comment'i güvenli şekilde al
        string commentText = "";
        try
        {
            var commentValue = commentData["comment"];
            if (commentValue == null)
            {
                Console.WriteLine("Ticket Service: comment is null");
                return Results.BadRequest(new { detail = "comment gereklidir" });
            }
            
            if (commentValue is System.Text.Json.JsonElement commentElement)
            {
                if (commentElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    commentText = commentElement.GetString() ?? "";
                }
                else
                {
                    commentText = commentElement.ToString();
                }
            }
            else
            {
                commentText = commentValue?.ToString() ?? "";
            }
        }
        catch (Exception commentEx)
        {
            Console.WriteLine($"Ticket Service: comment parse error: {commentEx.Message}");
            Console.WriteLine($"Ticket Service: Stack trace: {commentEx.StackTrace}");
            return Results.BadRequest(new { detail = $"Geçersiz comment formatı: {commentEx.Message}" });
        }
        
        if (string.IsNullOrWhiteSpace(commentText))
        {
            Console.WriteLine("Ticket Service: comment is empty");
            return Results.BadRequest(new { detail = "comment boş olamaz" });
        }
        
        var comment = new BiSoyle.Ticket.Service.Data.TicketComment
        {
            TicketId = ticketId,
            UserId = userId,
            Comment = commentText,
            CreatedDate = DateTime.UtcNow
        };
        
        dbContext.TicketComments.Add(comment);
        
        // Ticket'ı güncelle
        ticket.UpdatedDate = DateTime.UtcNow;
        // Eğer ticket kapalıysa, yorum yapıldığında tekrar açık yap
        if (ticket.Status == "Closed")
        {
            ticket.Status = "Open";
        }
        else if (ticket.Status == "Open")
        {
            ticket.Status = "InProgress";
        }
        
        await dbContext.SaveChangesAsync();
        
        Console.WriteLine($"Ticket Service: Comment {comment.Id} created successfully");
        
        // Response'u hazırla - önce response'u hazırla, sonra notification gönder
        var response = new
        {
            id = comment.Id,
            ticketId = comment.TicketId,
            userId = comment.UserId,
            comment = comment.Comment,
            createdDate = comment.CreatedDate
        };
        
        // Response'u önce döndür, notification'ı arka planda gönder
        var result = Results.Created($"/api/tickets/{ticketId}/comments/{comment.Id}", response);
        
        // Yorum yapan SuperAdmin ise ticket sahibine, değilse SuperAdmin'lere bildirim gönder
        // Notification'ı arka planda gönder, response'u bekleme
        _ = Task.Run(async () =>
        {
            try
            {
                await SendTicketCommentNotifications(ticket, comment, serviceProvider);
            }
            catch (Exception notifEx)
            {
                Console.WriteLine($"Notification creation error: {notifEx.Message}");
                Console.WriteLine($"Notification error stack trace: {notifEx.StackTrace}");
            }
        });
        
        // Response'u döndür - notification hatası response'u etkilemesin
        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Comment save error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        return Results.Problem($"Yorum kaydetme hatası: {ex.Message}");
    }
});

// Delete comment
app.MapDelete("/api/tickets/{ticketId}/comments/{commentId}", async (int ticketId, int commentId, TicketDbContext dbContext) =>
{
    var comment = await dbContext.TicketComments.FindAsync(commentId);
    if (comment == null || comment.TicketId != ticketId)
        return Results.NotFound(new { message = "Yorum bulunamadı" });
    
    dbContext.TicketComments.Remove(comment);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Yorum silindi" });
});

// =====================================================
// FILE UPLOAD ENDPOINTS
// =====================================================

// Upload file for ticket (multiple files support)
app.MapPost("/api/tickets/{ticketId}/upload", async (int ticketId, HttpContext context, TicketDbContext dbContext) =>
{
    try
    {
        Console.WriteLine($"Ticket Service: File upload request for ticket {ticketId}");
        
        var ticket = await dbContext.Tickets.FindAsync(ticketId);
        if (ticket == null)
        {
            Console.WriteLine($"Ticket Service: Ticket {ticketId} not found");
            return Results.NotFound(new { message = "Ticket bulunamadı" });
        }
        
        if (!context.Request.HasFormContentType)
        {
            Console.WriteLine($"Ticket Service: Request does not have form content type");
            return Results.BadRequest(new { detail = "Form data bekleniyor" });
        }
        
        Console.WriteLine($"Ticket Service: Reading form data");
        var form = await context.Request.ReadFormAsync();
        var files = form.Files;
        
        Console.WriteLine($"Ticket Service: Received {files?.Count ?? 0} files");
        if (files == null || files.Count == 0)
        {
            Console.WriteLine($"Ticket Service: No files in request");
            return Results.BadRequest(new { detail = "Dosya yüklenmedi" });
        }
        
        var maxFileSize = builder.Configuration.GetValue<long>("FileUpload:MaxFileSize", 10485760); // 10MB default
        var allowedExtensions = builder.Configuration["FileUpload:AllowedExtensions"]?.Split(',') 
            ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
        
        var uploadedAttachments = new List<object>();
        
        // Ensure uploads directory exists
        Directory.CreateDirectory(uploadsPath);
        
        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
                continue;
            
            if (file.Length > maxFileSize)
            {
                Console.WriteLine($"File {file.FileName} exceeds max size: {file.Length}");
                continue; // Skip this file but continue with others
            }
            
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                Console.WriteLine($"File {file.FileName} has invalid extension: {fileExtension}");
                continue; // Skip this file but continue with others
            }
            
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            var attachment = new BiSoyle.Ticket.Service.Data.TicketAttachment
            {
                TicketId = ticketId,
                CommentId = null,
                FileName = file.FileName,
                FilePath = filePath,
                FileType = file.ContentType ?? fileExtension,
                FileSize = file.Length,
                UploadedDate = DateTime.UtcNow
            };
            
            dbContext.TicketAttachments.Add(attachment);
            uploadedAttachments.Add(new
            {
                attachment.Id,
                attachment.TicketId,
                attachment.CommentId,
                attachment.FileName,
                attachment.FilePath,
                attachment.FileType,
                attachment.FileSize,
                attachment.UploadedDate
            });
        }
        
        await dbContext.SaveChangesAsync();
        
        Console.WriteLine($"Ticket Service: Uploaded {uploadedAttachments.Count} files for ticket {ticketId}");
        
        return Results.Ok(new { attachments = uploadedAttachments });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"File upload error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        return Results.Problem($"Dosya yükleme hatası: {ex.Message}");
    }
});

// Upload file for comment (multiple files support)
app.MapPost("/api/tickets/{ticketId}/comments/{commentId}/upload", async (int ticketId, int commentId, HttpContext context, TicketDbContext dbContext) =>
{
    try
    {
        Console.WriteLine($"Ticket Service: File upload request for ticket {ticketId}, comment {commentId}");
        
        var comment = await dbContext.TicketComments.FindAsync(commentId);
        if (comment == null || comment.TicketId != ticketId)
        {
            Console.WriteLine($"Ticket Service: Comment {commentId} not found or doesn't belong to ticket {ticketId}");
            return Results.NotFound(new { message = "Yorum bulunamadı" });
        }
        
        Console.WriteLine($"Ticket Service: Comment found, checking form content type");
        if (!context.Request.HasFormContentType)
        {
            Console.WriteLine($"Ticket Service: Request does not have form content type");
            return Results.BadRequest(new { detail = "Form data bekleniyor" });
        }
        
        Console.WriteLine($"Ticket Service: Reading form data");
        var form = await context.Request.ReadFormAsync();
        var files = form.Files;
        
        Console.WriteLine($"Ticket Service: Received {files?.Count ?? 0} files");
        if (files == null || files.Count == 0)
        {
            Console.WriteLine($"Ticket Service: No files in request");
            return Results.BadRequest(new { detail = "Dosya yüklenmedi" });
        }
        
        var maxFileSize = builder.Configuration.GetValue<long>("FileUpload:MaxFileSize", 10485760); // 10MB default
        var allowedExtensions = builder.Configuration["FileUpload:AllowedExtensions"]?.Split(',') 
            ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
        
        var uploadedAttachments = new List<object>();
        
        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
                continue;
            
            if (file.Length > maxFileSize)
            {
                Console.WriteLine($"File {file.FileName} exceeds max size: {file.Length}");
                continue; // Skip this file but continue with others
            }
            
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                Console.WriteLine($"File {file.FileName} has invalid extension: {fileExtension}");
                continue; // Skip this file but continue with others
            }
            
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);
            
            // Ensure uploads directory exists
            Directory.CreateDirectory(uploadsPath);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            var attachment = new BiSoyle.Ticket.Service.Data.TicketAttachment
            {
                TicketId = ticketId,
                CommentId = commentId,
                FileName = file.FileName,
                FilePath = filePath,
                FileType = file.ContentType ?? fileExtension,
                FileSize = file.Length,
                UploadedDate = DateTime.UtcNow
            };
            
            dbContext.TicketAttachments.Add(attachment);
            uploadedAttachments.Add(new
            {
                attachment.Id,
                attachment.TicketId,
                attachment.CommentId,
                attachment.FileName,
                attachment.FilePath,
                attachment.FileType,
                attachment.FileSize,
                attachment.UploadedDate
            });
        }
        
        await dbContext.SaveChangesAsync();
        
        Console.WriteLine($"Ticket Service: Uploaded {uploadedAttachments.Count} files for comment {commentId}");
        
        return Results.Ok(new { attachments = uploadedAttachments });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"File upload error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        return Results.Problem($"Dosya yükleme hatası: {ex.Message}");
    }
});

// Download file
app.MapGet("/api/tickets/attachments/{id}/download", async (int id, TicketDbContext dbContext) =>
{
    var attachment = await dbContext.TicketAttachments.FindAsync(id);
    if (attachment == null || !File.Exists(attachment.FilePath))
        return Results.NotFound(new { message = "Dosya bulunamadı" });
    
    var fileBytes = await File.ReadAllBytesAsync(attachment.FilePath);
    return Results.File(fileBytes, "application/octet-stream", attachment.FileName);
});

// =====================================================
// NOTIFICATION ENDPOINTS
// =====================================================

// Get notifications
app.MapGet("/api/notifications", async (TicketDbContext dbContext, int userId, int? tenantId) =>
{
    try
    {
        var query = dbContext.Notifications.AsQueryable();
        
        query = query.Where(n => n.UserId == userId);
        
        if (tenantId.HasValue)
            query = query.Where(n => n.TenantId == tenantId.Value);
        
        var notifications = await query.OrderByDescending(n => n.CreatedDate).ToListAsync();
        return Results.Ok(notifications);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Notifications get error: {ex.Message}");
        return Results.Problem($"Bildirimler getirme hatası: {ex.Message}");
    }
});

// Get unread count
app.MapGet("/api/notifications/unread-count", async (TicketDbContext dbContext, int userId, int? tenantId) =>
{
    try
    {
        var query = dbContext.Notifications.Where(n => n.UserId == userId && !n.IsRead);
        
        if (tenantId.HasValue)
            query = query.Where(n => n.TenantId == tenantId.Value);
        
        var count = await query.CountAsync();
        return Results.Ok(new { count });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unread count error: {ex.Message}");
        return Results.Problem($"Okunmamış bildirim sayısı getirme hatası: {ex.Message}");
    }
});

// Mark as read
app.MapPatch("/api/notifications/{id}/read", async (int id, TicketDbContext dbContext) =>
{
    var notification = await dbContext.Notifications.FindAsync(id);
    if (notification == null)
        return Results.NotFound(new { message = "Bildirim bulunamadı" });
    
    notification.IsRead = true;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(notification);
});

// Mark all as read
app.MapPatch("/api/notifications/read-all", async (HttpContext context, TicketDbContext dbContext) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var request = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (request == null || !request.ContainsKey("userId"))
            return Results.BadRequest(new { detail = "userId gereklidir" });
        
        var userId = Convert.ToInt32(request["userId"].ToString());
        
        var notifications = await dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(new { message = "Tüm bildirimler okundu olarak işaretlendi" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Mark all as read error: {ex.Message}");
        return Results.Problem($"Bildirimler işaretleme hatası: {ex.Message}");
    }
});

// Delete notification
app.MapDelete("/api/notifications/{id}", async (int id, TicketDbContext dbContext) =>
{
    var notification = await dbContext.Notifications.FindAsync(id);
    if (notification == null)
        return Results.NotFound(new { message = "Bildirim bulunamadı" });
    
    dbContext.Notifications.Remove(notification);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Bildirim silindi" });
});

// Helper methods
async Task SendTicketCreatedNotifications(BiSoyle.Ticket.Service.Data.Ticket ticket, IServiceProvider serviceProvider)
{
    try
    {
        var userServiceUrl = Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? "http://localhost:5004";
        var emailServiceUrl = Environment.GetEnvironmentVariable("EMAIL_SERVICE_URL") ?? "http://localhost:5011";
        
        using var httpClient = new HttpClient();
        
        // SuperAdmin kullanıcılarını bul
        var usersResponse = await httpClient.GetAsync($"{userServiceUrl}/api/users?role=SuperAdmin");
        if (usersResponse.IsSuccessStatusCode)
        {
            var usersJson = await usersResponse.Content.ReadAsStringAsync();
            var users = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(usersJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (users != null)
            {
                foreach (var user in users)
                {
                    try
                    {
                        var userId = Convert.ToInt32(user.GetType().GetProperty("Id")?.GetValue(user));
                        var email = user.GetType().GetProperty("Email")?.GetValue(user)?.ToString();
                        var firstName = user.GetType().GetProperty("FirstName")?.GetValue(user)?.ToString() ?? "";
                        var lastName = user.GetType().GetProperty("LastName")?.GetValue(user)?.ToString() ?? "";
                        
                        // Notification oluştur
                        var notification = new BiSoyle.Ticket.Service.Data.Notification
                        {
                            TenantId = 0, // SuperAdmin için
                            UserId = userId,
                            Type = "TicketCreated",
                            Title = "Yeni Ticket Talebi",
                            Message = $"'{ticket.Title}' başlıklı yeni bir ticket talebi oluşturuldu.",
                            RelatedTicketId = ticket.Id,
                            IsRead = false,
                            CreatedDate = DateTime.UtcNow
                        };
                        
                        // Notification'ı kaydetmek için yeni bir scope oluştur
                        using (var serviceScope = serviceProvider.CreateScope())
                        {
                            var db = serviceScope.ServiceProvider.GetRequiredService<TicketDbContext>();
                            db.Notifications.Add(notification);
                            await db.SaveChangesAsync();
                        }
                        
                        // Email gönder
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            var emailBody = $@"
                                <h2>Yeni Ticket Talebi: {ticket.Title}</h2>
                                <p>Sayın {firstName} {lastName},</p>
                                <p>Yeni bir ticket talebi oluşturuldu:</p>
                                <p><strong>Başlık:</strong> {ticket.Title}</p>
                                <p><strong>Açıklama:</strong> {ticket.Description}</p>
                                <p><strong>Öncelik:</strong> {ticket.Priority}</p>
                                <p>Detaylar için lütfen sisteme giriş yapın.</p>
                            ";
                            
                            var emailRequest = new
                            {
                                To = email,
                                Subject = $"Yeni Ticket Talebi: {ticket.Title}",
                                Body = emailBody,
                                IsHtml = true
                            };
                            
                            var emailJson = System.Text.Json.JsonSerializer.Serialize(emailRequest);
                            var emailContent = new StringContent(emailJson, Encoding.UTF8, "application/json");
                            
                            await httpClient.PostAsync($"{emailServiceUrl}/api/email/send", emailContent);
                            Console.WriteLine($"Ticket created email sent to: {email}");
                        }
                    }
                    catch (Exception userEx)
                    {
                        Console.WriteLine($"User notification/email error: {userEx.Message}");
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ticket notification error: {ex.Message}");
    }
}

async Task SendTicketCommentNotifications(BiSoyle.Ticket.Service.Data.Ticket ticket, BiSoyle.Ticket.Service.Data.TicketComment comment, IServiceProvider serviceProvider)
{
    try
    {
        var userServiceUrl = Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? "http://localhost:5004";
        var emailServiceUrl = Environment.GetEnvironmentVariable("EMAIL_SERVICE_URL") ?? "http://localhost:5011";
        
        using var httpClient = new HttpClient();
        
        // Comment yapan kullanıcıyı al
        Dictionary<string, object>? commentUser = null;
        try
        {
            var commentUserResponse = await httpClient.GetAsync($"{userServiceUrl}/api/users/{comment.UserId}");
            if (commentUserResponse.IsSuccessStatusCode)
            {
                var commentUserJson = await commentUserResponse.Content.ReadAsStringAsync();
                commentUser = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(commentUserJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                Console.WriteLine($"Ticket Service: Failed to get comment user {comment.UserId}, status: {commentUserResponse.StatusCode}");
                return; // Kullanıcı bilgisi alınamazsa notification gönderme
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ticket Service: Error getting comment user: {ex.Message}");
            return; // Hata durumunda notification gönderme
        }
        
        if (commentUser == null)
        {
            Console.WriteLine($"Ticket Service: Comment user is null for userId {comment.UserId}");
            return;
        }
        
        var commentUserRoles = commentUser.GetValueOrDefault("roles")?.ToString() ?? "";
        var isSuperAdmin = commentUserRoles.Contains("SuperAdmin", StringComparison.OrdinalIgnoreCase);
        
        if (isSuperAdmin)
        {
            // SuperAdmin yorum yaptı, ticket sahibine bildirim gönder
            var ticketCreatorResponse = await httpClient.GetAsync($"{userServiceUrl}/api/users/{ticket.CreatedByUserId}");
            if (ticketCreatorResponse.IsSuccessStatusCode)
            {
                var ticketCreatorJson = await ticketCreatorResponse.Content.ReadAsStringAsync();
                var ticketCreator = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ticketCreatorJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                var userId = Convert.ToInt32(ticketCreator?["id"]?.ToString());
                var email = ticketCreator?["email"]?.ToString();
                var firstName = ticketCreator?["firstName"]?.ToString() ?? "";
                var lastName = ticketCreator?["lastName"]?.ToString() ?? "";
                
                // Notification
                var notification = new BiSoyle.Ticket.Service.Data.Notification
                {
                    TenantId = ticket.TenantId,
                    UserId = userId,
                    Type = "TicketReplied",
                    Title = "Ticket'inize Yanıt Verildi",
                    Message = $"'{ticket.Title}' ticket'ınıza yanıt verildi.",
                    RelatedTicketId = ticket.Id,
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                };
                
                // Notification'ı kaydetmek için yeni bir scope oluştur
                using (var serviceScope = serviceProvider.CreateScope())
                {
                    var db = serviceScope.ServiceProvider.GetRequiredService<TicketDbContext>();
                    db.Notifications.Add(notification);
                    await db.SaveChangesAsync();
                }
                
                // Email
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var emailBody = $@"
                        <h2>Ticket'inize Yanıt Verildi: {ticket.Title}</h2>
                        <p>Sayın {firstName} {lastName},</p>
                        <p>Ticket'ınıza yanıt verildi:</p>
                        <p><strong>Yanıt:</strong> {comment.Comment}</p>
                        <p>Detaylar için lütfen sisteme giriş yapın.</p>
                    ";
                    
                    var emailRequest = new
                    {
                        To = email,
                        Subject = $"Ticket'inize Yanıt Verildi: {ticket.Title}",
                        Body = emailBody,
                        IsHtml = true
                    };
                    
                    var emailJson = System.Text.Json.JsonSerializer.Serialize(emailRequest);
                    var emailContent = new StringContent(emailJson, Encoding.UTF8, "application/json");
                    
                    await httpClient.PostAsync($"{emailServiceUrl}/api/email/send", emailContent);
                    Console.WriteLine($"Ticket reply email sent to: {email}");
                }
            }
        }
        else
        {
            // Admin yorum yaptı, SuperAdmin'lere bildirim gönder
            var usersResponse = await httpClient.GetAsync($"{userServiceUrl}/api/users?role=SuperAdmin");
            if (usersResponse.IsSuccessStatusCode)
            {
                var usersJson = await usersResponse.Content.ReadAsStringAsync();
                var users = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(usersJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (users != null)
                {
                    foreach (var user in users)
                    {
                        try
                        {
                            var userId = Convert.ToInt32(user.GetType().GetProperty("Id")?.GetValue(user));
                            var email = user.GetType().GetProperty("Email")?.GetValue(user)?.ToString();
                            var firstName = user.GetType().GetProperty("FirstName")?.GetValue(user)?.ToString() ?? "";
                            var lastName = user.GetType().GetProperty("LastName")?.GetValue(user)?.ToString() ?? "";
                            
                            // Notification
                            var notification = new BiSoyle.Ticket.Service.Data.Notification
                            {
                                TenantId = 0,
                                UserId = userId,
                                Type = "TicketCommented",
                                Title = "Ticket'a Yorum Yapıldı",
                                Message = $"'{ticket.Title}' ticket'ına yorum yapıldı.",
                                RelatedTicketId = ticket.Id,
                                IsRead = false,
                                CreatedDate = DateTime.UtcNow
                            };
                            
                            // Notification'ı kaydetmek için yeni bir scope oluştur
                            using (var serviceScope = serviceProvider.CreateScope())
                            {
                                var db = serviceScope.ServiceProvider.GetRequiredService<TicketDbContext>();
                                db.Notifications.Add(notification);
                                await db.SaveChangesAsync();
                            }
                            
                            // Email
                            if (!string.IsNullOrWhiteSpace(email))
                            {
                                var emailBody = $@"
                                    <h2>Ticket'a Yorum Yapıldı: {ticket.Title}</h2>
                                    <p>Sayın {firstName} {lastName},</p>
                                    <p>Ticket'a yorum yapıldı:</p>
                                    <p><strong>Yorum:</strong> {comment.Comment}</p>
                                    <p>Detaylar için lütfen sisteme giriş yapın.</p>
                                ";
                                
                                var emailRequest = new
                                {
                                    To = email,
                                    Subject = $"Ticket'a Yorum Yapıldı: {ticket.Title}",
                                    Body = emailBody,
                                    IsHtml = true
                                };
                                
                                var emailJson = System.Text.Json.JsonSerializer.Serialize(emailRequest);
                                var emailContent = new StringContent(emailJson, Encoding.UTF8, "application/json");
                                
                                await httpClient.PostAsync($"{emailServiceUrl}/api/email/send", emailContent);
                                Console.WriteLine($"Ticket comment email sent to: {email}");
                            }
                        }
                        catch (Exception userEx)
                        {
                            Console.WriteLine($"User notification/email error: {userEx.Message}");
                        }
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ticket comment notification error: {ex.Message}");
    }
}

app.Run();


using Microsoft.EntityFrameworkCore;
using BiSoyle.Ticket.Service.Data;
using System.Linq;
using System.Net.Http;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BiSoyle - Ticket Service API",
        Version = "v1",
        Description = "Multi-Tenant Ticket Management API<br/><strong>BY HOSTEAGLE INFORMATION TECHNOLOGIES</strong>",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HostEagle Information Technologies",
            Email = "info@hosteagle.com"
        }
    });
});

// Database
builder.Services.AddDbContext<TicketDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// HttpClient for calling Tenant Service
builder.Services.AddHttpClient();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:4200", "http://localhost:3000", "http://localhost:4300", "http://127.0.0.1:4200", "http://127.0.0.1:4300")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(',') 
                ?? new[] { "http://localhost:4200" };
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// JSON Serializer Options for Minimal APIs
var jsonOptions = new System.Text.Json.JsonSerializerOptions
{
    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
    WriteIndented = true
};

var app = builder.Build();

// Set port for Ticket Service (5005)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5005";
app.Urls.Add($"http://localhost:{port}");

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration error: {ex.Message}");
    }
    
    // Tables oluştur (eğer yoksa)
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
                    CREATE TABLE IF NOT EXISTS tickets (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INTEGER NOT NULL,
                        ""CreatedByUserId"" INTEGER NOT NULL,
                        ""Title"" VARCHAR(200) NOT NULL,
                        ""Description"" VARCHAR(2000) NOT NULL,
                        ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Open',
                        ""Priority"" VARCHAR(50) NOT NULL DEFAULT 'Medium',
                        ""IsStarred"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""IsImportant"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""ResolvedDate"" TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_tickets_TenantId"" ON tickets (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_tickets_CreatedByUserId"" ON tickets (""CreatedByUserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_tickets_Status"" ON tickets (""Status"");
                    CREATE INDEX IF NOT EXISTS ""IX_tickets_CreatedDate"" ON tickets (""CreatedDate"");
                    
                    CREATE TABLE IF NOT EXISTS ticket_comments (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TicketId"" INTEGER NOT NULL,
                        ""UserId"" INTEGER NOT NULL,
                        ""Comment"" VARCHAR(2000) NOT NULL,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT ""FK_ticket_comments_tickets"" FOREIGN KEY (""TicketId"") REFERENCES tickets(""Id"") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_ticket_comments_TicketId"" ON ticket_comments (""TicketId"");
                    CREATE INDEX IF NOT EXISTS ""IX_ticket_comments_UserId"" ON ticket_comments (""UserId"");
                    
                    CREATE TABLE IF NOT EXISTS ticket_attachments (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TicketId"" INTEGER NOT NULL,
                        ""CommentId"" INTEGER,
                        ""FileName"" VARCHAR(255) NOT NULL,
                        ""FilePath"" VARCHAR(500) NOT NULL,
                        ""FileType"" VARCHAR(50),
                        ""FileSize"" BIGINT NOT NULL,
                        ""UploadedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT ""FK_ticket_attachments_tickets"" FOREIGN KEY (""TicketId"") REFERENCES tickets(""Id"") ON DELETE CASCADE,
                        CONSTRAINT ""FK_ticket_attachments_comments"" FOREIGN KEY (""CommentId"") REFERENCES ticket_comments(""Id"") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_ticket_attachments_TicketId"" ON ticket_attachments (""TicketId"");
                    CREATE INDEX IF NOT EXISTS ""IX_ticket_attachments_CommentId"" ON ticket_attachments (""CommentId"");
                    
                    CREATE TABLE IF NOT EXISTS notifications (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INTEGER NOT NULL,
                        ""UserId"" INTEGER NOT NULL,
                        ""Type"" VARCHAR(50) NOT NULL,
                        ""Title"" VARCHAR(200) NOT NULL,
                        ""Message"" VARCHAR(500),
                        ""RelatedTicketId"" INTEGER,
                        ""IsRead"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_UserId"" ON notifications (""UserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_TenantId"" ON notifications (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_RelatedTicketId"" ON notifications (""RelatedTicketId"");
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_IsRead"" ON notifications (""IsRead"");
                ";
                command.ExecuteNonQuery();
                Console.WriteLine("Ticket tables created successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ticket tables creation error (may already exist): {ex.Message}");
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
        Console.WriteLine($"Ticket tables setup error: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// File uploads directory
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// =====================================================
// TICKET ENDPOINTS
// =====================================================

// Get all tickets
app.MapGet("/api/tickets", async (TicketDbContext dbContext, int? tenantId, int? createdByUserId, string? status, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        Console.WriteLine($"Ticket Service: GET /api/tickets - tenantId={tenantId}, createdByUserId={createdByUserId}, status={status}");
        
        var query = dbContext.Tickets.AsQueryable();
        
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);
        
        if (createdByUserId.HasValue)
            query = query.Where(t => t.CreatedByUserId == createdByUserId.Value);
        
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);
        
        var tickets = await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
        Console.WriteLine($"Ticket Service: Found {tickets.Count} tickets");
        
        // Tenant bilgilerini al (eğer tenant ID'leri varsa)
        var tenantIds = tickets.Where(t => t.TenantId > 0).Select(t => t.TenantId).Distinct().ToList();
        var tenantNames = new Dictionary<int, string>();
        
        if (tenantIds.Any())
        {
            try
            {
                var tenantServiceUrl = Environment.GetEnvironmentVariable("TENANT_SERVICE_URL") ?? "http://localhost:5006";
                var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                foreach (var tid in tenantIds)
                {
                    try
                    {
                        var response = await client.GetAsync($"{tenantServiceUrl}/api/tenants/{tid}");
                        if (response.IsSuccessStatusCode)
                        {
                            var tenantJson = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Tenant {tid} response: {tenantJson}");
                            var tenantDoc = System.Text.Json.JsonDocument.Parse(tenantJson);
                            
                            // Önce "FirmaAdi" field'ını dene (Tenant Service'in döndürdüğü field)
                            if (tenantDoc.RootElement.TryGetProperty("firmaAdi", out var firmaAdiElement))
                            {
                                tenantNames[tid] = firmaAdiElement.GetString() ?? $"Tenant {tid}";
                                Console.WriteLine($"Tenant {tid} firmaAdi found: {tenantNames[tid]}");
                            }
                            // Sonra "FirmaAdi" (PascalCase) field'ını dene
                            else if (tenantDoc.RootElement.TryGetProperty("FirmaAdi", out var FirmaAdiElement))
                            {
                                tenantNames[tid] = FirmaAdiElement.GetString() ?? $"Tenant {tid}";
                                Console.WriteLine($"Tenant {tid} FirmaAdi found: {tenantNames[tid]}");
                            }
                            // Sonra "name" field'ını dene (camelCase)
                            else if (tenantDoc.RootElement.TryGetProperty("name", out var nameElement))
                            {
                                tenantNames[tid] = nameElement.GetString() ?? $"Tenant {tid}";
                                Console.WriteLine($"Tenant {tid} name found: {tenantNames[tid]}");
                            }
                            // Sonra "Name" field'ını dene (PascalCase)
                            else if (tenantDoc.RootElement.TryGetProperty("Name", out var nameElement2))
                            {
                                tenantNames[tid] = nameElement2.GetString() ?? $"Tenant {tid}";
                                Console.WriteLine($"Tenant {tid} Name found: {tenantNames[tid]}");
                            }
                            // Sonra "companyName" field'ını dene
                            else if (tenantDoc.RootElement.TryGetProperty("companyName", out var companyNameElement))
                            {
                                tenantNames[tid] = companyNameElement.GetString() ?? $"Tenant {tid}";
                                Console.WriteLine($"Tenant {tid} companyName found: {tenantNames[tid]}");
                            }
                            else
                            {
                                Console.WriteLine($"Tenant {tid} - name field not found. Available properties:");
                                foreach (var prop in tenantDoc.RootElement.EnumerateObject())
                                {
                                    Console.WriteLine($"  - {prop.Name}");
                                }
                                tenantNames[tid] = $"Tenant {tid}";
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Tenant {tid} request failed with status: {response.StatusCode}");
                            tenantNames[tid] = $"Tenant {tid}";
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Tenant {tid} bilgisi alınamadı: {ex.Message}");
                        tenantNames[tid] = $"Tenant {tid}";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tenant bilgileri alınırken hata: {ex.Message}");
            }
        }
        
        // Circular reference'ı önlemek için anonim objelere dönüştür
        var ticketDtos = tickets.Select(t => new
        {
            t.Id,
            t.TenantId,
            TenantName = t.TenantId > 0 && tenantNames.ContainsKey(t.TenantId) ? tenantNames[t.TenantId] : (t.TenantId == 0 ? "SuperAdmin" : $"Tenant {t.TenantId}"),
            t.CreatedByUserId,
            t.Title,
            t.Description,
            t.Status,
            t.Priority,
            t.IsStarred,
            t.IsImportant,
            t.CreatedDate,
            t.UpdatedDate,
            t.ResolvedDate
        }).ToList();
        
        Console.WriteLine($"Ticket Service: Returning {ticketDtos.Count} ticket DTOs");
        if (ticketDtos.Any())
        {
            var firstTicket = ticketDtos.First();
            Console.WriteLine($"First ticket - ID: {firstTicket.Id}, TenantId: {firstTicket.TenantId}, TenantName: {firstTicket.TenantName}");
        }
        Console.WriteLine($"Tenant names dictionary count: {tenantNames.Count}");
        foreach (var kvp in tenantNames)
        {
            Console.WriteLine($"  Tenant {kvp.Key}: {kvp.Value}");
        }
        return Results.Ok(ticketDtos);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Tickets get error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        return Results.Problem($"Ticket'lar getirme hatası: {ex.Message}");
    }
});

// Get ticket by ID
app.MapGet("/api/tickets/{id}", async (int id, TicketDbContext dbContext, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var ticket = await dbContext.Tickets
            .Include(t => t.Comments)
                .ThenInclude(c => c.Attachments)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (ticket == null)
            return Results.NotFound(new { message = "Ticket bulunamadı" });
        
        // Tenant adını al
        string tenantName = "SuperAdmin";
        if (ticket.TenantId > 0)
        {
            try
            {
                var tenantServiceUrl = Environment.GetEnvironmentVariable("TENANT_SERVICE_URL") ?? "http://localhost:5006";
                var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                var response = await client.GetAsync($"{tenantServiceUrl}/api/tenants/{ticket.TenantId}");
                if (response.IsSuccessStatusCode)
                {
                    var tenantJson = await response.Content.ReadAsStringAsync();
                    var tenantDoc = System.Text.Json.JsonDocument.Parse(tenantJson);
                    // Önce "FirmaAdi" field'ını dene (Tenant Service'in döndürdüğü field)
                    if (tenantDoc.RootElement.TryGetProperty("firmaAdi", out var firmaAdiElement))
                    {
                        tenantName = firmaAdiElement.GetString() ?? $"Tenant {ticket.TenantId}";
                    }
                    else if (tenantDoc.RootElement.TryGetProperty("FirmaAdi", out var FirmaAdiElement))
                    {
                        tenantName = FirmaAdiElement.GetString() ?? $"Tenant {ticket.TenantId}";
                    }
                    else if (tenantDoc.RootElement.TryGetProperty("name", out var nameElement))
                    {
                        tenantName = nameElement.GetString() ?? $"Tenant {ticket.TenantId}";
                    }
                    else if (tenantDoc.RootElement.TryGetProperty("Name", out var NameElement))
                    {
                        tenantName = NameElement.GetString() ?? $"Tenant {ticket.TenantId}";
                    }
                    else if (tenantDoc.RootElement.TryGetProperty("companyName", out var companyNameElement))
                    {
                        tenantName = companyNameElement.GetString() ?? $"Tenant {ticket.TenantId}";
                    }
                    else
                    {
                        tenantName = $"Tenant {ticket.TenantId}";
                    }
                }
                else
                {
                    tenantName = $"Tenant {ticket.TenantId}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tenant bilgisi alınamadı: {ex.Message}");
                tenantName = $"Tenant {ticket.TenantId}";
            }
        }
        
            // Yorumlardaki kullanıcı adlarını al
            var userIds = ticket.Comments?.Select(c => c.UserId).Distinct().ToList() ?? new List<int>();
            var userNames = new Dictionary<int, string>();
            
            if (userIds.Any())
            {
                try
                {
                    var userServiceUrl = Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? "http://localhost:5004";
                    var client = httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    
                    foreach (var uid in userIds)
                    {
                        try
                        {
                            var response = await client.GetAsync($"{userServiceUrl}/api/users/internal/{uid}");
                            if (response.IsSuccessStatusCode)
                            {
                                var userJson = await response.Content.ReadAsStringAsync();
                                var userDoc = System.Text.Json.JsonDocument.Parse(userJson);
                                
                                string userName = $"User {uid}";
                                if (userDoc.RootElement.TryGetProperty("firstName", out var firstNameElement) && 
                                    userDoc.RootElement.TryGetProperty("lastName", out var lastNameElement))
                                {
                                    var firstName = firstNameElement.GetString() ?? "";
                                    var lastName = lastNameElement.GetString() ?? "";
                                    userName = $"{firstName} {lastName}".Trim();
                                    if (string.IsNullOrEmpty(userName))
                                    {
                                        // firstName ve lastName boşsa username'i kullan
                                        if (userDoc.RootElement.TryGetProperty("username", out var usernameElement))
                                        {
                                            userName = usernameElement.GetString() ?? $"User {uid}";
                                        }
                                    }
                                }
                                else if (userDoc.RootElement.TryGetProperty("username", out var usernameElement2))
                                {
                                    userName = usernameElement2.GetString() ?? $"User {uid}";
                                }
                                
                                userNames[uid] = userName;
                            }
                            else
                            {
                                userNames[uid] = $"User {uid}";
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"User {uid} bilgisi alınamadı: {ex.Message}");
                            userNames[uid] = $"User {uid}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Kullanıcı bilgileri alınırken hata: {ex.Message}");
                }
            }
            
            // Circular reference'ı önlemek için anonim objeye dönüştür
            var ticketDto = new
            {
                ticket.Id,
                ticket.TenantId,
                TenantName = tenantName,
                ticket.CreatedByUserId,
                ticket.Title,
                ticket.Description,
                ticket.Status,
                ticket.Priority,
                ticket.IsStarred,
                ticket.IsImportant,
                ticket.CreatedDate,
                ticket.UpdatedDate,
                ticket.ResolvedDate,
                Comments = ticket.Comments?.Select(c => new
                {
                    c.Id,
                    c.TicketId,
                    c.UserId,
                    UserName = userNames.ContainsKey(c.UserId) ? userNames[c.UserId] : $"User {c.UserId}",
                    c.Comment,
                    c.CreatedDate,
                    Attachments = c.Attachments?.Select(a => new
                    {
                        a.Id,
                        a.TicketId,
                        a.CommentId,
                        a.FileName,
                        a.FilePath,
                        a.FileType,
                        a.FileSize,
                        a.UploadedDate
                    }).ToList()
                }).ToList(),
                Attachments = ticket.Attachments?.Select(a => new
                {
                    a.Id,
                    a.TicketId,
                    a.CommentId,
                    a.FileName,
                    a.FilePath,
                    a.FileType,
                    a.FileSize,
                    a.UploadedDate
                }).ToList()
            };
        
        return Results.Ok(ticketDto);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ticket get error: {ex.Message}");
        return Results.Problem($"Ticket getirme hatası: {ex.Message}");
    }
});

// Create ticket (Admin only)
app.MapPost("/api/tickets", async (HttpContext context, TicketDbContext dbContext, IServiceProvider serviceProvider) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var ticket = System.Text.Json.JsonSerializer.Deserialize<BiSoyle.Ticket.Service.Data.Ticket>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (ticket == null)
            return Results.BadRequest(new { detail = "Geçersiz ticket verisi" });
        
        if (string.IsNullOrWhiteSpace(ticket.Title))
            return Results.BadRequest(new { detail = "Ticket başlığı gereklidir" });
        
        if (string.IsNullOrWhiteSpace(ticket.Description))
            return Results.BadRequest(new { detail = "Ticket açıklaması gereklidir" });
        
        if (ticket.TenantId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz TenantId" });
        
        if (ticket.CreatedByUserId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz CreatedByUserId" });
        
        ticket.Status = "Open";
        ticket.CreatedDate = DateTime.UtcNow;
        ticket.UpdatedDate = DateTime.UtcNow;
        
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();
        
        Console.WriteLine($"Ticket Service: Created ticket with ID {ticket.Id}");
        
        // SuperAdmin'lere bildirim gönder
        _ = Task.Run(async () =>
        {
            try
            {
                await SendTicketCreatedNotifications(ticket, serviceProvider);
            }
            catch (Exception notifEx)
            {
                Console.WriteLine($"Notification creation error: {notifEx.Message}");
            }
        });
        
        // Circular reference'ı önlemek için anonim objeye dönüştür
        var ticketDto = new
        {
            ticket.Id,
            ticket.TenantId,
            ticket.CreatedByUserId,
            ticket.Title,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            ticket.CreatedDate,
            ticket.UpdatedDate,
            ticket.ResolvedDate
        };
        
        return Results.Created($"/api/tickets/{ticket.Id}", ticketDto);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ticket save error: {ex.Message}");
        return Results.Problem($"Ticket kaydetme hatası: {ex.Message}");
    }
});

// Update ticket
app.MapPut("/api/tickets/{id}", async (int id, HttpContext context, TicketDbContext dbContext) =>
{
    try
    {
        var existingTicket = await dbContext.Tickets.FindAsync(id);
        if (existingTicket == null)
            return Results.NotFound(new { message = "Ticket bulunamadı" });
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var ticket = System.Text.Json.JsonSerializer.Deserialize<BiSoyle.Ticket.Service.Data.Ticket>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (ticket == null)
            return Results.BadRequest(new { detail = "Geçersiz ticket verisi" });
        
        existingTicket.Title = ticket.Title;
        existingTicket.Description = ticket.Description;
        existingTicket.Status = ticket.Status;
        existingTicket.Priority = ticket.Priority;
        // IsStarred ve IsImportant alanlarını güncelle (eğer gönderilmişse)
        existingTicket.IsStarred = ticket.IsStarred;
        existingTicket.IsImportant = ticket.IsImportant;
        existingTicket.UpdatedDate = DateTime.UtcNow;
        
        if (ticket.Status == "Resolved" || ticket.Status == "Closed")
        {
            existingTicket.ResolvedDate = DateTime.UtcNow;
        }
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(existingTicket);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ticket update error: {ex.Message}");
        return Results.Problem($"Ticket güncelleme hatası: {ex.Message}");
    }
});

// Delete ticket
app.MapDelete("/api/tickets/{id}", async (int id, TicketDbContext dbContext) =>
{
    var ticket = await dbContext.Tickets.FindAsync(id);
    if (ticket == null)
        return Results.NotFound(new { message = "Ticket bulunamadı" });
    
    dbContext.Tickets.Remove(ticket);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Ticket silindi" });
});

// =====================================================
// TICKET COMMENT ENDPOINTS
// =====================================================

// Get ticket comments
app.MapGet("/api/tickets/{ticketId}/comments", async (int ticketId, TicketDbContext dbContext) =>
{
    try
    {
        var comments = await dbContext.TicketComments
            .Include(c => c.Attachments)
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedDate)
            .ToListAsync();
        
        // Circular reference'ı önlemek için anonim objelere dönüştür
        var commentDtos = comments.Select(c => new
        {
            c.Id,
            c.TicketId,
            c.UserId,
            c.Comment,
            c.CreatedDate,
            Attachments = c.Attachments?.Select(a => new
            {
                a.Id,
                a.TicketId,
                a.CommentId,
                a.FileName,
                a.FilePath,
                a.FileType,
                a.FileSize,
                a.UploadedDate
            }).ToList()
        }).ToList();
        
        return Results.Ok(commentDtos);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Comments get error: {ex.Message}");
        return Results.Problem($"Yorumlar getirme hatası: {ex.Message}");
    }
});

// Add comment to ticket
app.MapPost("/api/tickets/{ticketId}/comments", async (int ticketId, HttpContext context, TicketDbContext dbContext, IServiceProvider serviceProvider) =>
{
    try
    {
        Console.WriteLine($"Ticket Service: POST /api/tickets/{ticketId}/comments");
        
        var ticket = await dbContext.Tickets.FindAsync(ticketId);
        if (ticket == null)
        {
            Console.WriteLine($"Ticket Service: Ticket {ticketId} not found");
            return Results.NotFound(new { message = "Ticket bulunamadı" });
        }
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Console.WriteLine($"Ticket Service: Request body: {body}");
        
        var commentData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (commentData == null)
        {
            Console.WriteLine("Ticket Service: commentData is null");
            return Results.BadRequest(new { detail = "Geçersiz veri" });
        }
        
        if (!commentData.ContainsKey("comment"))
        {
            Console.WriteLine("Ticket Service: comment key not found");
            return Results.BadRequest(new { detail = "comment gereklidir" });
        }
        
        if (!commentData.ContainsKey("userId"))
        {
            Console.WriteLine("Ticket Service: userId key not found");
            return Results.BadRequest(new { detail = "userId gereklidir" });
        }
        
        // userId'yi güvenli şekilde parse et
        int userId = 0;
        try
        {
            var userIdValue = commentData["userId"];
            if (userIdValue == null)
            {
                Console.WriteLine("Ticket Service: userId is null");
                return Results.BadRequest(new { detail = "userId gereklidir" });
            }
            
            if (userIdValue is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    userId = jsonElement.GetInt32();
                }
                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var userIdStr = jsonElement.GetString();
                    if (!int.TryParse(userIdStr, out userId))
                    {
                        Console.WriteLine($"Ticket Service: userId parse failed: {userIdStr}");
                        return Results.BadRequest(new { detail = "Geçersiz userId formatı" });
                    }
                }
                else
                {
                    Console.WriteLine($"Ticket Service: userId unexpected type: {jsonElement.ValueKind}");
                    return Results.BadRequest(new { detail = "Geçersiz userId formatı" });
                }
            }
            else
            {
                var userIdStr = userIdValue?.ToString();
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out userId))
                {
                    Console.WriteLine($"Ticket Service: userId parse failed: {userIdStr}");
                    return Results.BadRequest(new { detail = "Geçersiz userId formatı" });
                }
            }
        }
        catch (Exception parseEx)
        {
            Console.WriteLine($"Ticket Service: userId parse error: {parseEx.Message}");
            Console.WriteLine($"Ticket Service: Stack trace: {parseEx.StackTrace}");
            return Results.BadRequest(new { detail = $"Geçersiz userId formatı: {parseEx.Message}" });
        }
        
        if (userId <= 0)
        {
            Console.WriteLine($"Ticket Service: Invalid userId: {userId}");
            return Results.BadRequest(new { detail = "Geçersiz userId (0'dan büyük olmalı)" });
        }
        
        // comment'i güvenli şekilde al
        string commentText = "";
        try
        {
            var commentValue = commentData["comment"];
            if (commentValue == null)
            {
                Console.WriteLine("Ticket Service: comment is null");
                return Results.BadRequest(new { detail = "comment gereklidir" });
            }
            
            if (commentValue is System.Text.Json.JsonElement commentElement)
            {
                if (commentElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    commentText = commentElement.GetString() ?? "";
                }
                else
                {
                    commentText = commentElement.ToString();
                }
            }
            else
            {
                commentText = commentValue?.ToString() ?? "";
            }
        }
        catch (Exception commentEx)
        {
            Console.WriteLine($"Ticket Service: comment parse error: {commentEx.Message}");
            Console.WriteLine($"Ticket Service: Stack trace: {commentEx.StackTrace}");
            return Results.BadRequest(new { detail = $"Geçersiz comment formatı: {commentEx.Message}" });
        }
        
        if (string.IsNullOrWhiteSpace(commentText))
        {
            Console.WriteLine("Ticket Service: comment is empty");
            return Results.BadRequest(new { detail = "comment boş olamaz" });
        }
        
        var comment = new BiSoyle.Ticket.Service.Data.TicketComment
        {
            TicketId = ticketId,
            UserId = userId,
            Comment = commentText,
            CreatedDate = DateTime.UtcNow
        };
        
        dbContext.TicketComments.Add(comment);
        
        // Ticket'ı güncelle
        ticket.UpdatedDate = DateTime.UtcNow;
        // Eğer ticket kapalıysa, yorum yapıldığında tekrar açık yap
        if (ticket.Status == "Closed")
        {
            ticket.Status = "Open";
        }
        else if (ticket.Status == "Open")
        {
            ticket.Status = "InProgress";
        }
        
        await dbContext.SaveChangesAsync();
        
        Console.WriteLine($"Ticket Service: Comment {comment.Id} created successfully");
        
        // Response'u hazırla - önce response'u hazırla, sonra notification gönder
        var response = new
        {
            id = comment.Id,
            ticketId = comment.TicketId,
            userId = comment.UserId,
            comment = comment.Comment,
            createdDate = comment.CreatedDate
        };
        
        // Response'u önce döndür, notification'ı arka planda gönder
        var result = Results.Created($"/api/tickets/{ticketId}/comments/{comment.Id}", response);
        
        // Yorum yapan SuperAdmin ise ticket sahibine, değilse SuperAdmin'lere bildirim gönder
        // Notification'ı arka planda gönder, response'u bekleme
        _ = Task.Run(async () =>
        {
            try
            {
                await SendTicketCommentNotifications(ticket, comment, serviceProvider);
            }
            catch (Exception notifEx)
            {
                Console.WriteLine($"Notification creation error: {notifEx.Message}");
                Console.WriteLine($"Notification error stack trace: {notifEx.StackTrace}");
            }
        });
        
        // Response'u döndür - notification hatası response'u etkilemesin
        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Comment save error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        return Results.Problem($"Yorum kaydetme hatası: {ex.Message}");
    }
});

// Delete comment
app.MapDelete("/api/tickets/{ticketId}/comments/{commentId}", async (int ticketId, int commentId, TicketDbContext dbContext) =>
{
    var comment = await dbContext.TicketComments.FindAsync(commentId);
    if (comment == null || comment.TicketId != ticketId)
        return Results.NotFound(new { message = "Yorum bulunamadı" });
    
    dbContext.TicketComments.Remove(comment);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Yorum silindi" });
});

// =====================================================
// FILE UPLOAD ENDPOINTS
// =====================================================

// Upload file for ticket (multiple files support)
app.MapPost("/api/tickets/{ticketId}/upload", async (int ticketId, HttpContext context, TicketDbContext dbContext) =>
{
    try
    {
        Console.WriteLine($"Ticket Service: File upload request for ticket {ticketId}");
        
        var ticket = await dbContext.Tickets.FindAsync(ticketId);
        if (ticket == null)
        {
            Console.WriteLine($"Ticket Service: Ticket {ticketId} not found");
            return Results.NotFound(new { message = "Ticket bulunamadı" });
        }
        
        if (!context.Request.HasFormContentType)
        {
            Console.WriteLine($"Ticket Service: Request does not have form content type");
            return Results.BadRequest(new { detail = "Form data bekleniyor" });
        }
        
        Console.WriteLine($"Ticket Service: Reading form data");
        var form = await context.Request.ReadFormAsync();
        var files = form.Files;
        
        Console.WriteLine($"Ticket Service: Received {files?.Count ?? 0} files");
        if (files == null || files.Count == 0)
        {
            Console.WriteLine($"Ticket Service: No files in request");
            return Results.BadRequest(new { detail = "Dosya yüklenmedi" });
        }
        
        var maxFileSize = builder.Configuration.GetValue<long>("FileUpload:MaxFileSize", 10485760); // 10MB default
        var allowedExtensions = builder.Configuration["FileUpload:AllowedExtensions"]?.Split(',') 
            ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
        
        var uploadedAttachments = new List<object>();
        
        // Ensure uploads directory exists
        Directory.CreateDirectory(uploadsPath);
        
        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
                continue;
            
            if (file.Length > maxFileSize)
            {
                Console.WriteLine($"File {file.FileName} exceeds max size: {file.Length}");
                continue; // Skip this file but continue with others
            }
            
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                Console.WriteLine($"File {file.FileName} has invalid extension: {fileExtension}");
                continue; // Skip this file but continue with others
            }
            
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            var attachment = new BiSoyle.Ticket.Service.Data.TicketAttachment
            {
                TicketId = ticketId,
                CommentId = null,
                FileName = file.FileName,
                FilePath = filePath,
                FileType = file.ContentType ?? fileExtension,
                FileSize = file.Length,
                UploadedDate = DateTime.UtcNow
            };
            
            dbContext.TicketAttachments.Add(attachment);
            uploadedAttachments.Add(new
            {
                attachment.Id,
                attachment.TicketId,
                attachment.CommentId,
                attachment.FileName,
                attachment.FilePath,
                attachment.FileType,
                attachment.FileSize,
                attachment.UploadedDate
            });
        }
        
        await dbContext.SaveChangesAsync();
        
        Console.WriteLine($"Ticket Service: Uploaded {uploadedAttachments.Count} files for ticket {ticketId}");
        
        return Results.Ok(new { attachments = uploadedAttachments });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"File upload error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        return Results.Problem($"Dosya yükleme hatası: {ex.Message}");
    }
});

// Upload file for comment (multiple files support)
app.MapPost("/api/tickets/{ticketId}/comments/{commentId}/upload", async (int ticketId, int commentId, HttpContext context, TicketDbContext dbContext) =>
{
    try
    {
        Console.WriteLine($"Ticket Service: File upload request for ticket {ticketId}, comment {commentId}");
        
        var comment = await dbContext.TicketComments.FindAsync(commentId);
        if (comment == null || comment.TicketId != ticketId)
        {
            Console.WriteLine($"Ticket Service: Comment {commentId} not found or doesn't belong to ticket {ticketId}");
            return Results.NotFound(new { message = "Yorum bulunamadı" });
        }
        
        Console.WriteLine($"Ticket Service: Comment found, checking form content type");
        if (!context.Request.HasFormContentType)
        {
            Console.WriteLine($"Ticket Service: Request does not have form content type");
            return Results.BadRequest(new { detail = "Form data bekleniyor" });
        }
        
        Console.WriteLine($"Ticket Service: Reading form data");
        var form = await context.Request.ReadFormAsync();
        var files = form.Files;
        
        Console.WriteLine($"Ticket Service: Received {files?.Count ?? 0} files");
        if (files == null || files.Count == 0)
        {
            Console.WriteLine($"Ticket Service: No files in request");
            return Results.BadRequest(new { detail = "Dosya yüklenmedi" });
        }
        
        var maxFileSize = builder.Configuration.GetValue<long>("FileUpload:MaxFileSize", 10485760); // 10MB default
        var allowedExtensions = builder.Configuration["FileUpload:AllowedExtensions"]?.Split(',') 
            ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
        
        var uploadedAttachments = new List<object>();
        
        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
                continue;
            
            if (file.Length > maxFileSize)
            {
                Console.WriteLine($"File {file.FileName} exceeds max size: {file.Length}");
                continue; // Skip this file but continue with others
            }
            
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                Console.WriteLine($"File {file.FileName} has invalid extension: {fileExtension}");
                continue; // Skip this file but continue with others
            }
            
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);
            
            // Ensure uploads directory exists
            Directory.CreateDirectory(uploadsPath);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            var attachment = new BiSoyle.Ticket.Service.Data.TicketAttachment
            {
                TicketId = ticketId,
                CommentId = commentId,
                FileName = file.FileName,
                FilePath = filePath,
                FileType = file.ContentType ?? fileExtension,
                FileSize = file.Length,
                UploadedDate = DateTime.UtcNow
            };
            
            dbContext.TicketAttachments.Add(attachment);
            uploadedAttachments.Add(new
            {
                attachment.Id,
                attachment.TicketId,
                attachment.CommentId,
                attachment.FileName,
                attachment.FilePath,
                attachment.FileType,
                attachment.FileSize,
                attachment.UploadedDate
            });
        }
        
        await dbContext.SaveChangesAsync();
        
        Console.WriteLine($"Ticket Service: Uploaded {uploadedAttachments.Count} files for comment {commentId}");
        
        return Results.Ok(new { attachments = uploadedAttachments });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"File upload error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        return Results.Problem($"Dosya yükleme hatası: {ex.Message}");
    }
});

// Download file
app.MapGet("/api/tickets/attachments/{id}/download", async (int id, TicketDbContext dbContext) =>
{
    var attachment = await dbContext.TicketAttachments.FindAsync(id);
    if (attachment == null || !File.Exists(attachment.FilePath))
        return Results.NotFound(new { message = "Dosya bulunamadı" });
    
    var fileBytes = await File.ReadAllBytesAsync(attachment.FilePath);
    return Results.File(fileBytes, "application/octet-stream", attachment.FileName);
});

// =====================================================
// NOTIFICATION ENDPOINTS
// =====================================================

// Get notifications
app.MapGet("/api/notifications", async (TicketDbContext dbContext, int userId, int? tenantId) =>
{
    try
    {
        var query = dbContext.Notifications.AsQueryable();
        
        query = query.Where(n => n.UserId == userId);
        
        if (tenantId.HasValue)
            query = query.Where(n => n.TenantId == tenantId.Value);
        
        var notifications = await query.OrderByDescending(n => n.CreatedDate).ToListAsync();
        return Results.Ok(notifications);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Notifications get error: {ex.Message}");
        return Results.Problem($"Bildirimler getirme hatası: {ex.Message}");
    }
});

// Get unread count
app.MapGet("/api/notifications/unread-count", async (TicketDbContext dbContext, int userId, int? tenantId) =>
{
    try
    {
        var query = dbContext.Notifications.Where(n => n.UserId == userId && !n.IsRead);
        
        if (tenantId.HasValue)
            query = query.Where(n => n.TenantId == tenantId.Value);
        
        var count = await query.CountAsync();
        return Results.Ok(new { count });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unread count error: {ex.Message}");
        return Results.Problem($"Okunmamış bildirim sayısı getirme hatası: {ex.Message}");
    }
});

// Mark as read
app.MapPatch("/api/notifications/{id}/read", async (int id, TicketDbContext dbContext) =>
{
    var notification = await dbContext.Notifications.FindAsync(id);
    if (notification == null)
        return Results.NotFound(new { message = "Bildirim bulunamadı" });
    
    notification.IsRead = true;
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(notification);
});

// Mark all as read
app.MapPatch("/api/notifications/read-all", async (HttpContext context, TicketDbContext dbContext) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var request = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (request == null || !request.ContainsKey("userId"))
            return Results.BadRequest(new { detail = "userId gereklidir" });
        
        var userId = Convert.ToInt32(request["userId"].ToString());
        
        var notifications = await dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(new { message = "Tüm bildirimler okundu olarak işaretlendi" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Mark all as read error: {ex.Message}");
        return Results.Problem($"Bildirimler işaretleme hatası: {ex.Message}");
    }
});

// Delete notification
app.MapDelete("/api/notifications/{id}", async (int id, TicketDbContext dbContext) =>
{
    var notification = await dbContext.Notifications.FindAsync(id);
    if (notification == null)
        return Results.NotFound(new { message = "Bildirim bulunamadı" });
    
    dbContext.Notifications.Remove(notification);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Bildirim silindi" });
});

// Helper methods
async Task SendTicketCreatedNotifications(BiSoyle.Ticket.Service.Data.Ticket ticket, IServiceProvider serviceProvider)
{
    try
    {
        var userServiceUrl = Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? "http://localhost:5004";
        var emailServiceUrl = Environment.GetEnvironmentVariable("EMAIL_SERVICE_URL") ?? "http://localhost:5011";
        
        using var httpClient = new HttpClient();
        
        // SuperAdmin kullanıcılarını bul
        var usersResponse = await httpClient.GetAsync($"{userServiceUrl}/api/users?role=SuperAdmin");
        if (usersResponse.IsSuccessStatusCode)
        {
            var usersJson = await usersResponse.Content.ReadAsStringAsync();
            var users = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(usersJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (users != null)
            {
                foreach (var user in users)
                {
                    try
                    {
                        var userId = Convert.ToInt32(user.GetType().GetProperty("Id")?.GetValue(user));
                        var email = user.GetType().GetProperty("Email")?.GetValue(user)?.ToString();
                        var firstName = user.GetType().GetProperty("FirstName")?.GetValue(user)?.ToString() ?? "";
                        var lastName = user.GetType().GetProperty("LastName")?.GetValue(user)?.ToString() ?? "";
                        
                        // Notification oluştur
                        var notification = new BiSoyle.Ticket.Service.Data.Notification
                        {
                            TenantId = 0, // SuperAdmin için
                            UserId = userId,
                            Type = "TicketCreated",
                            Title = "Yeni Ticket Talebi",
                            Message = $"'{ticket.Title}' başlıklı yeni bir ticket talebi oluşturuldu.",
                            RelatedTicketId = ticket.Id,
                            IsRead = false,
                            CreatedDate = DateTime.UtcNow
                        };
                        
                        // Notification'ı kaydetmek için yeni bir scope oluştur
                        using (var serviceScope = serviceProvider.CreateScope())
                        {
                            var db = serviceScope.ServiceProvider.GetRequiredService<TicketDbContext>();
                            db.Notifications.Add(notification);
                            await db.SaveChangesAsync();
                        }
                        
                        // Email gönder
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            var emailBody = $@"
                                <h2>Yeni Ticket Talebi: {ticket.Title}</h2>
                                <p>Sayın {firstName} {lastName},</p>
                                <p>Yeni bir ticket talebi oluşturuldu:</p>
                                <p><strong>Başlık:</strong> {ticket.Title}</p>
                                <p><strong>Açıklama:</strong> {ticket.Description}</p>
                                <p><strong>Öncelik:</strong> {ticket.Priority}</p>
                                <p>Detaylar için lütfen sisteme giriş yapın.</p>
                            ";
                            
                            var emailRequest = new
                            {
                                To = email,
                                Subject = $"Yeni Ticket Talebi: {ticket.Title}",
                                Body = emailBody,
                                IsHtml = true
                            };
                            
                            var emailJson = System.Text.Json.JsonSerializer.Serialize(emailRequest);
                            var emailContent = new StringContent(emailJson, Encoding.UTF8, "application/json");
                            
                            await httpClient.PostAsync($"{emailServiceUrl}/api/email/send", emailContent);
                            Console.WriteLine($"Ticket created email sent to: {email}");
                        }
                    }
                    catch (Exception userEx)
                    {
                        Console.WriteLine($"User notification/email error: {userEx.Message}");
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ticket notification error: {ex.Message}");
    }
}

async Task SendTicketCommentNotifications(BiSoyle.Ticket.Service.Data.Ticket ticket, BiSoyle.Ticket.Service.Data.TicketComment comment, IServiceProvider serviceProvider)
{
    try
    {
        var userServiceUrl = Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? "http://localhost:5004";
        var emailServiceUrl = Environment.GetEnvironmentVariable("EMAIL_SERVICE_URL") ?? "http://localhost:5011";
        
        using var httpClient = new HttpClient();
        
        // Comment yapan kullanıcıyı al
        Dictionary<string, object>? commentUser = null;
        try
        {
            var commentUserResponse = await httpClient.GetAsync($"{userServiceUrl}/api/users/{comment.UserId}");
            if (commentUserResponse.IsSuccessStatusCode)
            {
                var commentUserJson = await commentUserResponse.Content.ReadAsStringAsync();
                commentUser = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(commentUserJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                Console.WriteLine($"Ticket Service: Failed to get comment user {comment.UserId}, status: {commentUserResponse.StatusCode}");
                return; // Kullanıcı bilgisi alınamazsa notification gönderme
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ticket Service: Error getting comment user: {ex.Message}");
            return; // Hata durumunda notification gönderme
        }
        
        if (commentUser == null)
        {
            Console.WriteLine($"Ticket Service: Comment user is null for userId {comment.UserId}");
            return;
        }
        
        var commentUserRoles = commentUser.GetValueOrDefault("roles")?.ToString() ?? "";
        var isSuperAdmin = commentUserRoles.Contains("SuperAdmin", StringComparison.OrdinalIgnoreCase);
        
        if (isSuperAdmin)
        {
            // SuperAdmin yorum yaptı, ticket sahibine bildirim gönder
            var ticketCreatorResponse = await httpClient.GetAsync($"{userServiceUrl}/api/users/{ticket.CreatedByUserId}");
            if (ticketCreatorResponse.IsSuccessStatusCode)
            {
                var ticketCreatorJson = await ticketCreatorResponse.Content.ReadAsStringAsync();
                var ticketCreator = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ticketCreatorJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                var userId = Convert.ToInt32(ticketCreator?["id"]?.ToString());
                var email = ticketCreator?["email"]?.ToString();
                var firstName = ticketCreator?["firstName"]?.ToString() ?? "";
                var lastName = ticketCreator?["lastName"]?.ToString() ?? "";
                
                // Notification
                var notification = new BiSoyle.Ticket.Service.Data.Notification
                {
                    TenantId = ticket.TenantId,
                    UserId = userId,
                    Type = "TicketReplied",
                    Title = "Ticket'inize Yanıt Verildi",
                    Message = $"'{ticket.Title}' ticket'ınıza yanıt verildi.",
                    RelatedTicketId = ticket.Id,
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                };
                
                // Notification'ı kaydetmek için yeni bir scope oluştur
                using (var serviceScope = serviceProvider.CreateScope())
                {
                    var db = serviceScope.ServiceProvider.GetRequiredService<TicketDbContext>();
                    db.Notifications.Add(notification);
                    await db.SaveChangesAsync();
                }
                
                // Email
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var emailBody = $@"
                        <h2>Ticket'inize Yanıt Verildi: {ticket.Title}</h2>
                        <p>Sayın {firstName} {lastName},</p>
                        <p>Ticket'ınıza yanıt verildi:</p>
                        <p><strong>Yanıt:</strong> {comment.Comment}</p>
                        <p>Detaylar için lütfen sisteme giriş yapın.</p>
                    ";
                    
                    var emailRequest = new
                    {
                        To = email,
                        Subject = $"Ticket'inize Yanıt Verildi: {ticket.Title}",
                        Body = emailBody,
                        IsHtml = true
                    };
                    
                    var emailJson = System.Text.Json.JsonSerializer.Serialize(emailRequest);
                    var emailContent = new StringContent(emailJson, Encoding.UTF8, "application/json");
                    
                    await httpClient.PostAsync($"{emailServiceUrl}/api/email/send", emailContent);
                    Console.WriteLine($"Ticket reply email sent to: {email}");
                }
            }
        }
        else
        {
            // Admin yorum yaptı, SuperAdmin'lere bildirim gönder
            var usersResponse = await httpClient.GetAsync($"{userServiceUrl}/api/users?role=SuperAdmin");
            if (usersResponse.IsSuccessStatusCode)
            {
                var usersJson = await usersResponse.Content.ReadAsStringAsync();
                var users = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(usersJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (users != null)
                {
                    foreach (var user in users)
                    {
                        try
                        {
                            var userId = Convert.ToInt32(user.GetType().GetProperty("Id")?.GetValue(user));
                            var email = user.GetType().GetProperty("Email")?.GetValue(user)?.ToString();
                            var firstName = user.GetType().GetProperty("FirstName")?.GetValue(user)?.ToString() ?? "";
                            var lastName = user.GetType().GetProperty("LastName")?.GetValue(user)?.ToString() ?? "";
                            
                            // Notification
                            var notification = new BiSoyle.Ticket.Service.Data.Notification
                            {
                                TenantId = 0,
                                UserId = userId,
                                Type = "TicketCommented",
                                Title = "Ticket'a Yorum Yapıldı",
                                Message = $"'{ticket.Title}' ticket'ına yorum yapıldı.",
                                RelatedTicketId = ticket.Id,
                                IsRead = false,
                                CreatedDate = DateTime.UtcNow
                            };
                            
                            // Notification'ı kaydetmek için yeni bir scope oluştur
                            using (var serviceScope = serviceProvider.CreateScope())
                            {
                                var db = serviceScope.ServiceProvider.GetRequiredService<TicketDbContext>();
                                db.Notifications.Add(notification);
                                await db.SaveChangesAsync();
                            }
                            
                            // Email
                            if (!string.IsNullOrWhiteSpace(email))
                            {
                                var emailBody = $@"
                                    <h2>Ticket'a Yorum Yapıldı: {ticket.Title}</h2>
                                    <p>Sayın {firstName} {lastName},</p>
                                    <p>Ticket'a yorum yapıldı:</p>
                                    <p><strong>Yorum:</strong> {comment.Comment}</p>
                                    <p>Detaylar için lütfen sisteme giriş yapın.</p>
                                ";
                                
                                var emailRequest = new
                                {
                                    To = email,
                                    Subject = $"Ticket'a Yorum Yapıldı: {ticket.Title}",
                                    Body = emailBody,
                                    IsHtml = true
                                };
                                
                                var emailJson = System.Text.Json.JsonSerializer.Serialize(emailRequest);
                                var emailContent = new StringContent(emailJson, Encoding.UTF8, "application/json");
                                
                                await httpClient.PostAsync($"{emailServiceUrl}/api/email/send", emailContent);
                                Console.WriteLine($"Ticket comment email sent to: {email}");
                            }
                        }
                        catch (Exception userEx)
                        {
                            Console.WriteLine($"User notification/email error: {userEx.Message}");
                        }
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ticket comment notification error: {ex.Message}");
    }
}

app.Run();



