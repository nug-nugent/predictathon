using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
using Predictathon.WebApi.Extensions;
using Predictathon.WebApi.Hubs;
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
var jwtSection = builder.Configuration.GetSection("Jwt");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!)),
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
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
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
var avatarsStoragePath = Path.GetFullPath(builder.Configuration["Avatars:StoragePath"] ?? "Uploads/Avatars");
Directory.CreateDirectory(avatarsStoragePath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(avatarsStoragePath),
    RequestPath = "/uploads/avatars"
});

var messageImagesStoragePath = Path.GetFullPath(builder.Configuration["MessageImages:StoragePath"] ?? "Uploads/MessageImages");
Directory.CreateDirectory(messageImagesStoragePath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(messageImagesStoragePath),
    RequestPath = "/uploads/message-images"
});

app.UseAuthentication();
app.UseAuthorization();

// Enable ProblemDetails middleware (produces application/problem+json for errors)
app.UseApiProblemDetails();

app.MapControllers();
app.MapHub<MessageboardHub>("/hubs/messageboard");

await app.SeedRolesAsync();

app.Run();
