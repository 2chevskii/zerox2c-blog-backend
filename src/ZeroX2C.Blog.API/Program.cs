using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using ZeroX2C.Blog.API.CrossCutting.Api;
using ZeroX2C.Blog.API.CrossCutting.Bootstrap;
using ZeroX2C.Blog.API.Modules.Assets.Images;
using ZeroX2C.Blog.API.Modules.Posts;
using ZeroX2C.Blog.API.Modules.Posts.Admin;
using ZeroX2C.Blog.API.Modules.Posts.Markdown;
using ZeroX2C.Blog.API.Modules.Users;
using ZeroX2C.Blog.API.Modules.Users.Admin;
using ZeroX2C.Blog.API.Modules.Users.Auth;
using ZeroX2C.Blog.API.Modules.Users.Profile;
using ZeroX2C.Blog.API.Persistence;
using ZeroX2C.Blog.API.Persistence.Auditing;
using ZeroX2C.Blog.API.Utility.Configuration;

var builder = WebApplication.CreateSlimBuilder(args);
var jwtOptions = GetRequiredJwtOptions(builder.Configuration);

builder.Services.AddDbContext<BlogDbContext>(
    (serviceProvider, options) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetRequiredConnectionString("PostgreSql");
        var auditInterceptor =
            serviceProvider.GetRequiredService<EntityAuditSaveChangesInterceptor>();
        options.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsAssembly(Assembly.GetExecutingAssembly());
            }
        );
        options.AddInterceptors(auditInterceptor);
    }
);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SuperAdminOptions>(
    builder.Configuration.GetSection(SuperAdminOptions.SectionName)
);
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddSingleton<IAuthenticationContextManager, AuthenticationContextManager>();
builder.Services.AddSingleton<IAuthenticationContext>(serviceProvider =>
    serviceProvider.GetRequiredService<IAuthenticationContextManager>().Current
);
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IImageQueryService, ImageQueryService>();
builder.Services.AddScoped<IAdminImageService, AdminImageService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IMarkdownDocumentRenderer, MarkdownDocumentRenderer>();
builder.Services.AddScoped<IPostQueryService, PostQueryService>();
builder.Services.AddScoped<IPostReactionService, PostReactionService>();
builder.Services.AddScoped<IPostCommentService, PostCommentService>();
builder.Services.AddScoped<ITagQueryService, TagQueryService>();
builder.Services.AddScoped<IAdminPostService, AdminPostService>();
builder.Services.AddScoped<IAdminMarkdownService, AdminMarkdownService>();
builder.Services.AddScoped<IAdminPostMarkdownImageService, AdminPostMarkdownImageService>();
builder.Services.AddScoped<IAdminTagService, AdminTagService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAuditEntityChangeHandler, AddedEntityAuditHandler>();
builder.Services.AddScoped<IAuditEntityChangeHandler, ModifiedEntityAuditHandler>();
builder.Services.AddScoped<IAuditEntityChangeHandler, DeletedEntityAuditHandler>();
builder.Services.AddScoped<EntityAuditSaveChangesInterceptor>();
builder.Services.AddScoped<ApplicationBootstrapper>();
builder.Services.AddScoped<IBootstrapHandler, SuperAdminBootstrapHandler>();
builder.Services.AddScoped<IBootstrapHandler, PostMarkdownBootstrapHandler>();
builder.Services.AddScoped<IAuthorizationHandler, NotBlockedRequirementHandler>();
builder.Services.AddHttpClient<ISteamOpenIdClient, SteamOpenIdClient>();

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtOptions.CreateSecurityKey(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicyNames.Admin,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(UserRole.Admin.ToString(), UserRole.SuperAdmin.ToString());
        }
    );

    options.AddPolicy(
        AuthorizationPolicyNames.SuperAdmin,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(UserRole.SuperAdmin.ToString());
        }
    );

    options.AddPolicy(
        AuthorizationPolicyNames.AuthenticatedNotBlocked,
        policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new NotBlockedRequirement());
        }
    );
});

builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.Configure<RouteOptions>(route =>
{
    route.LowercaseQueryStrings = true;
    route.LowercaseUrls = true;
    route.ConstraintMap["slug"] = typeof(SlugRouteConstraint);
});

builder.Services.AddOpenApi(openapi =>
{
    openapi.AddScalarTransformers();
});

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedHost
        | ForwardedHeaders.XForwardedProto,
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);

if (
    !app.Environment.IsProduction()
    || app.Configuration.GetValue<bool>("Diagnostics:DeveloperExceptionPageEnabled")
)
{
    app.UseDeveloperExceptionPage();
}

if (!app.Environment.IsProduction() || app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/api/health", Results.NoContent).AllowAnonymous();
app.UseAuthentication();
app.UseMiddleware<AuthenticationContextMiddleware>();
app.UseAuthorization();
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
    await dbContext.Database.MigrateAsync();

    var applicationBootstrapper =
        scope.ServiceProvider.GetRequiredService<ApplicationBootstrapper>();
    await applicationBootstrapper.BootstrapAsync();
}

await app.RunAsync();

static JwtOptions GetRequiredJwtOptions(IConfiguration configuration)
{
    var options =
        configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Missing Jwt configuration section.");

    if (Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
    {
        throw new InvalidOperationException(
            "Jwt:SigningKey must be configured and contain at least 32 UTF-8 bytes."
        );
    }

    return options;
}
