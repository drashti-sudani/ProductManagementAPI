using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Infrastructure.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Application.Validators;
using Infrastructure.Identity;
using Application.DTOs.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register generic repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
// Register product-specific repository
builder.Services.AddScoped<IProductRepository, ProductRepository>();
// Register unit of work
builder.Services.AddScoped<Application.Interfaces.IUnitOfWork, Infrastructure.UnitOfWork>();
// Register product service
builder.Services.AddScoped<Application.Interfaces.IProductService, Application.Services.ProductService>();

// Add services to the container.
// Configure Swagger/OpenAPI
// Register FluentValidation validators
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ProductCreateValidator>();
// Response compression for improved network performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});
// Jwt settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<AuthService>();

// (API Versioning removed — controllers use explicit v1 routes)

// Authentication
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSection.GetValue<string>("Secret") ?? throw new InvalidOperationException("JwtSettings:Secret is not configured");
// Derive the same 256-bit key as used by JwtTokenGenerator (SHA-256 of secret)
var key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
// CORS: allow local development origins (adjust in production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev", p => p
        .WithOrigins("https://localhost:5001", "http://localhost:5000")
        .AllowAnyHeader()
        .AllowAnyMethod()
    );
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
    // Add events to log validation errors and successful validations to aid debugging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Logging.ILoggerFactory)) as Microsoft.Extensions.Logging.ILoggerFactory;
            logger?.CreateLogger("JwtBearer").LogError(context.Exception, "JWT authentication failed");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Logging.ILoggerFactory)) as Microsoft.Extensions.Logging.ILoggerFactory;
            var name = context.Principal?.Identity?.Name ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            logger?.CreateLogger("JwtBearer").LogInformation("JWT token validated for {Name}", name);
            return Task.CompletedTask;
        }
    };
});

// Authorization services
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"{token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

// Add MVC controllers and global filters
builder.Services.AddControllers(options =>
{
    options.Filters.Add<API.Filters.ValidateModelAttribute>();
});

var app = builder.Build();

// Map controllers
app.MapControllers();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// Enable response compression
app.UseResponseCompression();
// Use CORS policy
app.UseCors("LocalDev");
// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
    await next();
});
// HSTS in non-development
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
// Global exception handler
app.UseMiddleware<API.Middleware.ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Auth endpoints
app.MapPost("/api/v1/auth/login", async (LoginRequestDto req, AuthService auth) =>
{
    try
    {
        var tokens = await auth.LoginAsync(req);
        return Results.Ok(tokens);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
});

app.MapPost("/api/v1/auth/refresh", async (RefreshRequestDto req, AuthService auth) =>
{
    try
    {
        var tokens = await auth.RefreshAsync(req);
        return Results.Ok(tokens);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
});

app.MapPost("/api/v1/auth/logout", async (RefreshRequestDto req, AuthService auth) =>
{
    await auth.LogoutAsync(req);
    return Results.NoContent();
});

// Removed default weather endpoints and sample types

// Apply any pending EF Core migrations on startup. This helps when running in Docker so the
// database schema is created/updated before the application starts serving requests.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger("Migration");
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Retry loop: the database container may not be ready immediately when the app starts.
        var retries = 10;
        for (var i = 0; i < retries; i++)
        {
            try
            {
                context.Database.Migrate();
                logger?.LogInformation("Database migrated successfully.");
                break;
            }
            catch (Exception ex) when (i < retries - 1)
            {
                logger?.LogWarning(ex, "Database migration attempt {Attempt} failed - retrying in 5s", i + 1);
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
    }
    catch (Exception ex)
    {
        // If migration fails completely, we still want to fail fast so the container can be restarted
        // and the issue becomes visible in logs.
        var lf = services.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
        lf?.CreateLogger("Migration").LogError(ex, "An error occurred while migrating or initializing the database.");
        throw;
    }
}

app.Run();

// Expose Program class for WebApplicationFactory in integration tests
public partial class Program { }
