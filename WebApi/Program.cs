using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Predictathon.Application.Extensions;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Mapping;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;
using Predictathon.WebApi.BackgroundServices;
using Predictathon.WebApi.Extensions;
using Predictathon.WebApi.HealthChecks;
using Predictathon.WebApi.Hubs;
using Predictathon.WebApi.Options;
using Predictathon.WebApi.Realtime;
using System.Text;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
builder.Services.AddScoped<IGenericDbContext, ApplicationDbContext>();

builder.Services.AddHttpClient();

// Configure Identity (Identity.Users/Identity.Roles schema)
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // Unset (false) by default, which makes Identity's built-in UserValidator skip email
        // format/uniqueness checks entirely - needed so SetEmailAsync/CreateAsync actually
        // enforce them (profile editing relies on this to reject duplicate emails).
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure JWT bearer authentication
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);

var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrEmpty(jwtOptions.SigningKey))
{
    throw new InvalidOperationException($"'{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)}' must be configured.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // SignalR's browser client can't set an Authorization header on the WebSocket handshake,
        // so it sends the access token as a query string parameter instead - only honoured for the
        // messageboard hub's own path, not for regular API requests.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/messageboard"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSignalR();
builder.Services.AddScoped<IMessageboardNotifier, MessageboardNotifier>();

// Configure CORS. No origins are allowed until Cors:AllowedOrigins is populated in config -
// there's no permissive "just for dev" fallback, so nothing is silently left wide open.
var corsSection = builder.Configuration.GetSection(CorsOptions.SectionName);
builder.Services.Configure<CorsOptions>(corsSection);

var corsOptions = corsSection.Get<CorsOptions>() ?? new CorsOptions();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Needed so the browser sends/receives the HttpOnly refresh-token cookie cross-origin.
            // Only valid combined with an explicit origin allow-list above, never AllowAnyOrigin().
            .AllowCredentials();
    });
});

// Configure Mapster
MapsterConfiguration.Configure();
var config = TypeAdapterConfig.GlobalSettings;
builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, Mapper>();

// Add services to the container.
builder.Services.AddControllers();

// Configure ProblemDetails / RFC7807 behaviour for model validation and errors
builder.Services.AddApiProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add all [ScopedService] services from the Application assembly
builder.Services.AddApplication();

// Runs the daily prediction-reminder and league-history scheduled tasks in-process, replacing the
// old UptimeRobot-pinged TasksController endpoints.
builder.Services.Configure<ScheduledTasksOptions>(builder.Configuration.GetSection(ScheduledTasksOptions.SectionName));
builder.Services.AddHostedService<ScheduledTasksHostedService>();

var avatarsSection = builder.Configuration.GetSection(AvatarsOptions.SectionName);
builder.Services.Configure<AvatarsOptions>(avatarsSection);
var avatarsOptions = avatarsSection.Get<AvatarsOptions>() ?? new AvatarsOptions();

var messageImagesSection = builder.Configuration.GetSection(MessageImagesOptions.SectionName);
builder.Services.Configure<MessageImagesOptions>(messageImagesSection);
var messageImagesOptions = messageImagesSection.Get<MessageImagesOptions>() ?? new MessageImagesOptions();

// "/health" (bare true/false, for an external uptime monitor to keyword-match on) and
// "/health/detailed" (per-check breakdown, for diagnosing which dependency is down) both run the
// same checks - currently just the database, since almost every page needs it, so "process up but
// DB unreachable" isn't a state worth calling healthy. Each is optionally gated by its own key below.
var healthSection = builder.Configuration.GetSection(HealthOptions.SectionName);
builder.Services.Configure<HealthOptions>(healthSection);
var healthOptions = healthSection.Get<HealthOptions>() ?? new HealthOptions();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(name: "database", tags: ["db"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

// Serve uploaded avatars directly from disk. Images don't need CORS headers (only canvas pixel
// reads would), so this works cross-origin from the frontend's own host as-is.
var avatarsStoragePath = Path.GetFullPath(avatarsOptions.StoragePath);
Directory.CreateDirectory(avatarsStoragePath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(avatarsStoragePath),
    RequestPath = "/uploads/avatars"
});

var messageImagesStoragePath = Path.GetFullPath(messageImagesOptions.StoragePath);
Directory.CreateDirectory(messageImagesStoragePath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(messageImagesStoragePath),
    RequestPath = "/uploads/message-images"
});

// Serve the vendored reaction images (standard-emoji SVGs + Predictathon's own custom reactions).
// Unlike the Uploads/* folders above, this is static, checked-in content, not user-uploaded -
// content root rather than a configurable storage path, since it ships with the app.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "Assets", "Reactions")),
    RequestPath = "/reactions"
});

app.UseAuthentication();
app.UseAuthorization();

// Enable ProblemDetails middleware (produces application/problem+json for errors)
app.UseApiProblemDetails();

app.MapControllers();
app.MapHub<MessageboardHub>("/hubs/messageboard");

// Bare "true"/"false" body, nothing else - safe to point an external uptime monitor at.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteBooleanAsync
}).AddEndpointFilter(new ApiKeyEndpointFilter(healthOptions.ApiKey));

// Full per-check breakdown, gated by its own key since it reveals more than "/health".
app.MapHealthChecks("/health/detailed", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteDetailedAsync
}).AddEndpointFilter(new ApiKeyEndpointFilter(healthOptions.DetailedApiKey));

await app.SeedRolesAsync();

app.Run();
