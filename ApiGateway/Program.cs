using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 20,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});

builder.Services.AddHttpClient("Proxy")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false
    });

var serviceRoutes = builder.Configuration
    .GetSection("ServiceRoutes")
    .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    service = "Smart Dormitory API Gateway",
    routes = serviceRoutes
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    checkedAt = DateTime.UtcNow
}));

app.MapMethods(
    "/api/{**path}",
    new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS" },
    async (
        HttpContext context,
        IHttpClientFactory httpClientFactory,
        string path) =>
    {
        if (context.Request.Method == HttpMethods.Options)
            return Results.Ok();

        var serviceKey = ResolveServiceKey(path);

        var isPublicPath = IsPublicPath(path, context.Request.Method);
        var isInternalServiceRequest = IsInternalServiceRequest(context, app.Configuration);

        if (!isPublicPath &&
            !isInternalServiceRequest &&
            context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        if (!isPublicPath &&
            !isInternalServiceRequest &&
            !IsAuthorizedForPath(path, context.Request.Method, context.User))
        {
            return Results.Forbid();
        }

        if (!serviceRoutes.TryGetValue(serviceKey, out var serviceBaseUrl))
        {
            return Results.NotFound(new
            {
                message = $"No gateway route configured for /api/{path}",
                serviceKey
            });
        }

        var targetUrl = BuildTargetUrl(serviceBaseUrl, context.Request);
        using var requestMessage = await CreateProxyRequestAsync(context, targetUrl);

        var client = httpClientFactory.CreateClient("Proxy");
        HttpResponseMessage responseMessage;

        try
        {
            responseMessage = await client.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                title: "Gateway cannot reach downstream service",
                detail: $"{serviceKey} at {serviceBaseUrl} is not reachable. {ex.Message}",
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (TaskCanceledException ex)
        {
            return Results.Problem(
                title: "Gateway downstream request timed out",
                detail: $"{serviceKey} at {serviceBaseUrl} did not respond in time. {ex.Message}",
                statusCode: StatusCodes.Status504GatewayTimeout);
        }

        using (responseMessage)
        {
            context.Response.StatusCode = (int)responseMessage.StatusCode;
            CopyResponseHeaders(context, responseMessage);

            await responseMessage.Content.CopyToAsync(context.Response.Body);
        }

        return Results.Empty;
    });

app.Run();

static bool IsPublicPath(string path, string method)
{
    var normalized = path.Trim('/').ToLowerInvariant();
    var isGet = HttpMethods.IsGet(method);

    if (isGet && IsRoomBuildingReadPath(normalized))
        return true;

    return normalized is "auth/login"
        or "auth/forgot-password"
        or "auth/reset-password"
        or "auth/reset-password/validate"
        or "billing/webhooks/payment";
}

static bool IsRoomBuildingReadPath(string normalizedPath)
{
    return normalizedPath == "rooms" ||
        normalizedPath.StartsWith("rooms/") ||
        normalizedPath == "buildings" ||
        normalizedPath.StartsWith("buildings/") ||
        normalizedPath == "roomtypes" ||
        normalizedPath.StartsWith("roomtypes/") ||
        normalizedPath == "room-types" ||
        normalizedPath.StartsWith("room-types/");
}

static bool IsAuthorizedForPath(
    string path,
    string method,
    ClaimsPrincipal user)
{
    if (user.IsInRole("Admin") || user.IsInRole("Staff"))
        return true;

    if (!user.IsInRole("Student"))
        return false;

    var normalized = path.Trim('/').ToLowerInvariant();
    var isGet = HttpMethods.IsGet(method);
    var isPost = HttpMethods.IsPost(method);

    if (normalized == "auth/change-password")
        return isPost;

    if (normalized == "students" || normalized.StartsWith("students/"))
        return isGet;

    if (normalized == "registrations")
        return isGet || isPost;

    if (normalized.StartsWith("registrations/"))
        return isGet;

    if (normalized.StartsWith("contracts/student/") ||
        normalized.StartsWith("contracts/"))
    {
        if (isPost && normalized.EndsWith("/sign"))
            return true;

        return isGet;
    }

    if (normalized == "billing/monthly-invoices" ||
        normalized.StartsWith("billing/monthly-invoices/") ||
        normalized == "billing/payment-history")
    {
        return isGet;
    }

    if (normalized.StartsWith("billing/statistics/student/"))
        return isGet;

    if (normalized.StartsWith("billing/wallets/"))
    {
        if (isGet)
            return true;

        return HttpMethods.IsPut(method) && normalized.EndsWith("/auto-pay");
    }

    if (normalized == "incidents")
        return isGet || isPost;

    if (normalized == "incidents/analyze")
        return isPost;

    if (normalized == "notifications")
        return isGet;

    if (normalized.StartsWith("notifications/") &&
        isGet &&
        normalized.Contains("/attachments/"))
    {
        return true;
    }

    if (normalized.StartsWith("notifications/") &&
        HttpMethods.IsPut(method) &&
        normalized.EndsWith("/read"))
    {
        return true;
    }

    if (normalized == "system/logs")
        return isPost;

    return normalized.StartsWith("incidents/") &&
        isPost &&
        (normalized.EndsWith("/confirm") ||
            normalized.EndsWith("/reopen") ||
            normalized.EndsWith("/cancel"));
}

static bool IsInternalServiceRequest(HttpContext context, IConfiguration configuration)
{
    var expectedKey = configuration["InternalService:ApiKey"];

    if (string.IsNullOrWhiteSpace(expectedKey))
        return false;

    return context.Request.Headers.TryGetValue("X-Internal-Service-Key", out var providedKey) &&
        string.Equals(providedKey.ToString(), expectedKey, StringComparison.Ordinal);
}

static string ResolveServiceKey(string path)
{
    var firstSegment = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault()?
        .ToLowerInvariant();

    return firstSegment switch
    {
        "auth" => "AuthService",
        "buildings" or "rooms" or "roomtypes" or "room-types" => "RoomService",
        "bills" or "billing" or "maintenance" or "incidents" or "notifications" or "system" => "BillingService",
        "students" or "registrations" or "roomregistration" or "contracts"
            or "contract" or "dashboard" or "checkhistory" => "ContractStudentService",
        _ => "ContractStudentService"
    };
}

static string BuildTargetUrl(string serviceBaseUrl, HttpRequest request)
{
    var path = request.Path.ToString();
    var query = request.QueryString.ToString();

    return $"{serviceBaseUrl.TrimEnd('/')}{path}{query}";
}

static async Task<HttpRequestMessage> CreateProxyRequestAsync(
    HttpContext context,
    string targetUrl)
{
    var requestMessage = new HttpRequestMessage(
        new HttpMethod(context.Request.Method),
        targetUrl);

    foreach (var header in context.Request.Headers)
    {
        if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
            header.Key.Equals("X-Internal-Service-Key", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
        {
            requestMessage.Content ??= new StreamContent(context.Request.Body);
            requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    if (context.Request.ContentLength > 0 ||
        context.Request.Headers.ContainsKey("Content-Type"))
    {
        requestMessage.Content ??= new StreamContent(context.Request.Body);

        if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
        {
            requestMessage.Content.Headers.ContentType =
                MediaTypeHeaderValue.Parse(context.Request.ContentType);
        }
    }

    await Task.CompletedTask;
    return requestMessage;
}

static void CopyResponseHeaders(
    HttpContext context,
    HttpResponseMessage responseMessage)
{
    foreach (var header in responseMessage.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    foreach (var header in responseMessage.Content.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    context.Response.Headers.Remove("transfer-encoding");
}
