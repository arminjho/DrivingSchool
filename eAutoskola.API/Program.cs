using System.Text;
using DotNetEnv;
using eAutoskola.API.Filters;
using eAutoskola.Infrastructure.Configuration;
using eAutoskola.Infrastructure.Database;
using eAutoskola.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddSingleton(new SmtpOptions());
builder.Services.AddSingleton(new PayPalOptions());
builder.Services.AddSingleton(corsOptions);

// --- Database ---
builder.Services.AddDbContext<EAutoskolaDbContext>(options =>
    options.UseSqlServer(databaseOptions.ConnectionString));

// --- Messaging (RabbitMQ publisher) ---
builder.Services.AddSingleton<RabbitMqPublisherConnectionProvider>();
builder.Services.AddScoped<IEmailPublisher, RabbitMqEmailPublisher>();

// --- HTTP client factory (never `new HttpClient()` directly - upute.md §3.4) ---
builder.Services.AddHttpClient();

// --- HttpContextAccessor (registered exactly once) ---
builder.Services.AddHttpContextAccessor();

// --- Controllers + global exception filter ---
builder.Services.AddControllers(options => options.Filters.Add<ExceptionFilter>());

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
        };
        // OnTokenValidated (jti revocation blocklist + password-change cutoff check) is wired
        // once the User/RevokedToken entities exist - see docs/REFERENCE_ANALYSIS.md §2.6.
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
        Description = "Unesite JWT token u formatu: Bearer {token}",
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

app.Run();
