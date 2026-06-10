using System.Net.Http.Headers;

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

static string ResolveServiceKey(string path)
{
    var firstSegment = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault()?
        .ToLowerInvariant();

    return firstSegment switch
    {
        "auth" => "AuthService",
        "buildings" or "rooms" or "roomtypes" or "room-types" => "RoomService",
        "bills" or "billing" or "maintenance" or "incidents" => "BillingService",
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
        if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
            continue;

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
