using Microsoft.EntityFrameworkCore;
using MiniBackend.Data;
using MiniBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();

// PostgreSQL bağlantısı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://minifrontend-6ivp.onrender.com") // frontend URL
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Email service
builder.Services.AddScoped<EmailService>();

builder.Services.AddScoped<AuthHelper>();

// IConfiguration erişimi AuthController için zaten var
var app = builder.Build();

// CORS middleware
app.UseCors();

// DB auto migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Swagger (sadece dev ortamı)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS Render için devre dışı
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

// Render port ayarı
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://*:{port}");

app.Run();
