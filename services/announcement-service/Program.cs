using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BiSoyle.Announcement.Service.Data;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BiSoyle - Announcement Service API",
        Version = "v1",
        Description = "Multi-Tenant Announcement Management API<br/><strong>BY HOSTEAGLE INFORMATION TECHNOLOGIES</strong>",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HostEagle Information Technologies",
            Email = "info@hosteagle.com"
        }
    });
});

// Database
builder.Services.AddDbContext<AnnouncementDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:4200", "http://localhost:3000", "http://127.0.0.1:4200")
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

var app = builder.Build();

// Set port for Announcement Service (5008)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5008";
app.Urls.Add($"http://localhost:{port}");

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnnouncementDbContext>();
    try
    {
        // Check if announcements table exists
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }
        
        bool tableExists = false;
        using (var checkCommand = connection.CreateCommand())
        {
            checkCommand.CommandText = @"
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'announcements'
                );
            ";
            var result = checkCommand.ExecuteScalar();
            tableExists = result != null && Convert.ToBoolean(result);
        }
        
        if (tableExists)
        {
            // Table exists, check if migration history has the migration
            using (var historyCommand = connection.CreateCommand())
            {
                historyCommand.CommandText = @"
                    SELECT COUNT(*) FROM ""__EFMigrationsHistory"" 
                    WHERE ""MigrationId"" = '20251116212753_InitialCreate';
                ";
                var count = Convert.ToInt32(historyCommand.ExecuteScalar());
                
                if (count == 0)
                {
                    // Add migration to history without running it
                    using (var insertCommand = connection.CreateCommand())
                    {
                        insertCommand.CommandText = @"
                            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                            VALUES ('20251116212753_InitialCreate', '8.0.0');
                        ";
                        insertCommand.ExecuteNonQuery();
                        Console.WriteLine("Migration history updated (table already exists)");
                    }
                }
            }
        }
        else
        {
            // Table doesn't exist, run migration normally
            db.Database.Migrate();
        }
        
        if (connection.State == System.Data.ConnectionState.Open)
        {
            connection.Close();
        }
    }
    catch (Exception ex)
    {
        // Only log if it's not a "table already exists" error
        if (!ex.Message.Contains("already exists") && !ex.Message.Contains("42P07"))
        {
            Console.WriteLine($"Database migration error: {ex.Message}");
        }
    }
    
    // Announcements tablosunu manuel olarak oluştur (eğer yoksa)
    try
    {
        var connection2 = db.Database.GetDbConnection();
        if (connection2.State != System.Data.ConnectionState.Open)
        {
            connection2.Open();
        }
        try
        {
            using (var command = connection2.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS announcements (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Title"" VARCHAR(200) NOT NULL,
                        ""Message"" VARCHAR(2000) NOT NULL,
                        ""TenantId"" INTEGER,
                        ""CreatedByUserId"" INTEGER NOT NULL,
                        ""Priority"" VARCHAR(50) DEFAULT 'Normal',
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""ExpiryDate"" TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_announcements_TenantId"" ON announcements (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_announcements_IsActive"" ON announcements (""IsActive"");
                    CREATE INDEX IF NOT EXISTS ""IX_announcements_CreatedDate"" ON announcements (""CreatedDate"");
                    
                    CREATE TABLE IF NOT EXISTS announcement_reads (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""AnnouncementId"" INTEGER NOT NULL,
                        ""UserId"" INTEGER NOT NULL,
                        ""ReadDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT ""FK_announcement_reads_announcements"" FOREIGN KEY (""AnnouncementId"") REFERENCES announcements(""Id"") ON DELETE CASCADE,
                        CONSTRAINT ""UQ_announcement_reads"" UNIQUE (""AnnouncementId"", ""UserId"")
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_announcement_reads_UserId"" ON announcement_reads (""UserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_announcement_reads_ReadDate"" ON announcement_reads (""ReadDate"");
                ";
                command.ExecuteNonQuery();
                Console.WriteLine("Announcements and announcement_reads tables created successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Announcements table creation error (may already exist): {ex.Message}");
        }
        finally
        {
            if (connection2.State == System.Data.ConnectionState.Open)
            {
                connection2.Close();
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Announcements table setup error: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// =====================================================
// ANNOUNCEMENT ENDPOINTS
// =====================================================

// Get all announcements
app.MapGet("/api/announcements", async (AnnouncementDbContext dbContext, int? tenantId, bool? isActive, bool? allTenants) =>
{
    try
    {
        var query = dbContext.Announcements.AsQueryable();
        
        // allTenants=true ise (SuperAdmin için) tüm duyuruları getir
        if (allTenants == true)
        {
            // Tüm duyuruları getir (hem tenantId'li hem de tenantId null olanlar)
            // TenantId filtresi uygulanmaz
        }
        else if (tenantId.HasValue)
        {
            // Hem null (tüm firmalara) hem de belirli tenantId'ye ait duyuruları getir
            query = query.Where(a => a.TenantId == null || a.TenantId == tenantId.Value);
        }
        else
        {
            // TenantId belirtilmemişse sadece tüm firmalara olan duyuruları getir
            query = query.Where(a => a.TenantId == null);
        }
        
        // IsActive filtresi - sadece explicit olarak true/false gönderilirse filtrele
        if (isActive.HasValue)
        {
            query = query.Where(a => a.IsActive == isActive.Value);
        }
        // isActive belirtilmemişse tüm duyuruları getir (aktif ve pasif)
        
        // ExpiryDate kontrolü - süresi dolmuş duyuruları filtrele (sadece aktif duyurular için)
        var now = DateTime.UtcNow;
        query = query.Where(a => !a.ExpiryDate.HasValue || a.ExpiryDate.Value >= now);
        
        var announcements = await query.OrderByDescending(a => a.CreatedDate).ToListAsync();
        return Results.Ok(announcements);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Announcements get error: {ex.Message}");
        return Results.Problem($"Duyurular getirme hatası: {ex.Message}");
    }
});

// Get announcement by ID
app.MapGet("/api/announcements/{id}", async (int id, AnnouncementDbContext dbContext) =>
{
    var announcement = await dbContext.Announcements.FindAsync(id);
    
    if (announcement == null)
        return Results.NotFound(new { message = "Duyuru bulunamadı" });
    
    return Results.Ok(announcement);
});

// Create announcement (SuperAdmin only)
app.MapPost("/api/announcements", async (HttpContext context, AnnouncementDbContext dbContext) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Console.WriteLine($"Received announcement data: {body}");
        
        var announcement = System.Text.Json.JsonSerializer.Deserialize<BiSoyle.Announcement.Service.Data.Announcement>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (announcement == null)
        {
            Console.WriteLine("Announcement deserialization failed");
            return Results.BadRequest(new { detail = "Geçersiz duyuru verisi" });
        }
        
        Console.WriteLine($"Deserialized announcement - Title: {announcement.Title}, Message: {announcement.Message}, CreatedByUserId: {announcement.CreatedByUserId}");
        
        if (string.IsNullOrWhiteSpace(announcement.Title))
            return Results.BadRequest(new { detail = "Duyuru başlığı gereklidir" });
        
        if (string.IsNullOrWhiteSpace(announcement.Message))
            return Results.BadRequest(new { detail = "Duyuru mesajı gereklidir" });
        
        if (announcement.CreatedByUserId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz CreatedByUserId" });
        
        // Priority varsayılan değer
        if (string.IsNullOrWhiteSpace(announcement.Priority))
            announcement.Priority = "Normal";
        
        announcement.CreatedDate = DateTime.UtcNow;
        announcement.IsActive = true;
        
        dbContext.Announcements.Add(announcement);
        await dbContext.SaveChangesAsync();
        
        Console.WriteLine($"Announcement saved successfully with ID: {announcement.Id}");
        
        // Email gönderimi için RabbitMQ veya HTTP isteği (async olarak)
        _ = Task.Run(async () =>
        {
            try
            {
                await SendAnnouncementEmails(announcement);
            }
            catch (Exception emailEx)
            {
                Console.WriteLine($"Email gönderme hatası: {emailEx.Message}");
            }
        });
        
        return Results.Created($"/api/announcements/{announcement.Id}", announcement);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Announcement save error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem($"Duyuru kaydetme hatası: {ex.Message}");
    }
});

// Helper method to send announcement emails
async Task SendAnnouncementEmails(BiSoyle.Announcement.Service.Data.Announcement announcement)
{
    try
    {
        // User service'den admin kullanıcılarını al
        var userServiceUrl = Environment.GetEnvironmentVariable("USER_SERVICE_URL") ?? "http://localhost:5004";
        var emailServiceUrl = Environment.GetEnvironmentVariable("EMAIL_SERVICE_URL") ?? "http://localhost:5011";
        
        using var httpClient = new HttpClient();
        
        // Duyurunun hedeflediği firmaları belirle
        List<int> targetTenantIds = new List<int>();
        
        if (announcement.TenantId.HasValue)
        {
            // Belirli bir firmaya gönderiliyor
            targetTenantIds.Add(announcement.TenantId.Value);
        }
        else
        {
            // Tüm firmalara gönderiliyor - tenant service'den tüm firmaları al
            var tenantServiceUrl = Environment.GetEnvironmentVariable("TENANT_SERVICE_URL") ?? "http://localhost:5006";
            try
            {
                var tenantsResponse = await httpClient.GetAsync($"{tenantServiceUrl}/api/tenants");
                if (tenantsResponse.IsSuccessStatusCode)
                {
                    var tenantsJson = await tenantsResponse.Content.ReadAsStringAsync();
                    var tenants = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(tenantsJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (tenants != null)
                    {
                        foreach (var tenant in tenants)
                        {
                            if (tenant.GetType().GetProperty("Id") != null)
                            {
                                var tenantId = Convert.ToInt32(tenant.GetType().GetProperty("Id").GetValue(tenant));
                                targetTenantIds.Add(tenantId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tenant listesi alınamadı: {ex.Message}");
            }
        }
        
        // Her firma için Admin rolündeki kullanıcıları bul ve email gönder
        foreach (var tenantId in targetTenantIds)
        {
            try
            {
                var usersResponse = await httpClient.GetAsync($"{userServiceUrl}/api/users?tenantId={tenantId}&role=Admin");
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
                                var email = user.GetType().GetProperty("Email")?.GetValue(user)?.ToString();
                                var firstName = user.GetType().GetProperty("FirstName")?.GetValue(user)?.ToString() ?? "";
                                var lastName = user.GetType().GetProperty("LastName")?.GetValue(user)?.ToString() ?? "";
                                
                                if (!string.IsNullOrWhiteSpace(email))
                                {
                                    var emailBody = $@"
                                        <h2>Yeni Duyuru: {announcement.Title}</h2>
                                        <p>Sayın {firstName} {lastName},</p>
                                        <p>{announcement.Message}</p>
                                        <p><strong>Öncelik:</strong> {announcement.Priority}</p>
                                        <p><strong>Tarih:</strong> {announcement.CreatedDate:dd.MM.yyyy HH:mm}</p>
                                        <p>Detaylar için lütfen sisteme giriş yapın.</p>
                                    ";
                                    
                                    var emailRequest = new
                                    {
                                        To = email,
                                        Subject = $"Yeni Duyuru: {announcement.Title}",
                                        Body = emailBody,
                                        IsHtml = true
                                    };
                                    
                                    var emailJson = System.Text.Json.JsonSerializer.Serialize(emailRequest);
                                    var emailContent = new StringContent(emailJson, System.Text.Encoding.UTF8, "application/json");
                                    
                                    await httpClient.PostAsync($"{emailServiceUrl}/api/email/send", emailContent);
                                    Console.WriteLine($"Email gönderildi: {email}");
                                }
                            }
                            catch (Exception userEx)
                            {
                                Console.WriteLine($"Kullanıcı email gönderme hatası: {userEx.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception tenantEx)
            {
                Console.WriteLine($"Tenant {tenantId} için email gönderme hatası: {tenantEx.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Email gönderme genel hatası: {ex.Message}");
    }
}

// Update announcement
app.MapPut("/api/announcements/{id}", async (int id, HttpContext context, AnnouncementDbContext dbContext) =>
{
    try
    {
        var existingAnnouncement = await dbContext.Announcements.FindAsync(id);
        if (existingAnnouncement == null)
            return Results.NotFound(new { message = "Duyuru bulunamadı" });
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var announcement = System.Text.Json.JsonSerializer.Deserialize<BiSoyle.Announcement.Service.Data.Announcement>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (announcement == null)
            return Results.BadRequest(new { detail = "Geçersiz duyuru verisi" });
        
        if (string.IsNullOrWhiteSpace(announcement.Title))
            return Results.BadRequest(new { detail = "Duyuru başlığı gereklidir" });
        
        if (string.IsNullOrWhiteSpace(announcement.Message))
            return Results.BadRequest(new { detail = "Duyuru mesajı gereklidir" });
        
        // Güncelleme
        existingAnnouncement.Title = announcement.Title;
        existingAnnouncement.Message = announcement.Message;
        existingAnnouncement.TenantId = announcement.TenantId;
        existingAnnouncement.Priority = announcement.Priority;
        existingAnnouncement.IsActive = announcement.IsActive;
        existingAnnouncement.ExpiryDate = announcement.ExpiryDate;
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(existingAnnouncement);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Announcement update error: {ex.Message}");
        return Results.Problem($"Duyuru güncelleme hatası: {ex.Message}");
    }
});

// Delete announcement
app.MapDelete("/api/announcements/{id}", async (int id, AnnouncementDbContext dbContext) =>
{
    var announcement = await dbContext.Announcements.FindAsync(id);
    if (announcement == null)
        return Results.NotFound(new { message = "Duyuru bulunamadı" });
    
    dbContext.Announcements.Remove(announcement);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Duyuru silindi" });
});

// Get unread announcement count for user
app.MapGet("/api/announcements/unread-count", async (AnnouncementDbContext dbContext, int userId, int? tenantId) =>
{
    try
    {
        var query = dbContext.Announcements.AsQueryable();
        
        // TenantId filtresi
        if (tenantId.HasValue)
        {
            query = query.Where(a => a.TenantId == null || a.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(a => a.TenantId == null);
        }
        
        // Sadece aktif duyurular
        query = query.Where(a => a.IsActive == true);
        
        // ExpiryDate kontrolü
        var now = DateTime.UtcNow;
        query = query.Where(a => !a.ExpiryDate.HasValue || a.ExpiryDate.Value >= now);
        
        // Kullanıcının okuduğu duyuruları al
        var readAnnouncementIds = await dbContext.AnnouncementReads
            .Where(ar => ar.UserId == userId)
            .Select(ar => ar.AnnouncementId)
            .ToListAsync();
        
        // Okunmamış duyuru sayısı
        var unreadCount = await query
            .Where(a => !readAnnouncementIds.Contains(a.Id))
            .CountAsync();
        
        return Results.Ok(new { count = unreadCount });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unread count error: {ex.Message}");
        return Results.Problem($"Okunmamış duyuru sayısı getirme hatası: {ex.Message}");
    }
});

// Mark announcement as read
app.MapPost("/api/announcements/{id}/mark-read", async (int id, HttpContext context, AnnouncementDbContext dbContext) =>
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
        
        var announcement = await dbContext.Announcements.FindAsync(id);
        if (announcement == null)
            return Results.NotFound(new { message = "Duyuru bulunamadı" });
        
        // Zaten okunmuş mu kontrol et
        var existingRead = await dbContext.AnnouncementReads
            .FirstOrDefaultAsync(ar => ar.AnnouncementId == id && ar.UserId == userId);
        
        if (existingRead == null)
        {
            var announcementRead = new BiSoyle.Announcement.Service.Data.AnnouncementRead
            {
                AnnouncementId = id,
                UserId = userId,
                ReadDate = DateTime.UtcNow
            };
            
            dbContext.AnnouncementReads.Add(announcementRead);
            await dbContext.SaveChangesAsync();
        }
        
        return Results.Ok(new { message = "Duyuru okundu olarak işaretlendi" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Mark as read error: {ex.Message}");
        return Results.Problem($"Duyuru okundu işaretleme hatası: {ex.Message}");
    }
});

app.Run();


