using System.Collections.Concurrent;
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

var users = new ConcurrentDictionary<string, DemoUser>(StringComparer.OrdinalIgnoreCase);

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

    if (user == null && username.Equals(password, StringComparison.OrdinalIgnoreCase))
    {
        user = await TryResolveStudentAccountAsync(username, httpClientFactory);

        if (user != null)
            users[username] = user;
    }

    if (user == null || user.Password != password)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(ToAuthResponse(user));
});

app.MapGet("/api/auth/student-accounts", () =>
{
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

    users[studentCode] = user;

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

app.Run();

static string CreateDemoToken(DemoUser user)
{
    var raw = $"{user.Username}:{user.Role}:{user.StudentCode}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
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

public sealed record DemoUser(
    string Username,
    string Password,
    string Role,
    string FullName,
    long? StudentId = null,
    string? StudentCode = null);
