// Program.cs — Main application entry point

using Amazon.Runtime;
using Amazon.S3;
using IncedoBank.API.Hubs;
using IncedoBank.API.Infrastructure.Persistence;
using IncedoBank.API.Services.AI;
using IncedoBank.API.Services.NBA;
using IncedoBank.API.Services.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;
using System.Security.Cryptography;

// ===================================================================
// Bootstrap Serilog early
// ===================================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ---------------------------------------------------------------
    // Serilog
    // ---------------------------------------------------------------
    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .WriteTo.Console());

    // ---------------------------------------------------------------
    // PostgreSQL (Neon in production, local Docker in dev)
    // ---------------------------------------------------------------
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsql => npgsql.UseVector()   // pgvector for RAG
        ));

    // ---------------------------------------------------------------
    // Redis (Upstash in production, local Docker in dev)
    // ---------------------------------------------------------------
    var redisConnection = builder.Configuration.GetConnectionString("Redis")!;
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConnection));
    builder.Services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConnection);

    // ---------------------------------------------------------------
    // Cloudflare R2 (S3-compatible, zero egress fees)
    // ---------------------------------------------------------------
    var r2Config = builder.Configuration.GetSection("Cloudflare:R2");
    var r2Credentials = new BasicAWSCredentials(
        r2Config["AccessKeyId"],
        r2Config["SecretAccessKey"]);
    var r2ClientConfig = new AmazonS3Config
    {
        ServiceURL = $"https://{r2Config["AccountId"]}.r2.cloudflarestorage.com",
        ForcePathStyle = true
    };
    builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(r2Credentials, r2ClientConfig));
    builder.Services.AddScoped<IStorageService, R2StorageService>();

    // ---------------------------------------------------------------
    // HTTP Clients (Groq + HuggingFace)
    // ---------------------------------------------------------------
    builder.Services.AddHttpClient("groq", c =>
    {
        c.BaseAddress = new Uri("https://api.groq.com/");
        c.Timeout = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddHttpClient("huggingface", c =>
    {
        c.BaseAddress = new Uri("https://api-inference.huggingface.co/");
        c.Timeout = TimeSpan.FromSeconds(20);
    });

    // ---------------------------------------------------------------
    // AI Services
    // ---------------------------------------------------------------
    builder.Services.AddScoped<IGroqService, GroqService>();
    builder.Services.AddScoped<ISentimentService, SentimentService>();
    builder.Services.AddScoped<INBAService, NBAService>();

    // ---------------------------------------------------------------
    // SignalR (real-time WebSocket)
    // ---------------------------------------------------------------
    builder.Services.AddSignalR();
    builder.Services.AddScoped<INotificationPusher, NotificationPusher>();

    // ---------------------------------------------------------------
    // JWT Auth — Verifies Clerk-issued JWTs
    // ---------------------------------------------------------------
    var clerkJwksUrl = builder.Configuration["Clerk:JwksUrl"]!;

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Clerk:Authority"];
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Clerk:Issuer"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                // Clerk uses RS256 — public keys fetched from JWKS endpoint
                IssuerSigningKeyResolver = (_, _, _, _) =>
                {
                    using var http = new HttpClient();
                    var jwks = http.GetStringAsync(clerkJwksUrl).Result;
                    return new JsonWebKeySet(jwks).GetSigningKeys();
                }
            };

            // Allow JWT from SignalR query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var accessToken = ctx.Request.Query["access_token"];
                    var path = ctx.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        ctx.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // ---------------------------------------------------------------
    // CORS — allow Next.js dev + Vercel prod
    // ---------------------------------------------------------------
    builder.Services.AddCors(opt =>
    {
        opt.AddDefaultPolicy(policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:3000",
                    "https://*.vercel.app")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // ---------------------------------------------------------------
    // Controllers
    // ---------------------------------------------------------------
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // ---------------------------------------------------------------
    // Health Checks
    // ---------------------------------------------------------------
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres")
        .AddRedis(redisConnection, name: "redis");

    // ===================================================================
    var app = builder.Build();
    // ===================================================================

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        // Auto-migrate database in development
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapHealthChecks("/health");

    Log.Information("IncedoBank API starting on {Env}", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
