using Microsoft.EntityFrameworkCore;
using MiniBackend.Data;
using MiniBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();

// PostgreSQL bağla
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://minifrontend-6ivp.onrender.com")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Email service
builder.Services.AddScoped<EmailService>();


var app = builder.Build();
app.UseCors();

// DB auto create
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Render için devre dışı
app.UseAuthorization();
app.MapControllers();

// Render port
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://*:{port}");

app.Run();
