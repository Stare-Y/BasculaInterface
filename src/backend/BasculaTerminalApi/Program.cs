using System.Text;
using BasculaTerminalApi.Controllers;
using BasculaTerminalApi.Service;
using Core.Application.Settings;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.IdentityModel.Tokens;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.File("logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

//as windows service
builder.Host.UseWindowsService();
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddBusinessServices(builder.Configuration);

// User auth (issue #134): global default-authenticated fallback policy — every existing GET
// endpoint and the bascula websocket carry an explicit [AllowAnonymous] (design.md Decision 3 of
// add-user-authentication-and-audit-log); anything else, including any endpoint added later,
// requires a valid token with no per-endpoint opt-in needed.
AuthSettings authSettings = builder.Configuration.GetSection("AuthSettings").Get<AuthSettings>() ?? new AuthSettings();

// Fail fast outside local development if the signing key was never overridden (env var or
// appsettings) — signing every token with a public, known placeholder is worse than crashing on
// startup. Gated on IsDevelopment() (not just "is the placeholder set") because AuthSettings.cs's
// own C# default is that same placeholder — local `dotnet run`/tests rely on it being usable
// without extra setup (ASPNETCORE_ENVIRONMENT=Development via launchSettings.json for the former,
// WebApplicationFactory's own Development default for the latter). A real deployment (no
// ASPNETCORE_ENVIRONMENT set → defaults to Production) is not exempt.
if (!builder.Environment.IsDevelopment() && authSettings.JwtSigningKey == "REPLACE_WITH_A_REAL_SECRET_IN_CONFIGURATION")
{
    throw new InvalidOperationException(
        "AuthSettings:JwtSigningKey is still the placeholder value. Set the AuthSettings__JwtSigningKey " +
        "environment variable to a real secret before running outside Development.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authSettings.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = authSettings.JwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSettings.JwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

app.MapHub<SerialPortHub>("/basculaSocket");

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromMinutes(2)});

if (builder.Configuration.GetValue<bool>("UseSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WeightDBContext>();
    db.Database.Migrate();
}

app.MapControllers();

// Admin portal (BasculaUi) static hosting — added after MapControllers/MapHub so an unmatched
// /api/... or hub request still falls through to the normal 404 instead of index.html; this
// ordering, not a route exclusion list, is what keeps existing API behavior untouched
// (design.md Decision 1 of add-admin-portal-frontend). wwwroot missing (e.g. no frontend build
// yet) is a supported no-op — these just 404 instead of serving the SPA (Decision 2).
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

await app.RunAsync();

/// <summary>
/// Explicit entry-point marker so the integration-test project can reference the host via
/// <c>WebApplicationFactory&lt;Program&gt;</c>. Top-level-statement programs otherwise expose only
/// an <c>internal</c> <c>Program</c> that a separate test assembly cannot see.
/// </summary>
public partial class Program { }
