using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Predictathon.Application.Extensions;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Mapping;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;
using Predictathon.WebApi.Extensions;
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

// Configure Identity (new AspNetUsers/AspNetRoles schema - separate from the legacy dbo.User table)
builder.Services
    .AddIdentityCore<ApplicationUser>()
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
    });

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

// Enable ProblemDetails middleware (produces application/problem+json for errors)
app.UseApiProblemDetails();

app.MapControllers();

await app.SeedRolesAsync();

app.Run();
