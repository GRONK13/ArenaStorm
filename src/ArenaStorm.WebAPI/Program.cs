using System.Text;
using ArenaStorm.WebAPI.Data;
using ArenaStorm.WebAPI.Endpoints;
using ArenaStorm.WebAPI.Middleware;
using ArenaStorm.WebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Database configuration (PostgreSQL by default, or SQLite if configured)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=arenaStorm;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase) ||
        connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

// Dependency injection services
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<LobbyService>();
builder.Services.AddScoped<LeaderboardService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ArenaStorm_SuperSecret_JWT_Key_2026_Change_In_Production!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ArenaStorm";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ArenaStormPlayers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure CORS for development
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

// Application middleware pipeline
app.UseErrorHandling();
app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Map Minimal API endpoint groups
app.MapAuthEndpoints();
app.MapLobbyEndpoints();
app.MapLeaderboardEndpoints();
app.MapProfileEndpoints();
app.MapChatEndpoints();

// Ensure database schema exists on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        Log.Information("Database schema initialized and verified.");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not initialize database on startup. Ensure database server is reachable.");
    }
}

app.Run();
