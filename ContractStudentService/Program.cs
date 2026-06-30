using ContractStudentService.Data;
using ContractStudentService.Interfaces;
using ContractStudentService.Middleware;
using ContractStudentService.Repositories;
using ContractStudentService.Services;
using ContractStudentService.Validators;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using FluentValidation;
using Microsoft.Extensions.Options;
using PdfSharpCore.Fonts;

var builder = WebApplication.CreateBuilder(args);

GlobalFontSettings.FontResolver = new ContractFontResolver(builder.Environment);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var dataProtectionKeysPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("SmartDormitory.ContractStudentService");

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// Controllers
builder.Services.AddControllers();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateStudentDtoValidator>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Inter-service calls through API Gateway
builder.Services.AddHttpClient("Gateway", client =>
{
    var gatewayBaseUrl = builder.Configuration["Integration:GatewayBaseUrl"]
        ?? "http://api-gateway:8080";

    client.BaseAddress = new Uri(gatewayBaseUrl);

    var internalServiceKey = builder.Configuration["InternalService:ApiKey"];

    if (!string.IsNullOrWhiteSpace(internalServiceKey))
        client.DefaultRequestHeaders.Add("X-Internal-Service-Key", internalServiceKey);
});

var disableAuthForLocalAuthBypass =
    builder.Configuration.GetValue<bool>("Auth:DisableForLocalAuthBypass");

if (disableAuthForLocalAuthBypass)
{
    builder.Services.AddAuthentication("LocalAuthBypass")
        .AddScheme<AuthenticationSchemeOptions, LocalAuthBypassAuthenticationHandler>(
            "LocalAuthBypass",
            _ => { });
}
else
{
    // JWT Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
}

builder.Services.AddAuthorization();

// Dependency Injection
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentService, StudentService>();

builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<ContractEmailSender>();
builder.Services.AddSingleton<ContractPdfGenerator>();

builder.Services.AddScoped<IRoomRegistrationRepository,
    RoomRegistrationRepository>();

builder.Services.AddScoped<IRoomRegistrationService,
    RoomRegistrationService>();

builder.Services.AddScoped<IRoomGatewayClient, RoomGatewayClient>();
builder.Services.AddScoped<IBillingGatewayClient, BillingGatewayClient>();

builder.Services.AddScoped<ICheckHistoryRepository, CheckHistoryRepository>();
builder.Services.AddScoped<ICheckHistoryService, CheckHistoryService>();

builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// Middleware
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.Use(async (context, next) =>
{
    var expectedKey = context.RequestServices
        .GetRequiredService<IConfiguration>()["InternalService:ApiKey"];

    if (!string.IsNullOrWhiteSpace(expectedKey) &&
        context.Request.Headers.TryGetValue("X-Internal-Service-Key", out var providedKey) &&
        string.Equals(providedKey.ToString(), expectedKey, StringComparison.Ordinal))
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "internal-service"),
            new Claim(ClaimTypes.Name, "Internal Service"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Staff")
        };

        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(claims, "InternalService"));
    }

    await next();
});
app.UseAuthorization();

app.MapControllers();

app.Run();

public class LocalAuthBypassAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public LocalAuthBypassAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "local-auth-admin"),
            new Claim(ClaimTypes.Name, "local_auth_admin"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
