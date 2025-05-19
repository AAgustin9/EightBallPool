using Microsoft.EntityFrameworkCore;
using _8_ball_pool.Data;
using DotNetEnv;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.Extensions.NETCore.Setup;
using _8_ball_pool.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env
Env.Load();

// PostgreSQL connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Replace environment variable placeholders in the connection string
if (connectionString != null)
{
    connectionString = connectionString
        .Replace("${DB_HOST}", Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost")
        .Replace("${DB_PORT}", Environment.GetEnvironmentVariable("DB_PORT"))
        .Replace("${DB_NAME}", Environment.GetEnvironmentVariable("DB_NAME") ?? "pooldb")
        .Replace("${DB_USER}", Environment.GetEnvironmentVariable("DB_USER") ?? "postgres")
        .Replace("${DB_PASSWORD}", Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres");
}
else
{
    connectionString = "Host=localhost;Port=5432;Database=pooldb;Username=postgres;Password=postgres";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// AWS S3 configuration
builder.Services.AddDefaultAWSOptions(new AWSOptions
{
    Credentials = new BasicAWSCredentials(
        Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
        Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")),
    Region = Amazon.RegionEndpoint.GetBySystemName(
        Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1")
});
builder.Services.AddAWSService<IAmazonS3>();

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database")
    .AddCheck("self", () => HealthCheckResult.Healthy());

// App services
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<IMatchesService, MatchesService>();
builder.Services.AddScoped<IRankingService, RankingService>();

// Register background service for ranking updates
builder.Services.AddHostedService<RankingBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger setup with metadata
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "8 Ball Pool API",
        Version = "v1",
        Description = "API for managing players, matches, and media in 8 Ball Pool Challenge",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });
});

var app = builder.Build();

// Middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "8 Ball Pool API V1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at root
});

// Health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                exception = e.Value.Exception?.Message,
                duration = e.Value.Duration.ToString()
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
