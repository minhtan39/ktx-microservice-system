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

var builder = WebApplication.CreateBuilder(args);

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
        ?? "http://localhost:8080";

    client.BaseAddress = new Uri(gatewayBaseUrl);
});

var disableAuthForLocalDemo =
    builder.Configuration.GetValue<bool>("Auth:DisableForLocalDemo");

if (disableAuthForLocalDemo)
{
    builder.Services.AddAuthentication("LocalDemo")
        .AddScheme<AuthenticationSchemeOptions, LocalDemoAuthenticationHandler>(
            "LocalDemo",
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
app.UseAuthorization();

app.MapControllers();

app.Run();

public class LocalDemoAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public LocalDemoAuthenticationHandler(
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
            new Claim(ClaimTypes.NameIdentifier, "local-demo-admin"),
            new Claim(ClaimTypes.Name, "local_demo_admin"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
