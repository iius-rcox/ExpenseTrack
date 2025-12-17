using ExpenseFlow.Api.Middleware;
using ExpenseFlow.Api.Validators;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Extensions;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container

// Configure Entity Framework Core with PostgreSQL and pgvector
builder.Services.AddDbContext<ExpenseFlowDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgreSQL"),
        npgsqlOptions =>
        {
            npgsqlOptions.UseVector();
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        });
});

// Configure Authentication
var useDevAuth = builder.Configuration.GetValue<bool>("UseDevAuth", false);
if (useDevAuth && builder.Environment.IsDevelopment())
{
    // Development-only: Use mock authentication (NEVER use in production)
    builder.Services.AddAuthentication(ExpenseFlow.Api.Middleware.DevAuthenticationHandler.SchemeName)
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ExpenseFlow.Api.Middleware.DevAuthenticationHandler>(
            ExpenseFlow.Api.Middleware.DevAuthenticationHandler.SchemeName, null);
    Log.Warning("DEV AUTH ENABLED - This should NEVER be used in production!");
}
else
{
    // Production: Use Entra ID authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}

// Configure Authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("roles", "Admin", "ExpenseFlow.Admin"));

// Configure Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("PostgreSQL"))));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = builder.Configuration.GetValue("Hangfire:WorkerCount", 2);
});

// Add memory cache for session storage
builder.Services.AddMemoryCache();

// Register application services
builder.Services.AddExpenseFlowServices(builder.Configuration);

// Add controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<GenerateDraftRequestValidator>();

// Configure Problem Details
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ExpenseFlow API",
        Version = "v1",
        Description = "Core Backend API for ExpenseFlow expense management system"
    });

    // Configure JWT Bearer auth in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your Entra ID JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline

// Global exception handler (should be first)
app.UseGlobalExceptionHandler();

// Request logging
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});

// Swagger UI in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ExpenseFlow API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (admin only)
app.UseHangfireDashboard(
    builder.Configuration.GetValue("Hangfire:DashboardPath", "/hangfire"),
    new DashboardOptions
    {
        Authorization = new[] { new ExpenseFlow.Api.Filters.HangfireAuthorizationFilter() },
        DashboardTitle = "ExpenseFlow Jobs"
    });

// Configure recurring jobs
RecurringJob.AddOrUpdate<ExpenseFlow.Infrastructure.Jobs.ReferenceDataSyncJob>(
    "sync-reference-data",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 2 * * 0", // Every Sunday at 2 AM
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

// Sprint 5: Alias confidence decay job
RecurringJob.AddOrUpdate<ExpenseFlow.Infrastructure.Jobs.AliasConfidenceDecayJob>(
    "alias-confidence-decay",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 2 * * 0", // Every Sunday at 2 AM
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

// Sprint 6: Stale embedding cleanup job (monthly)
RecurringJob.AddOrUpdate<ExpenseFlow.Infrastructure.Jobs.EmbeddingCleanupJob>(
    "embedding-cleanup",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 3 1 * *", // First day of each month at 3 AM
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

// Sprint 7: Subscription alert check job (monthly)
RecurringJob.AddOrUpdate<ExpenseFlow.Infrastructure.Jobs.SubscriptionAlertJob>(
    "subscription-alert-check",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 4 1 * *", // First day of each month at 4 AM
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

app.MapControllers();

// Health check endpoint (minimal API for simplicity alongside controllers)
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"
})).AllowAnonymous();

try
{
    Log.Information("Starting ExpenseFlow API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Required for WebApplicationFactory in integration tests
public partial class Program { }
