using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ChatApp.Data;
using ChatApp.Services;
using ChatApp.Hubs;
using StackExchange.Redis; // Redis için gerekli

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add SignalR
builder.Services.AddSignalR();

// Veritabanı bağlantısını ortam değişkeninden veya appsettings'ten al
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Railway için DATABASE_URL ortam değişkenini kullanma
if (Environment.GetEnvironmentVariable("DATABASE_URL") != null)
{
    var railwayConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
    
    // Npgsql formatına dönüştürme
    var uri = new Uri(railwayConnectionString);
    var db = uri.AbsolutePath.Trim('/');
    var user = uri.UserInfo.Split(':')[0];
    var passwd = uri.UserInfo.Split(':')[1];
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    
    connectionString = $"Server={host};Port={port};Database={db};User Id={user};Password={passwd};";
}

// Entity Framework'ü yapılandırma
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseNpgsql(connectionString));

// Redis için Data Protection
if (Environment.GetEnvironmentVariable("REDIS_URL") != null)
{
    var redis = ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("REDIS_URL")!);
    builder.Services.AddDataProtection()
        .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");
}


// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? "your-secret-key-here"))
        };
        
        // Configure SignalR to use JWT
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Add Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMessageService, MessageService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Add static files support
app.UseStaticFiles();
app.UseDefaultFiles();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<ChatHub>("/chatHub");

// Map default route to index.html
app.MapFallbackToFile("index.html");

app.Run();