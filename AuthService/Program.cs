using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGateway", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();
builder.Services.AddSingleton<AccountStore>();
builder.Services.AddHttpClient("Gateway", client =>
{
    var gatewayBaseUrl = builder.Configuration["Integration:GatewayBaseUrl"]
        ?? "http://api-gateway:8080";

    client.BaseAddress = new Uri(gatewayBaseUrl);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowGateway");

var accountStore = app.Services.GetRequiredService<AccountStore>();
var users = accountStore.Users;

if (users.IsEmpty)
{
    users["admin"] = new(
        "admin",
        "admin123",
        "Admin",
        "Quản trị hệ thống");

    users["nhanvien"] = new(
        "nhanvien",
        "staff123",
        "Staff",
        "Nhân viên ký túc xá");

    users["SV20260001"] = new(
        "SV20260001",
        "SV20260001",
        "Student",
        "Nguyễn Văn A",
        2,
        "SV20260001");

    accountStore.Persist();
}

app.MapGet("/health", () => Results.Ok(new
{
    service = "AuthService",
    status = "Healthy"
}));

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    IHttpClientFactory httpClientFactory) =>
{
    var username = request.Username.Trim();
    var password = request.Password.Trim();

    users.TryGetValue(username, out var user);

    var studentAccountAlreadyExists = users.Values.Any(existing =>
        existing.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) &&
        existing.StudentCode?.Equals(username, StringComparison.OrdinalIgnoreCase) == true);

    if (user == null &&
        !studentAccountAlreadyExists &&
        username.Equals(password, StringComparison.OrdinalIgnoreCase))
    {
        user = await TryResolveStudentAccountAsync(username, httpClientFactory);

        if (user != null)
            accountStore.Upsert(user);
    }

    if (user == null || user.Password != password)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(ToAuthResponse(user));
});

app.MapGet("/api/auth/student-accounts", async (IHttpClientFactory httpClientFactory) =>
{
    await SynchronizeStudentAccountsAsync(accountStore, httpClientFactory);

    var accounts = users.Values
        .Where(user => user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
        .OrderBy(user => user.StudentCode)
        .Select(user => new
        {
            user.Username,
            user.Role,
            user.FullName,
            user.StudentId,
            user.StudentCode
        });

    return Results.Ok(new { data = accounts });
});

app.MapPost("/api/auth/student-accounts", (StudentAccountRequest request) =>
{
    var studentCode = request.StudentCode.Trim();

    if (string.IsNullOrWhiteSpace(studentCode))
        return Results.BadRequest(new { message = "StudentCode is required." });

    var user = new DemoUser(
        studentCode,
        studentCode,
        "Student",
        request.FullName.Trim(),
        request.StudentId,
        studentCode);

    accountStore.Upsert(user);

    return Results.Ok(new
    {
        data = new
        {
            user.Username,
            defaultPassword = studentCode,
            user.Role,
            user.FullName,
            user.StudentId,
            user.StudentCode,
            homePath = "/student/portal"
        }
    });
});

app.MapGet("/api/auth/accounts", async (
    HttpRequest request,
    IHttpClientFactory httpClientFactory) =>
{
    if (!IsAdminRequest(request))
        return Results.Unauthorized();

    await SynchronizeStudentAccountsAsync(accountStore, httpClientFactory);

    var accounts = users.Values
        .Where(user => !user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        .OrderBy(user => user.Role)
        .ThenBy(user => user.Username)
        .Select(ToAccountResponse)
        .ToList();

    return Results.Ok(new { data = accounts });
});

app.MapGet("/api/auth/accounts/{username}", (string username, HttpRequest request) =>
{
    if (!IsAdminRequest(request))
        return Results.Unauthorized();

    return users.TryGetValue(username, out var user) &&
        !user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            ? Results.Ok(new { data = ToAccountResponse(user) })
            : Results.NotFound(new { message = "Account not found." });
});

app.MapPut("/api/auth/accounts/{username}", (string username, UpdateAccountRequest update, HttpRequest request) =>
{
    if (!IsAdminRequest(request))
        return Results.Unauthorized();

    if (!users.TryGetValue(username, out var current) ||
        current.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        return Results.NotFound(new { message = "Account not found." });
    }

    var nextUsername = string.IsNullOrWhiteSpace(update.Username)
        ? current.Username
        : update.Username.Trim();

    var nextPassword = string.IsNullOrWhiteSpace(update.Password)
        ? current.Password
        : update.Password.Trim();

    var nextFullName = string.IsNullOrWhiteSpace(update.FullName)
        ? current.FullName
        : update.FullName.Trim();

    if (string.IsNullOrWhiteSpace(nextUsername) || string.IsNullOrWhiteSpace(nextPassword))
    {
        return Results.BadRequest(new { message = "Username and password are required." });
    }

    if (!nextUsername.Equals(username, StringComparison.OrdinalIgnoreCase) &&
        users.ContainsKey(nextUsername))
    {
        return Results.Conflict(new { message = "Username already exists." });
    }

    var updated = current with
    {
        Username = nextUsername,
        Password = nextPassword,
        FullName = nextFullName,
        StudentCode = current.StudentCode
    };

    accountStore.Replace(username, updated);

    return Results.Ok(new { data = ToAccountResponse(updated) });
});

app.Run();

static string CreateDemoToken(DemoUser user)
{
    var raw = $"{user.Username}:{user.Role}:{user.StudentCode}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
}

static bool IsAdminRequest(HttpRequest request)
{
    var authorization = request.Headers.Authorization.ToString();
    const string bearerPrefix = "Bearer ";

    if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        return false;

    var token = authorization[bearerPrefix.Length..].Trim();

    try
    {
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
        var parts = raw.Split(':');

        return parts.Length >= 2 &&
            parts[1].Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
    catch
    {
        return false;
    }
}

static object ToAuthResponse(DemoUser user)
{
    return new
    {
        data = new
        {
            token = CreateDemoToken(user),
            role = user.Role,
            username = user.Username,
            fullName = user.FullName,
            studentId = user.StudentId,
            studentCode = user.StudentCode,
            homePath = user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase)
                ? "/student/portal"
                : "/student-service/dashboard"
        }
    };
}

static object ToAccountResponse(DemoUser user)
{
    return new
    {
        user.Username,
        user.Password,
        user.Role,
        user.FullName,
        user.StudentId,
        user.StudentCode
    };
}

static async Task SynchronizeStudentAccountsAsync(
    AccountStore accountStore,
    IHttpClientFactory httpClientFactory)
{
    try
    {
        var client = httpClientFactory.CreateClient("Gateway");
        var response = await client.GetAsync("/api/students");

        if (!response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var students = TryGetStudentArray(document.RootElement);

        if (students.ValueKind != JsonValueKind.Array)
            return;

        var missingAccounts = new List<DemoUser>();

        foreach (var student in students.EnumerateArray())
        {
            var studentCode = GetString(student, "studentCode");

            if (string.IsNullOrWhiteSpace(studentCode))
                continue;

            var fullName = GetString(student, "fullName");

            missingAccounts.Add(new DemoUser(
                studentCode,
                studentCode,
                "Student",
                string.IsNullOrWhiteSpace(fullName) ? studentCode : fullName,
                GetInt64(student, "id"),
                studentCode));
        }

        accountStore.AddMissingStudents(missingAccounts);
    }
    catch (HttpRequestException)
    {
        // Persisted accounts remain available while ContractStudentService is offline.
    }
    catch (TaskCanceledException)
    {
        // A synchronization timeout must not block access to persisted accounts.
    }
    catch (JsonException)
    {
        // Ignore malformed downstream responses and keep the persisted account file.
    }
}

static async Task<DemoUser?> TryResolveStudentAccountAsync(
    string studentCode,
    IHttpClientFactory httpClientFactory)
{
    try
    {
        var client = httpClientFactory.CreateClient("Gateway");
        var response = await client.GetAsync("/api/students");

        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);

        var students = TryGetStudentArray(document.RootElement);

        if (students.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var student in students.EnumerateArray())
        {
            var code = GetString(student, "studentCode");

            if (!studentCode.Equals(code, StringComparison.OrdinalIgnoreCase))
                continue;

            var id = GetInt64(student, "id");
            var fullName = GetString(student, "fullName");

            return new DemoUser(
                code,
                code,
                "Student",
                string.IsNullOrWhiteSpace(fullName) ? code : fullName,
                id,
                code);
        }
    }
    catch
    {
        return null;
    }

    return null;
}

static JsonElement TryGetStudentArray(JsonElement root)
{
    if (root.ValueKind == JsonValueKind.Array)
        return root;

    if (root.TryGetProperty("data", out var data) &&
        data.ValueKind == JsonValueKind.Array)
        return data;

    if (root.TryGetProperty("value", out var value) &&
        value.ValueKind == JsonValueKind.Array)
        return value;

    return default;
}

static string GetString(JsonElement element, string propertyName)
{
    return element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
}

static long GetInt64(JsonElement element, string propertyName)
{
    return element.TryGetProperty(propertyName, out var property) &&
        property.TryGetInt64(out var value)
            ? value
            : 0;
}

public sealed record LoginRequest(string Username, string Password);

public sealed record StudentAccountRequest(
    long StudentId,
    string StudentCode,
    string FullName);

public sealed record UpdateAccountRequest(
    string? Username,
    string? Password,
    string? FullName);

public sealed record DemoUser(
    string Username,
    string Password,
    string Role,
    string FullName,
    long? StudentId = null,
    string? StudentCode = null);
