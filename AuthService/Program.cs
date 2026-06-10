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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowGateway");

var users = new Dictionary<string, DemoUser>(StringComparer.OrdinalIgnoreCase)
{
    ["admin"] = new(
        "admin",
        "admin123",
        "Admin",
        "Quản trị hệ thống"),
    ["nhanvien"] = new(
        "nhanvien",
        "staff123",
        "Staff",
        "Nhân viên ký túc xá"),
    ["sinhvien"] = new(
        "sinhvien",
        "sv123",
        "Student",
        "Nguyễn Văn A",
        2,
        "SV20260001")
};

app.MapGet("/health", () => Results.Ok(new
{
    service = "AuthService",
    status = "Healthy"
}));

app.MapPost("/api/auth/login", (LoginRequest request) =>
{
    if (!users.TryGetValue(request.Username, out var user) ||
        user.Password != request.Password)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
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
    });
});

app.Run();

static string CreateDemoToken(DemoUser user)
{
    var raw = $"{user.Username}:{user.Role}:{user.StudentCode}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
}

public sealed record LoginRequest(string Username, string Password);

public sealed record DemoUser(
    string Username,
    string Password,
    string Role,
    string FullName,
    long? StudentId = null,
    string? StudentCode = null);
