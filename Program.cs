using Microsoft.EntityFrameworkCore;
using MiniBackend.Data;
using MiniBackend.Services; // EmailService burada olacak

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://minifrontend-6ivp.onrender.com") // senin frontend URL’in
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Email Service (singleton)
builder.Services.AddSingleton<EmailService>();

var app = builder.Build();

app.UseCors();

// DB auto create
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Render için HTTPS yönlendirme kapalı
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

// Render için doğru portu al
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://*:{port}");

app.Run();
