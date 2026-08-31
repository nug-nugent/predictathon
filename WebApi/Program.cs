using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Predictathon.Application.Extensions;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Mapping;
using Predictathon.Application.Options;
using Predictathon.Application.Services;
using Predictathon.Domain.Identity;
using Predictathon.Infrastructure.Persistence;
using Predictathon.WebApi.Extensions;
using Predictathon.WebApi.HealthChecks;
using Predictathon.WebApi.Development;
using Predictathon.WebApi.HostedServices;
using Predictathon.WebApi.Hubs;
using Predictathon.WebApi.Options;
using Predictathon.WebApi.Realtime;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

const string FrontendCorsPolicy = "Frontend";

// Bootstrap logger so failures during host/config startup still get logged somewhere - it's
// replaced by the fully-configured logger (file + SQL sinks, from the "Serilog" config section)
// once UseSerilog below runs. The file sink matters on IIS hosting, where console output is
// discarded - without it, a crash before UseSerilog takes effect would leave no trace at all.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/bootstrap-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services);

        // Console output is discarded under IIS/Plesk in-process hosting, so only wire it up
        // locally rather than fighting Microsoft.Extensions.Configuration's array-merging rules
        // to drop it via an appsettings.Production.json override.
        if (context.HostingEnvironment.IsDevelopment())
        {
            loggerConfiguration.WriteTo.Console();
        }
    });

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

            // RegisterModelValidator already allows any non-email-shaped username, and usernames
            // with spaces exist (carried over from the legacy app). Identity's UserValidator runs
            // on every UpdateAsync - including UserManager.ResetPasswordAsync - so without this,
            // resetting the password for a space-containing username fails validation even though
            // the password itself is untouched.
            options.User.AllowedUserNameCharacters += " ";
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

    // Bound for TasksController's shared-secret check - see that controller for why the daily
    // prediction-reminder/league-history tasks are triggered externally rather than by an
    // in-process timer.
    builder.Services.Configure<ScheduledTasksOptions>(builder.Configuration.GetSection(ScheduledTasksOptions.SectionName));

    // Throttles the unauthenticated auth endpoints (login, register, forgot-password, reset-password)
    // per client IP, since nothing else in the app records or limits repeated failed attempts.
    const string AuthRateLimitPolicy = "auth";
    builder.Services.Configure<AuthRateLimitOptions>(builder.Configuration.GetSection(AuthRateLimitOptions.SectionName));
    var authRateLimitOptions = builder.Configuration.GetSection(AuthRateLimitOptions.SectionName).Get<AuthRateLimitOptions>() ?? new AuthRateLimitOptions();
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy(AuthRateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimitOptions.PermitLimit,
                Window = TimeSpan.FromSeconds(authRateLimitOptions.WindowSeconds),
                QueueLimit = 0
            }));
    });

    // PayPal and football-data.org settings, bound in the Application layer's own Options types (rather
    // than alongside the WebApi/Options above) since Application-layer services consume them directly
    // and Application can't reference the WebApi project.
    builder.Services.Configure<PayPalOptions>(builder.Configuration.GetSection(PayPalOptions.SectionName));
    builder.Services.Configure<FootballDataApiOptions>(builder.Configuration.GetSection(FootballDataApiOptions.SectionName));

    // The football-data.org rate limit is per API key, not per caller, so the tally of recent calls
    // has to be one object shared by everything in the process - a scoped limiter would let each
    // request start from zero and enforce nothing. TimeProvider is the clock it measures its window
    // against, registered here so tests can substitute a fake one.
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddSingleton<IExternalApiRateLimiter, ExternalApiRateLimiter>();

    // Polls the provider for in-play scores. See LiveScorePollingService for why an in-process loop
    // is acceptable here when the daily maintenance jobs deliberately run off external pings.
    builder.Services.AddHostedService<LiveScorePollingService>();

    // The Docker dev stack has no API key, and its sample fixtures don't exist at football-data.org,
    // so the real client would have nothing to report and the live-score feature would be invisible
    // locally. Swapping in a simulated provider is a capability toggle rather than an environment
    // identity - the native dev workflow can opt in the same way by setting this in its own
    // appsettings, and Production can't be caught out by it whatever the config says.
    var footballDataOptions = builder.Configuration.GetSection(FootballDataApiOptions.SectionName).Get<FootballDataApiOptions>()
        ?? new FootballDataApiOptions();

    if (footballDataOptions.UseSimulatedProvider && !builder.Environment.IsProduction())
    {
        builder.Services.Replace(ServiceDescriptor.Scoped<IExternalMatchDataService, SimulatedMatchDataService>());
    }
    else if (footballDataOptions.UseSimulatedProvider)
    {
        // Ignored rather than obeyed, but never silently: serving invented scores to real predictors
        // would be worse than any startup failure, and a misplaced setting should be findable.
        Log.Warning(
            "{Setting} is set in Production and has been ignored - live scores will come from the real provider.",
            $"{FootballDataApiOptions.SectionName}:{nameof(FootballDataApiOptions.UseSimulatedProvider)}");
    }

    var avatarsSection = builder.Configuration.GetSection(AvatarsOptions.SectionName);
    builder.Services.Configure<AvatarsOptions>(avatarsSection);
    var avatarsOptions = avatarsSection.Get<AvatarsOptions>() ?? new AvatarsOptions();

    var messageImagesSection = builder.Configuration.GetSection(MessageImagesOptions.SectionName);
    builder.Services.Configure<MessageImagesOptions>(messageImagesSection);
    var messageImagesOptions = messageImagesSection.Get<MessageImagesOptions>() ?? new MessageImagesOptions();

    // The host is shared IIS hosting with no user profile or HKLM registry available, so the
    // default Data Protection key repository falls back to an ephemeral in-memory key ring -
    // regenerated on every worker process recycle, which silently invalidates the refresh-token
    // cookie, antiforgery tokens, and password-reset/2FA tokens on every recycle. Persist keys to
    // disk instead, outside the msdeploy-synced content path (same convention as Avatars/
    // MessageImages/Serilog above) so they survive both recycles and redeploys. The site isn't
    // load-balanced, so a single file-system key ring is sufficient - no XML encryptor is
    // configured, so keys are stored unencrypted at rest, but the host has no Windows DPAPI
    // available anyway (no user profile/registry) and this only protects auth cookie payloads and
    // short-lived tokens, not passwords.
    // Resolved once here and written back into configuration so the /reactions static mount below
    // and ReactionCatalogue (which resolves reaction identities against the same folder) can never
    // disagree about where the images live.
    var reactionsAssetsPath = Path.GetFullPath(
        builder.Configuration["Reactions:AssetsPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "Assets", "Reactions"));
    builder.Configuration["Reactions:AssetsPath"] = reactionsAssetsPath;

    var dataProtectionSection = builder.Configuration.GetSection(DataProtectionKeysOptions.SectionName);
    builder.Services.Configure<DataProtectionKeysOptions>(dataProtectionSection);
    var dataProtectionOptions = dataProtectionSection.Get<DataProtectionKeysOptions>() ?? new DataProtectionKeysOptions();
    var dataProtectionKeysPath = Path.GetFullPath(dataProtectionOptions.KeysPath);
    Directory.CreateDirectory(dataProtectionKeysPath);
    builder.Services.AddDataProtection()
        .SetApplicationName("Predictathon")
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

    // "/health" (bare true/false, for an external uptime monitor to keyword-match on) and
    // "/health/detailed" (per-check breakdown, for diagnosing which dependency is down) both run the
    // same checks - currently just the database, since almost every page needs it, so "process up but
    // DB unreachable" isn't a state worth calling healthy. Each is optionally gated by its own key below.
    var healthSection = builder.Configuration.GetSection(HealthOptions.SectionName);
    builder.Services.Configure<HealthOptions>(healthSection);
    var healthOptions = healthSection.Get<HealthOptions>() ?? new HealthOptions();

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>(name: "database", tags: ["db"]);

    builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(30));

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
    else
    {
        // Not enabled in Development - HSTS is a client-cached, sticky instruction (the browser
        // enforces it for MaxAge regardless of what the server does afterwards), which is more
        // friction than value against a dev-only cert on localhost.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseCors(FrontendCorsPolicy);

    app.UseRateLimiter();

    // Uploaded images sit at a fixed filename per user/message, so replacing one leaves every URL
    // pointing at it unchanged - which is exactly what a browser cache keys on. The app stamps a
    // version taken from the file itself onto the URLs it hands out (see AvatarService), making a
    // stamped URL safe to cache hard: it changes the moment the picture does. Anything asked for
    // without a stamp (an older link, a hand-typed path, or a message image - those URLs aren't
    // stamped yet) is revalidated on every use instead, so a stale image can never get pinned in a
    // cache for a year.
    static void AddUploadCacheHeaders(StaticFileResponseContext context)
    {
        context.Context.Response.Headers.CacheControl = context.Context.Request.Query.ContainsKey("v")
            ? "public, max-age=31536000, immutable"
            : "no-cache";
    }

    // Serve uploaded avatars directly from disk. Images don't need CORS headers (only canvas pixel
    // reads would), so this works cross-origin from the frontend's own host as-is.
    var avatarsStoragePath = Path.GetFullPath(avatarsOptions.StoragePath);
    Directory.CreateDirectory(avatarsStoragePath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(avatarsStoragePath),
        RequestPath = "/uploads/avatars",
        OnPrepareResponse = AddUploadCacheHeaders
    });

    var messageImagesStoragePath = Path.GetFullPath(messageImagesOptions.StoragePath);
    Directory.CreateDirectory(messageImagesStoragePath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(messageImagesStoragePath),
        RequestPath = "/uploads/message-images",
        OnPrepareResponse = AddUploadCacheHeaders
    });

    // Serve the vendored reaction images (standard-emoji SVGs + Predictathon's own custom reactions).
    // Unlike the Uploads/* folders above, this is static, checked-in content, not user-uploaded -
    // content root rather than a configurable storage path, since it ships with the app.
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(reactionsAssetsPath),
        RequestPath = "/reactions",
        // These files are content-addressed - a given codepoint's artwork is the same bytes
        // forever, and a re-vendoring that changed one would change its name too - so they're
        // immutable by construction and worth caching hard. Without this they're served with no
        // caching directives at all, costing a conditional request per emoji per page load.
        OnPrepareResponse = context =>
        {
            context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
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
}
catch (Exception ex)
{
    Log.Fatal(ex, "Predictathon WebApi terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
