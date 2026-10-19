using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DotNetEnv;
using eAutoskola.API.Filters;
using eAutoskola.API.Hubs;
using eAutoskola.API.Json;
using eAutoskola.Application.Recommendations;
using eAutoskola.Application.ReferenceData.Cities;
using eAutoskola.Application.ReferenceData.Countries;
using eAutoskola.Application.ReferenceData.DrivingZones;
using eAutoskola.Application.ReferenceData.QuestionAreas;
using eAutoskola.Application.ReferenceData.RequestStatuses;
using eAutoskola.Application.ReferenceData.ServiceTypes;
using eAutoskola.Application.ReferenceData.VehicleCategories;
using eAutoskola.Application.ReferenceData.VehicleServiceTypes;
using eAutoskola.Domain.Messaging;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Configuration;
using eAutoskola.Infrastructure.Database;
using eAutoskola.Infrastructure.Messaging;
using eAutoskola.Infrastructure.Security;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// .env must be loaded before anything reads an environment variable - every *Options class
// constructor below depends on this running first (upute.md §3.3, REFERENCE_ANALYSIS.md §2.9).
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// --- Centralised configuration: each *Options class reads its env vars exactly once here ---
var jwtOptions = new JwtOptions();
var databaseOptions = new DatabaseOptions();
var corsOptions = new CorsOptions();
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton(databaseOptions);
builder.Services.AddSingleton(new RabbitMqOptions());
builder.Services.AddSingleton(new PayPalOptions());
builder.Services.AddSingleton(corsOptions);

// --- Database ---
// EF warns at startup that User/Vehicle have a soft-delete query filter and are the required end of
// 11 relationships. That is a model-level check; each relationship was reviewed and the one
// reachable case is handled with IgnoreQueryFilters (docs/BACKLOG.md), so the warning is silenced.
builder.Services.AddDbContext<EAutoskolaDbContext>(options =>
    options.UseSqlServer(databaseOptions.ConnectionString)
        .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

// --- Mapster (entity <-> DTO mapping) ---
var mapperConfig = TypeAdapterConfig.GlobalSettings;
mapperConfig.Scan(typeof(eAutoskola.Application.Common.PagedResult<>).Assembly);
builder.Services.AddSingleton(mapperConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

// --- Messaging (RabbitMQ publisher) ---
builder.Services.AddSingleton<RabbitMqPublisherConnectionProvider>();
builder.Services.AddScoped<IMessagePublisher, RabbitMqMessagePublisher>();

// --- HTTP client factory (never `new HttpClient()` directly - upute.md §3.4) ---
builder.Services.AddHttpClient();

// --- HttpContextAccessor (registered exactly once) ---
builder.Services.AddHttpContextAccessor();

// --- Auth services ---
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITokenBlocklistService, TokenBlocklistService>();
builder.Services.AddScoped<eAutoskola.Application.Auth.IAuthService, eAutoskola.Application.Auth.AuthService>();

// --- File storage ---
builder.Services.AddSingleton<eAutoskola.Domain.Files.IFileStorageService, eAutoskola.Infrastructure.Files.FileStorageService>();
builder.Services.AddScoped<eAutoskola.Application.Files.FileAccessService>();

// --- Reference-data services ---
builder.Services.AddCountryServices();
builder.Services.AddCityServices();
builder.Services.AddVehicleCategoryServices();
builder.Services.AddRequestStatusServices();
builder.Services.AddServiceTypeServices();
builder.Services.AddVehicleServiceTypeServices();
builder.Services.AddDrivingZoneServices();
builder.Services.AddQuestionAreaServices();
builder.Services.AddScoped<eAutoskola.Application.Users.UserService>();
builder.Services.AddScoped<eAutoskola.Application.Me.MeService>();
builder.Services.AddScoped<eAutoskola.Application.Instructors.InstructorQueryService>();
builder.Services.AddScoped<eAutoskola.Application.Vehicles.VehicleCrudService>();
builder.Services.AddScoped<eAutoskola.Application.VehicleMaintenance.VehicleMaintenanceService>();
builder.Services.AddScoped<eAutoskola.Application.Notifications.NotificationService>();
builder.Services.AddScoped<eAutoskola.Domain.Notifications.INotificationPusher, SignalRNotificationPusher>();
builder.Services.AddScoped<eAutoskola.Domain.Payments.IPayPalClient, eAutoskola.Infrastructure.Payments.PayPalClient>();
builder.Services.AddScoped<eAutoskola.Application.Payments.FinanceService>();
builder.Services.AddScoped<eAutoskola.Application.Payments.PaymentService>();
builder.Services.AddScoped<eAutoskola.Application.Payments.LessonPaymentService>();
builder.Services.AddScoped<eAutoskola.Application.Lessons.LessonQueryService>();
builder.Services.AddScoped<eAutoskola.Application.Lessons.LessonCommandService>();
builder.Services.AddScoped<eAutoskola.Application.Tests.TestCrudService>();
builder.Services.AddScoped<eAutoskola.Application.Tests.QuestionCrudService>();
builder.Services.AddScoped<eAutoskola.Application.Tests.TestAttemptService>();
builder.Services.AddScoped<eAutoskola.Application.StudyLessons.StudyLessonCrudService>();
builder.Services.AddScoped<eAutoskola.Application.StudyLessons.StudyLessonSectionCrudService>();
builder.Services.AddScoped<eAutoskola.Application.Recommendations.StudyRecommendationService>();
builder.Services.AddScoped<eAutoskola.Application.Reports.DashboardService>();
builder.Services.AddScoped<eAutoskola.Application.Reports.ReportService>();

// --- SignalR (NotificationHub) ---
builder.Services.AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));

// --- Controllers + global exception filter ---
builder.Services.AddControllers(options => options.Filters.Add<ExceptionFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));

// --- CORS (registered exactly once, explicit origins only) ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOptions.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsOptions.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// --- JWT authentication ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
        };
        options.Events = new JwtBearerEvents
        {
            // SignalR's browser/WebSocket clients can't set an Authorization header, so the
            // token is passed as a query string parameter for hub connections specifically.
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrEmpty(jti))
                {
                    context.Fail(eAutoskola.Domain.Resources.Messages.TokenMissingJti);
                    return;
                }

                var blocklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlocklistService>();
                if (await blocklist.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                {
                    context.Fail(eAutoskola.Domain.Resources.Messages.TokenRevoked);
                    return;
                }

                // Password-change cutoff: tokens issued before TokensValidFromUtc are rejected
                // even if otherwise unexpired.
                var subject = context.Principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var issuedAt = context.Principal?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iat)?.Value;
                if (int.TryParse(subject, out var userId) && long.TryParse(issuedAt, out var issuedAtUnixSeconds))
                {
                    var validFrom = await blocklist.GetTokensValidFromUtcAsync(userId, context.HttpContext.RequestAborted);
                    var issuedAtUtc = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnixSeconds).UtcDateTime;
                    if (validFrom is { } cutoffUtc && issuedAtUtc < cutoffUtc)
                    {
                        context.Fail(eAutoskola.Domain.Resources.Messages.PasswordChangedReloginRequired);
                    }
                }
            },
        };
    });
builder.Services.AddAuthorization();

// --- Swagger (Development only, with JWT bearer support) - registered exactly once ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "eAutoskola API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT as: Bearer {token}",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            []
        },
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// No HTTPS redirection: upute.md §9.2 explicitly recommends plain HTTP for API access during
// review, since self-signed certificates can expire or be invalid on the reviewer's machine.

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// Apply migrations and seed on startup, with retry - the DB container may still be starting
// even though compose reports it "healthy" (the same race fixed for RabbitMQ in Phase 2).
await MigrateAndSeedDatabaseAsync(app);

app.Run();

static async Task MigrateAndSeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<EAutoskolaDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var delay = TimeSpan.FromSeconds(2);
    const int maxAttempts = 6;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await MigrateAndSeedAsync(context);
            logger.LogInformation("Database migrated and seeded successfully");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Database migration attempt {Attempt}/{MaxAttempts} failed, retrying in {Delay}", attempt, maxAttempts, delay);
            await Task.Delay(delay);
            delay *= 2;
        }
    }

    // Final attempt - let it throw and fail startup loudly if the DB is truly unreachable.
    await MigrateAndSeedAsync(context);
}

static async Task MigrateAndSeedAsync(EAutoskolaDbContext context)
{
    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context);
    await RecommendationLogSeeder.SeedAsync(context);
}
