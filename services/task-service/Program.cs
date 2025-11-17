using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using BiSoyle.Task.Service.Data;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BiSoyle - Task Management Service API",
        Version = "v1",
        Description = "Multi-Tenant Task Management API<br/><strong>BY HOSTEAGLE INFORMATION TECHNOLOGIES</strong>",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HostEagle Information Technologies",
            Email = "info@hosteagle.com"
        }
    });
});

// Database
builder.Services.AddDbContext<TaskDbContext>(options =>
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

// Set port for Task Service (5007)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5007";
app.Urls.Add($"http://localhost:{port}");

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration error: {ex.Message}");
    }
    
    // Tasks tablosunu manuel olarak oluştur (eğer yoksa)
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
                    CREATE TABLE IF NOT EXISTS tasks (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INTEGER NOT NULL,
                        ""CreatedByUserId"" INTEGER NOT NULL,
                        ""AssignedToUserId"" INTEGER NOT NULL,
                        ""Title"" VARCHAR(200) NOT NULL,
                        ""Description"" VARCHAR(2000),
                        ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Pending',
                        ""Priority"" VARCHAR(50) DEFAULT 'Medium',
                        ""DueDate"" TIMESTAMP,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_tasks_TenantId"" ON tasks (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_tasks_AssignedToUserId"" ON tasks (""AssignedToUserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_tasks_Status"" ON tasks (""Status"");
                    
                    CREATE TABLE IF NOT EXISTS task_comments (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TaskId"" INTEGER NOT NULL,
                        ""UserId"" INTEGER NOT NULL,
                        ""Comment"" VARCHAR(1000) NOT NULL,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT ""FK_task_comments_tasks_TaskId"" FOREIGN KEY (""TaskId"") REFERENCES tasks (""Id"") ON DELETE CASCADE
                    );
                    
                    CREATE TABLE IF NOT EXISTS notifications (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INTEGER NOT NULL,
                        ""UserId"" INTEGER NOT NULL,
                        ""Type"" VARCHAR(50) NOT NULL,
                        ""Title"" VARCHAR(200) NOT NULL,
                        ""Message"" VARCHAR(500),
                        ""RelatedTaskId"" INTEGER,
                        ""IsRead"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_UserId"" ON notifications (""UserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_TenantId"" ON notifications (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_IsRead"" ON notifications (""IsRead"");
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_CreatedDate"" ON notifications (""CreatedDate"");
                ";
                command.ExecuteNonQuery();
                Console.WriteLine("Tasks tables created successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tasks table creation error (may already exist): {ex.Message}");
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
        Console.WriteLine($"Tasks table setup error: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// =====================================================
// TASK ENDPOINTS
// =====================================================

// Get all tasks
app.MapGet("/api/tasks", async (TaskDbContext dbContext, int? tenantId, int? assignedToUserId, int? createdByUserId, string? status) =>
{
    try
    {
        var query = dbContext.Tasks.Include(t => t.Comments).AsQueryable();
        
        if (tenantId.HasValue && tenantId.Value > 0)
            query = query.Where(t => t.TenantId == tenantId.Value);
        
        if (assignedToUserId.HasValue && assignedToUserId.Value > 0)
            query = query.Where(t => t.AssignedToUserId == assignedToUserId.Value);
        
        if (createdByUserId.HasValue && createdByUserId.Value > 0)
            query = query.Where(t => t.CreatedByUserId == createdByUserId.Value);
        
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);
        
        var tasks = await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
        return Results.Ok(tasks);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Tasks get error: {ex.Message}");
        return Results.Problem($"Görevler getirme hatası: {ex.Message}");
    }
});

// Get task by ID
app.MapGet("/api/tasks/{id}", async (int id, TaskDbContext dbContext) =>
{
    var task = await dbContext.Tasks
        .Include(t => t.Comments)
        .FirstOrDefaultAsync(t => t.Id == id);
    
    if (task == null)
        return Results.NotFound(new { message = "Görev bulunamadı" });
    
    return Results.Ok(task);
});

// Create task
app.MapPost("/api/tasks", async (HttpContext context, TaskDbContext dbContext) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var task = System.Text.Json.JsonSerializer.Deserialize<BiSoyle.Task.Service.Data.Task>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (task == null)
            return Results.BadRequest(new { detail = "Geçersiz görev verisi" });
        
        if (string.IsNullOrWhiteSpace(task.Title))
            return Results.BadRequest(new { detail = "Görev başlığı gereklidir" });
        
        if (task.TenantId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz TenantId" });
        
        if (task.CreatedByUserId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz CreatedByUserId" });
        
        if (task.AssignedToUserId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz AssignedToUserId" });
        
        // Status ve Priority varsayılan değerler
        if (string.IsNullOrWhiteSpace(task.Status))
            task.Status = "Pending";
        
        if (string.IsNullOrWhiteSpace(task.Priority))
            task.Priority = "Medium";
        
        task.CreatedDate = DateTime.UtcNow;
        task.UpdatedDate = DateTime.UtcNow;
        
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();
        
        return Results.Created($"/api/tasks/{task.Id}", task);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Task save error: {ex.Message}");
        return Results.Problem($"Görev kaydetme hatası: {ex.Message}");
    }
});

// Update task
app.MapPut("/api/tasks/{id}", async (int id, HttpContext context, TaskDbContext dbContext) =>
{
    try
    {
        var existingTask = await dbContext.Tasks.FindAsync(id);
        if (existingTask == null)
            return Results.NotFound(new { message = "Görev bulunamadı" });
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var task = System.Text.Json.JsonSerializer.Deserialize<BiSoyle.Task.Service.Data.Task>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (task == null)
            return Results.BadRequest(new { detail = "Geçersiz görev verisi" });
        
        if (string.IsNullOrWhiteSpace(task.Title))
            return Results.BadRequest(new { detail = "Görev başlığı gereklidir" });
        
        // Güncelleme
        existingTask.Title = task.Title;
        existingTask.Description = task.Description;
        existingTask.Status = task.Status;
        existingTask.Priority = task.Priority;
        existingTask.DueDate = task.DueDate;
        existingTask.AssignedToUserId = task.AssignedToUserId;
        existingTask.UpdatedDate = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(existingTask);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Task update error: {ex.Message}");
        return Results.Problem($"Görev güncelleme hatası: {ex.Message}");
    }
});

// Update task status (Accept, Start, Complete)
app.MapPatch("/api/tasks/{id}/status", async (int id, HttpContext context, TaskDbContext dbContext) =>
{
    try
    {
        var task = await dbContext.Tasks.FindAsync(id);
        if (task == null)
            return Results.NotFound(new { message = "Görev bulunamadı" });
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var statusUpdate = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body);
        
        if (!statusUpdate.TryGetProperty("status", out var statusElement))
            return Results.BadRequest(new { detail = "Status gereklidir" });
        
        var newStatus = statusElement.GetString();
        if (string.IsNullOrWhiteSpace(newStatus))
            return Results.BadRequest(new { detail = "Status gereklidir" });
        
        // Geçerli status değerleri
        var validStatuses = new[] { "Pending", "Accepted", "InProgress", "Completed" };
        if (!validStatuses.Contains(newStatus))
            return Results.BadRequest(new { detail = $"Geçersiz status. Geçerli değerler: {string.Join(", ", validStatuses)}" });
        
        var oldStatus = task.Status;
        task.Status = newStatus;
        task.UpdatedDate = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync();
        
        // Bildirim oluştur: Accepted veya Completed durumunda admin'e bildirim gönder
        if ((newStatus == "Accepted" || newStatus == "Completed") && oldStatus != newStatus)
        {
            try
            {
                var notification = new BiSoyle.Task.Service.Data.Notification
                {
                    TenantId = task.TenantId,
                    UserId = task.CreatedByUserId, // Görevi oluşturan admin
                    Type = newStatus == "Accepted" ? "TaskAccepted" : "TaskCompleted",
                    Title = newStatus == "Accepted" ? "Görev Kabul Edildi" : "Görev Tamamlandı",
                    Message = $"'{task.Title}' görevi {(newStatus == "Accepted" ? "kabul edildi" : "tamamlandı")}.",
                    RelatedTaskId = task.Id,
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                };
                dbContext.Notifications.Add(notification);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception notifEx)
            {
                Console.WriteLine($"Notification creation error: {notifEx.Message}");
                // Bildirim hatası görevi etkilemesin
            }
        }
        
        return Results.Ok(task);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Task status update error: {ex.Message}");
        return Results.Problem($"Görev durumu güncelleme hatası: {ex.Message}");
    }
});

// Delete task
app.MapDelete("/api/tasks/{id}", async (int id, TaskDbContext dbContext) =>
{
    var task = await dbContext.Tasks.FindAsync(id);
    if (task == null)
        return Results.NotFound(new { message = "Görev bulunamadı" });
    
    dbContext.Tasks.Remove(task);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Görev silindi" });
});

// =====================================================
// TASK COMMENT ENDPOINTS
// =====================================================

// Get comments for a task
app.MapGet("/api/tasks/{taskId}/comments", async (int taskId, TaskDbContext dbContext) =>
{
    var comments = await dbContext.TaskComments
        .Where(c => c.TaskId == taskId)
        .OrderBy(c => c.CreatedDate)
        .ToListAsync();
    
    return Results.Ok(comments);
});

// Add comment to task
app.MapPost("/api/tasks/{taskId}/comments", async (int taskId, HttpContext context, TaskDbContext dbContext) =>
{
    try
    {
        var task = await dbContext.Tasks.FindAsync(taskId);
        if (task == null)
            return Results.NotFound(new { message = "Görev bulunamadı" });
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var commentData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body);
        
        if (!commentData.TryGetProperty("userId", out var userIdElement) || 
            !commentData.TryGetProperty("comment", out var commentElement))
            return Results.BadRequest(new { detail = "userId ve comment gereklidir" });
        
        var userId = userIdElement.GetInt32();
        var commentText = commentElement.GetString();
        
        if (userId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz userId" });
        
        if (string.IsNullOrWhiteSpace(commentText))
            return Results.BadRequest(new { detail = "Yorum boş olamaz" });
        
        var comment = new TaskComment
        {
            TaskId = taskId,
            UserId = userId,
            Comment = commentText,
            CreatedDate = DateTime.UtcNow
        };
        
        dbContext.TaskComments.Add(comment);
        await dbContext.SaveChangesAsync();
        
        return Results.Created($"/api/tasks/{taskId}/comments/{comment.Id}", comment);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Comment save error: {ex.Message}");
        return Results.Problem($"Yorum kaydetme hatası: {ex.Message}");
    }
});

// Delete comment
app.MapDelete("/api/tasks/{taskId}/comments/{commentId}", async (int taskId, int commentId, TaskDbContext dbContext) =>
{
    var comment = await dbContext.TaskComments
        .FirstOrDefaultAsync(c => c.Id == commentId && c.TaskId == taskId);
    
    if (comment == null)
        return Results.NotFound(new { message = "Yorum bulunamadı" });
    
    dbContext.TaskComments.Remove(comment);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Yorum silindi" });
});

// =====================================================
// NOTIFICATION ENDPOINTS
// =====================================================

// Get notifications for user
app.MapGet("/api/notifications", async (TaskDbContext dbContext, int? userId, int? tenantId, bool? isRead) =>
{
    try
    {
        var query = dbContext.Notifications.AsQueryable();
        
        if (userId.HasValue && userId.Value > 0)
            query = query.Where(n => n.UserId == userId.Value);
        
        if (tenantId.HasValue && tenantId.Value > 0)
            query = query.Where(n => n.TenantId == tenantId.Value);
        
        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);
        
        var notifications = await query.OrderByDescending(n => n.CreatedDate).ToListAsync();
        return Results.Ok(notifications);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Notifications get error: {ex.Message}");
        return Results.Problem($"Bildirimler getirme hatası: {ex.Message}");
    }
});

// Get unread notification count
app.MapGet("/api/notifications/unread-count", async (TaskDbContext dbContext, int? userId, int? tenantId) =>
{
    try
    {
        var query = dbContext.Notifications.Where(n => !n.IsRead);
        
        if (userId.HasValue && userId.Value > 0)
            query = query.Where(n => n.UserId == userId.Value);
        
        if (tenantId.HasValue && tenantId.Value > 0)
            query = query.Where(n => n.TenantId == tenantId.Value);
        
        var count = await query.CountAsync();
        return Results.Ok(new { count });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unread count error: {ex.Message}");
        return Results.Problem($"Okunmamış bildirim sayısı hatası: {ex.Message}");
    }
});

// Mark notification as read
app.MapPatch("/api/notifications/{id}/read", async (int id, TaskDbContext dbContext) =>
{
    try
    {
        var notification = await dbContext.Notifications.FindAsync(id);
        if (notification == null)
            return Results.NotFound(new { message = "Bildirim bulunamadı" });
        
        notification.IsRead = true;
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(notification);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Notification read error: {ex.Message}");
        return Results.Problem($"Bildirim okundu işaretleme hatası: {ex.Message}");
    }
});

// Mark all notifications as read for user
app.MapPatch("/api/notifications/read-all", async (HttpContext context, TaskDbContext dbContext) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body);
        
        int? userId = null;
        int? tenantId = null;
        
        if (data.TryGetProperty("userId", out var userIdElement))
            userId = userIdElement.GetInt32();
        
        if (data.TryGetProperty("tenantId", out var tenantIdElement))
            tenantId = tenantIdElement.GetInt32();
        
        if (!userId.HasValue)
            return Results.BadRequest(new { detail = "userId gereklidir" });
        
        var query = dbContext.Notifications.Where(n => n.UserId == userId.Value && !n.IsRead);
        
        if (tenantId.HasValue)
            query = query.Where(n => n.TenantId == tenantId.Value);
        
        var notifications = await query.ToListAsync();
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(new { message = $"{notifications.Count} bildirim okundu olarak işaretlendi" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Mark all read error: {ex.Message}");
        return Results.Problem($"Tüm bildirimleri okundu işaretleme hatası: {ex.Message}");
    }
});

// Delete notification
app.MapDelete("/api/notifications/{id}", async (int id, TaskDbContext dbContext) =>
{
    var notification = await dbContext.Notifications.FindAsync(id);
    if (notification == null)
        return Results.NotFound(new { message = "Bildirim bulunamadı" });
    
    dbContext.Notifications.Remove(notification);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Bildirim silindi" });
});

app.Run();


using Microsoft.EntityFrameworkCore;
using BiSoyle.Task.Service.Data;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BiSoyle - Task Management Service API",
        Version = "v1",
        Description = "Multi-Tenant Task Management API<br/><strong>BY HOSTEAGLE INFORMATION TECHNOLOGIES</strong>",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "HostEagle Information Technologies",
            Email = "info@hosteagle.com"
        }
    });
});

// Database
builder.Services.AddDbContext<TaskDbContext>(options =>
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

// Set port for Task Service (5007)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5007";
app.Urls.Add($"http://localhost:{port}");

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration error: {ex.Message}");
    }
    
    // Tasks tablosunu manuel olarak oluştur (eğer yoksa)
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
                    CREATE TABLE IF NOT EXISTS tasks (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INTEGER NOT NULL,
                        ""CreatedByUserId"" INTEGER NOT NULL,
                        ""AssignedToUserId"" INTEGER NOT NULL,
                        ""Title"" VARCHAR(200) NOT NULL,
                        ""Description"" VARCHAR(2000),
                        ""Status"" VARCHAR(50) NOT NULL DEFAULT 'Pending',
                        ""Priority"" VARCHAR(50) DEFAULT 'Medium',
                        ""DueDate"" TIMESTAMP,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_tasks_TenantId"" ON tasks (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_tasks_AssignedToUserId"" ON tasks (""AssignedToUserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_tasks_Status"" ON tasks (""Status"");
                    
                    CREATE TABLE IF NOT EXISTS task_comments (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TaskId"" INTEGER NOT NULL,
                        ""UserId"" INTEGER NOT NULL,
                        ""Comment"" VARCHAR(1000) NOT NULL,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT ""FK_task_comments_tasks_TaskId"" FOREIGN KEY (""TaskId"") REFERENCES tasks (""Id"") ON DELETE CASCADE
                    );
                    
                    CREATE TABLE IF NOT EXISTS notifications (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""TenantId"" INTEGER NOT NULL,
                        ""UserId"" INTEGER NOT NULL,
                        ""Type"" VARCHAR(50) NOT NULL,
                        ""Title"" VARCHAR(200) NOT NULL,
                        ""Message"" VARCHAR(500),
                        ""RelatedTaskId"" INTEGER,
                        ""IsRead"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""CreatedDate"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_UserId"" ON notifications (""UserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_TenantId"" ON notifications (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_IsRead"" ON notifications (""IsRead"");
                    CREATE INDEX IF NOT EXISTS ""IX_notifications_CreatedDate"" ON notifications (""CreatedDate"");
                ";
                command.ExecuteNonQuery();
                Console.WriteLine("Tasks tables created successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tasks table creation error (may already exist): {ex.Message}");
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
        Console.WriteLine($"Tasks table setup error: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// =====================================================
// TASK ENDPOINTS
// =====================================================

// Get all tasks
app.MapGet("/api/tasks", async (TaskDbContext dbContext, int? tenantId, int? assignedToUserId, int? createdByUserId, string? status) =>
{
    try
    {
        var query = dbContext.Tasks.Include(t => t.Comments).AsQueryable();
        
        if (tenantId.HasValue && tenantId.Value > 0)
            query = query.Where(t => t.TenantId == tenantId.Value);
        
        if (assignedToUserId.HasValue && assignedToUserId.Value > 0)
            query = query.Where(t => t.AssignedToUserId == assignedToUserId.Value);
        
        if (createdByUserId.HasValue && createdByUserId.Value > 0)
            query = query.Where(t => t.CreatedByUserId == createdByUserId.Value);
        
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);
        
        var tasks = await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
        return Results.Ok(tasks);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Tasks get error: {ex.Message}");
        return Results.Problem($"Görevler getirme hatası: {ex.Message}");
    }
});

// Get task by ID
app.MapGet("/api/tasks/{id}", async (int id, TaskDbContext dbContext) =>
{
    var task = await dbContext.Tasks
        .Include(t => t.Comments)
        .FirstOrDefaultAsync(t => t.Id == id);
    
    if (task == null)
        return Results.NotFound(new { message = "Görev bulunamadı" });
    
    return Results.Ok(task);
});

// Create task
app.MapPost("/api/tasks", async (HttpContext context, TaskDbContext dbContext) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var task = System.Text.Json.JsonSerializer.Deserialize<BiSoyle.Task.Service.Data.Task>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (task == null)
            return Results.BadRequest(new { detail = "Geçersiz görev verisi" });
        
        if (string.IsNullOrWhiteSpace(task.Title))
            return Results.BadRequest(new { detail = "Görev başlığı gereklidir" });
        
        if (task.TenantId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz TenantId" });
        
        if (task.CreatedByUserId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz CreatedByUserId" });
        
        if (task.AssignedToUserId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz AssignedToUserId" });
        
        // Status ve Priority varsayılan değerler
        if (string.IsNullOrWhiteSpace(task.Status))
            task.Status = "Pending";
        
        if (string.IsNullOrWhiteSpace(task.Priority))
            task.Priority = "Medium";
        
        task.CreatedDate = DateTime.UtcNow;
        task.UpdatedDate = DateTime.UtcNow;
        
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();
        
        return Results.Created($"/api/tasks/{task.Id}", task);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Task save error: {ex.Message}");
        return Results.Problem($"Görev kaydetme hatası: {ex.Message}");
    }
});

// Update task
app.MapPut("/api/tasks/{id}", async (int id, HttpContext context, TaskDbContext dbContext) =>
{
    try
    {
        var existingTask = await dbContext.Tasks.FindAsync(id);
        if (existingTask == null)
            return Results.NotFound(new { message = "Görev bulunamadı" });
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var task = System.Text.Json.JsonSerializer.Deserialize<BiSoyle.Task.Service.Data.Task>(body, new System.Text.Json.JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (task == null)
            return Results.BadRequest(new { detail = "Geçersiz görev verisi" });
        
        if (string.IsNullOrWhiteSpace(task.Title))
            return Results.BadRequest(new { detail = "Görev başlığı gereklidir" });
        
        // Güncelleme
        existingTask.Title = task.Title;
        existingTask.Description = task.Description;
        existingTask.Status = task.Status;
        existingTask.Priority = task.Priority;
        existingTask.DueDate = task.DueDate;
        existingTask.AssignedToUserId = task.AssignedToUserId;
        existingTask.UpdatedDate = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(existingTask);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Task update error: {ex.Message}");
        return Results.Problem($"Görev güncelleme hatası: {ex.Message}");
    }
});

// Update task status (Accept, Start, Complete)
app.MapPatch("/api/tasks/{id}/status", async (int id, HttpContext context, TaskDbContext dbContext) =>
{
    try
    {
        var task = await dbContext.Tasks.FindAsync(id);
        if (task == null)
            return Results.NotFound(new { message = "Görev bulunamadı" });
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var statusUpdate = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body);
        
        if (!statusUpdate.TryGetProperty("status", out var statusElement))
            return Results.BadRequest(new { detail = "Status gereklidir" });
        
        var newStatus = statusElement.GetString();
        if (string.IsNullOrWhiteSpace(newStatus))
            return Results.BadRequest(new { detail = "Status gereklidir" });
        
        // Geçerli status değerleri
        var validStatuses = new[] { "Pending", "Accepted", "InProgress", "Completed" };
        if (!validStatuses.Contains(newStatus))
            return Results.BadRequest(new { detail = $"Geçersiz status. Geçerli değerler: {string.Join(", ", validStatuses)}" });
        
        var oldStatus = task.Status;
        task.Status = newStatus;
        task.UpdatedDate = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync();
        
        // Bildirim oluştur: Accepted veya Completed durumunda admin'e bildirim gönder
        if ((newStatus == "Accepted" || newStatus == "Completed") && oldStatus != newStatus)
        {
            try
            {
                var notification = new BiSoyle.Task.Service.Data.Notification
                {
                    TenantId = task.TenantId,
                    UserId = task.CreatedByUserId, // Görevi oluşturan admin
                    Type = newStatus == "Accepted" ? "TaskAccepted" : "TaskCompleted",
                    Title = newStatus == "Accepted" ? "Görev Kabul Edildi" : "Görev Tamamlandı",
                    Message = $"'{task.Title}' görevi {(newStatus == "Accepted" ? "kabul edildi" : "tamamlandı")}.",
                    RelatedTaskId = task.Id,
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                };
                dbContext.Notifications.Add(notification);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception notifEx)
            {
                Console.WriteLine($"Notification creation error: {notifEx.Message}");
                // Bildirim hatası görevi etkilemesin
            }
        }
        
        return Results.Ok(task);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Task status update error: {ex.Message}");
        return Results.Problem($"Görev durumu güncelleme hatası: {ex.Message}");
    }
});

// Delete task
app.MapDelete("/api/tasks/{id}", async (int id, TaskDbContext dbContext) =>
{
    var task = await dbContext.Tasks.FindAsync(id);
    if (task == null)
        return Results.NotFound(new { message = "Görev bulunamadı" });
    
    dbContext.Tasks.Remove(task);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Görev silindi" });
});

// =====================================================
// TASK COMMENT ENDPOINTS
// =====================================================

// Get comments for a task
app.MapGet("/api/tasks/{taskId}/comments", async (int taskId, TaskDbContext dbContext) =>
{
    var comments = await dbContext.TaskComments
        .Where(c => c.TaskId == taskId)
        .OrderBy(c => c.CreatedDate)
        .ToListAsync();
    
    return Results.Ok(comments);
});

// Add comment to task
app.MapPost("/api/tasks/{taskId}/comments", async (int taskId, HttpContext context, TaskDbContext dbContext) =>
{
    try
    {
        var task = await dbContext.Tasks.FindAsync(taskId);
        if (task == null)
            return Results.NotFound(new { message = "Görev bulunamadı" });
        
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var commentData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body);
        
        if (!commentData.TryGetProperty("userId", out var userIdElement) || 
            !commentData.TryGetProperty("comment", out var commentElement))
            return Results.BadRequest(new { detail = "userId ve comment gereklidir" });
        
        var userId = userIdElement.GetInt32();
        var commentText = commentElement.GetString();
        
        if (userId <= 0)
            return Results.BadRequest(new { detail = "Geçersiz userId" });
        
        if (string.IsNullOrWhiteSpace(commentText))
            return Results.BadRequest(new { detail = "Yorum boş olamaz" });
        
        var comment = new TaskComment
        {
            TaskId = taskId,
            UserId = userId,
            Comment = commentText,
            CreatedDate = DateTime.UtcNow
        };
        
        dbContext.TaskComments.Add(comment);
        await dbContext.SaveChangesAsync();
        
        return Results.Created($"/api/tasks/{taskId}/comments/{comment.Id}", comment);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Comment save error: {ex.Message}");
        return Results.Problem($"Yorum kaydetme hatası: {ex.Message}");
    }
});

// Delete comment
app.MapDelete("/api/tasks/{taskId}/comments/{commentId}", async (int taskId, int commentId, TaskDbContext dbContext) =>
{
    var comment = await dbContext.TaskComments
        .FirstOrDefaultAsync(c => c.Id == commentId && c.TaskId == taskId);
    
    if (comment == null)
        return Results.NotFound(new { message = "Yorum bulunamadı" });
    
    dbContext.TaskComments.Remove(comment);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Yorum silindi" });
});

// =====================================================
// NOTIFICATION ENDPOINTS
// =====================================================

// Get notifications for user
app.MapGet("/api/notifications", async (TaskDbContext dbContext, int? userId, int? tenantId, bool? isRead) =>
{
    try
    {
        var query = dbContext.Notifications.AsQueryable();
        
        if (userId.HasValue && userId.Value > 0)
            query = query.Where(n => n.UserId == userId.Value);
        
        if (tenantId.HasValue && tenantId.Value > 0)
            query = query.Where(n => n.TenantId == tenantId.Value);
        
        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);
        
        var notifications = await query.OrderByDescending(n => n.CreatedDate).ToListAsync();
        return Results.Ok(notifications);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Notifications get error: {ex.Message}");
        return Results.Problem($"Bildirimler getirme hatası: {ex.Message}");
    }
});

// Get unread notification count
app.MapGet("/api/notifications/unread-count", async (TaskDbContext dbContext, int? userId, int? tenantId) =>
{
    try
    {
        var query = dbContext.Notifications.Where(n => !n.IsRead);
        
        if (userId.HasValue && userId.Value > 0)
            query = query.Where(n => n.UserId == userId.Value);
        
        if (tenantId.HasValue && tenantId.Value > 0)
            query = query.Where(n => n.TenantId == tenantId.Value);
        
        var count = await query.CountAsync();
        return Results.Ok(new { count });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unread count error: {ex.Message}");
        return Results.Problem($"Okunmamış bildirim sayısı hatası: {ex.Message}");
    }
});

// Mark notification as read
app.MapPatch("/api/notifications/{id}/read", async (int id, TaskDbContext dbContext) =>
{
    try
    {
        var notification = await dbContext.Notifications.FindAsync(id);
        if (notification == null)
            return Results.NotFound(new { message = "Bildirim bulunamadı" });
        
        notification.IsRead = true;
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(notification);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Notification read error: {ex.Message}");
        return Results.Problem($"Bildirim okundu işaretleme hatası: {ex.Message}");
    }
});

// Mark all notifications as read for user
app.MapPatch("/api/notifications/read-all", async (HttpContext context, TaskDbContext dbContext) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body);
        
        int? userId = null;
        int? tenantId = null;
        
        if (data.TryGetProperty("userId", out var userIdElement))
            userId = userIdElement.GetInt32();
        
        if (data.TryGetProperty("tenantId", out var tenantIdElement))
            tenantId = tenantIdElement.GetInt32();
        
        if (!userId.HasValue)
            return Results.BadRequest(new { detail = "userId gereklidir" });
        
        var query = dbContext.Notifications.Where(n => n.UserId == userId.Value && !n.IsRead);
        
        if (tenantId.HasValue)
            query = query.Where(n => n.TenantId == tenantId.Value);
        
        var notifications = await query.ToListAsync();
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }
        
        await dbContext.SaveChangesAsync();
        
        return Results.Ok(new { message = $"{notifications.Count} bildirim okundu olarak işaretlendi" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Mark all read error: {ex.Message}");
        return Results.Problem($"Tüm bildirimleri okundu işaretleme hatası: {ex.Message}");
    }
});

// Delete notification
app.MapDelete("/api/notifications/{id}", async (int id, TaskDbContext dbContext) =>
{
    var notification = await dbContext.Notifications.FindAsync(id);
    if (notification == null)
        return Results.NotFound(new { message = "Bildirim bulunamadı" });
    
    dbContext.Notifications.Remove(notification);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok(new { message = "Bildirim silindi" });
});

app.Run();



