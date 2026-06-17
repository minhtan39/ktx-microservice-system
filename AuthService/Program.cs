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
builder.Services.AddSingleton<PasswordResetTokenStore>();
builder.Services.AddSingleton<PasswordResetEmailSender>();
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
        PasswordHasher.Hash("admin123"),
        "Admin",
        "Quản trị hệ thống");

    users["nhanvien"] = new(
        "nhanvien",
        PasswordHasher.Hash("staff123"),
        "Staff",
        "Nhân viên ký túc xá",
        EmployeeCode: "NV001",
        Email: "nhanvien@dormmanager.local",
        Department: "Kỹ thuật",
        JobTitle: "Nhân viên vận hành",
        AssignedArea: "Tòa A",
        AccountStatus: "Active",
        Permissions: DefaultStaffPermissions(),
        PasswordChangedAt: DateTimeOffset.UtcNow);

    users["SV20260001"] = new(
        "SV20260001",
        PasswordHasher.HashTemporaryPassword(),
        "Student",
        "Nguyễn Văn A",
        2,
        "SV20260001",
        AccountStatus: "Pending");

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
    var password = request.Password;

    users.TryGetValue(username, out var user);

    if (user == null)
    {
        await SynchronizeStudentAccountsAsync(accountStore, httpClientFactory);
        users.TryGetValue(username, out user);
    }

    if (user != null &&
        user.LockoutUntil.HasValue &&
        user.LockoutUntil.Value > DateTimeOffset.UtcNow)
    {
        return Results.Json(new
        {
            message = "Tai khoan dang bi tam khoa do nhap sai mat khau nhieu lan. Vui long thu lai sau."
        }, statusCode: StatusCodes.Status423Locked);
    }

    if (user != null && !user.AccountStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Json(new
        {
            message = user.AccountStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase)
                ? "Tài khoản đang chờ kích hoạt. Vui lòng mở email để đặt mật khẩu."
                : "Tài khoản đã bị khóa hoặc ngừng hoạt động."
        }, statusCode: StatusCodes.Status403Forbidden);
    }

    if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
    {
        if (user != null)
            RegisterFailedLogin(accountStore, user);

        return Results.Unauthorized();
    }

    var signedIn = user with
    {
        FailedLoginCount = 0,
        LockoutUntil = null,
        LastLoginAt = DateTimeOffset.UtcNow
    };

    accountStore.Upsert(signedIn);

    return Results.Ok(ToAuthResponse(signedIn));
});

app.MapPost("/api/auth/change-password", (
    ChangePasswordRequest request,
    HttpRequest httpRequest,
    PasswordResetTokenStore passwordResetTokens) =>
{
    var user = TryGetAuthenticatedUser(httpRequest, users);

    if (user == null)
        return Results.Unauthorized();

    if (!user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Json(
            new { message = "Chức năng này chỉ dành cho sinh viên." },
            statusCode: StatusCodes.Status403Forbidden);
    }

    var currentPassword = request.CurrentPassword.Trim();
    var newPassword = request.NewPassword.Trim();

    if (!PasswordHasher.Verify(currentPassword, user.PasswordHash))
    {
        return Results.BadRequest(new { message = "Mật khẩu hiện tại không đúng." });
    }

    if (!IsValidNewPassword(newPassword))
    {
        return Results.BadRequest(new
        {
            message = "Mật khẩu mới phải có ít nhất 8 ký tự, gồm cả chữ và số."
        });
    }

    if (PasswordHasher.Verify(newPassword, user.PasswordHash))
    {
        return Results.BadRequest(new
        {
            message = "Mật khẩu mới phải khác mật khẩu hiện tại."
        });
    }

    accountStore.Upsert(user with
    {
        PasswordHash = PasswordHasher.Hash(newPassword),
        PasswordChangedAt = DateTimeOffset.UtcNow,
        MustChangePassword = false,
        FailedLoginCount = 0,
        LockoutUntil = null
    });
    passwordResetTokens.InvalidateForUsername(user.Username);

    return Results.Ok(new { message = "Đổi mật khẩu thành công." });
});

app.MapPost("/api/auth/forgot-password", async (
    ForgotPasswordRequest request,
    IHttpClientFactory httpClientFactory,
    PasswordResetTokenStore passwordResetTokens,
    PasswordResetEmailSender emailSender) =>
{
    var studentCode = request.StudentCode.Trim();

    if (string.IsNullOrWhiteSpace(studentCode))
    {
        return Results.BadRequest(new { message = "Vui lòng nhập mã sinh viên." });
    }

    const string genericMessage =
        "Nếu mã sinh viên hợp lệ và hồ sơ có email, hệ thống sẽ gửi liên kết đặt lại mật khẩu.";

    await SynchronizeStudentAccountsAsync(accountStore, httpClientFactory);

    var account = users.Values.FirstOrDefault(user =>
        user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) &&
        (user.Username.Equals(studentCode, StringComparison.OrdinalIgnoreCase) ||
         user.StudentCode?.Equals(studentCode, StringComparison.OrdinalIgnoreCase) == true));

    if (account == null)
        return Results.Ok(new { message = genericMessage });

    var contact = await TryResolveStudentContactAsync(
        account.StudentId,
        account.StudentCode ?? studentCode,
        httpClientFactory);

    if (contact == null || string.IsNullOrWhiteSpace(contact.Email))
        return Results.Ok(new { message = genericMessage });

    if (!emailSender.IsConfigured)
    {
        return Results.Problem(
            title: "Dịch vụ email chưa được cấu hình",
            detail: "Admin cần cấu hình Gmail và App Password cho AuthService.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var lifetimeMinutes = Math.Clamp(
        app.Configuration.GetValue("PasswordReset:LifetimeMinutes", 30),
        5,
        120);
    var token = passwordResetTokens.Create(
        account.Username,
        TimeSpan.FromMinutes(lifetimeMinutes));
    var frontendBaseUrl = (app.Configuration["Frontend:BaseUrl"]
        ?? "http://localhost:5173").TrimEnd('/');
    var resetUrl = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";

    try
    {
        await emailSender.SendPasswordResetAsync(
            contact.Email,
            string.IsNullOrWhiteSpace(contact.FullName) ? account.FullName : contact.FullName,
            resetUrl);
    }
    catch (Exception exception)
    {
        passwordResetTokens.InvalidateForUsername(account.Username);
        app.Logger.LogError(exception, "Could not send password reset email.");

        return Results.Problem(
            title: "Không gửi được email đặt lại mật khẩu",
            detail: GetSafeEmailFailureDetail(exception),
            statusCode: StatusCodes.Status502BadGateway);
    }

    return Results.Ok(new { message = genericMessage });
});

app.MapGet("/api/auth/reset-password/validate", (
    string token,
    PasswordResetTokenStore passwordResetTokens) =>
{
    return passwordResetTokens.IsValid(token)
        ? Results.Ok(new { valid = true })
        : Results.BadRequest(new
        {
            valid = false,
            message = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn."
        });
});

app.MapPost("/api/auth/reset-password", (
    ResetPasswordRequest request,
    PasswordResetTokenStore passwordResetTokens) =>
{
    var newPassword = request.NewPassword.Trim();

    if (!IsValidNewPassword(newPassword))
    {
        return Results.BadRequest(new
        {
            message = "Mật khẩu mới phải có ít nhất 8 ký tự, gồm cả chữ và số."
        });
    }

    var username = passwordResetTokens.Consume(request.Token);

    if (username == null ||
        !users.TryGetValue(username, out var user) ||
        user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new
        {
            message = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn."
        });
    }

    accountStore.Upsert(user with
    {
        PasswordHash = PasswordHasher.Hash(newPassword),
        AccountStatus = "Active",
        PasswordChangedAt = DateTimeOffset.UtcNow,
        MustChangePassword = false,
        FailedLoginCount = 0,
        LockoutUntil = null
    });

    return Results.Ok(new { message = "Đặt lại mật khẩu thành công." });
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

    var existing = users.Values.FirstOrDefault(user =>
        user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) &&
        ((request.StudentId > 0 && user.StudentId == request.StudentId) ||
         user.StudentCode?.Equals(studentCode, StringComparison.OrdinalIgnoreCase) == true));

    var user = existing == null
        ? new DemoUser(
            studentCode,
            PasswordHasher.HashTemporaryPassword(),
            "Student",
            request.FullName.Trim(),
            request.StudentId,
            studentCode,
            AccountStatus: "Pending")
        : existing with
        {
            FullName = request.FullName.Trim(),
            StudentId = request.StudentId,
            StudentCode = studentCode
        };

    accountStore.Upsert(user);

    return Results.Ok(new
    {
        data = new
        {
            user.Username,
            user.Role,
            user.FullName,
            user.StudentId,
            user.StudentCode,
            user.AccountStatus,
            securityState = SecurityState(user),
            homePath = "/student/portal"
        }
    });
});

app.MapPost("/api/auth/student-accounts/invite", async (
    StudentAccountInviteRequest request,
    IHttpClientFactory httpClientFactory,
    PasswordResetTokenStore passwordResetTokens,
    PasswordResetEmailSender emailSender) =>
{
    var studentCode = request.StudentCode.Trim();

    if (string.IsNullOrWhiteSpace(studentCode))
        return Results.BadRequest(new { message = "StudentCode is required." });

    await SynchronizeStudentAccountsAsync(accountStore, httpClientFactory);

    var account = users.Values.FirstOrDefault(user =>
        user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) &&
        (user.Username.Equals(studentCode, StringComparison.OrdinalIgnoreCase) ||
         user.StudentCode?.Equals(studentCode, StringComparison.OrdinalIgnoreCase) == true));

    if (account == null)
    {
        account = new DemoUser(
            studentCode,
            PasswordHasher.HashTemporaryPassword(),
            "Student",
            string.IsNullOrWhiteSpace(request.FullName) ? studentCode : request.FullName.Trim(),
            request.StudentId,
            studentCode,
            AccountStatus: "Pending");

        accountStore.Upsert(account);
    }

    var contact = await TryResolveStudentContactAsync(
        request.StudentId,
        studentCode,
        httpClientFactory);

    var recipientEmail = string.IsNullOrWhiteSpace(request.Email)
        ? contact?.Email
        : request.Email.Trim();

    if (string.IsNullOrWhiteSpace(recipientEmail))
    {
        return Results.BadRequest(new
        {
            message = "Hồ sơ sinh viên chưa có email để gửi thư kích hoạt."
        });
    }

    if (!emailSender.IsConfigured)
    {
        return Results.Problem(
            title: "Dịch vụ email chưa được cấu hình",
            detail: "Admin cần cấu hình Gmail và App Password cho AuthService.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var recipientName = string.IsNullOrWhiteSpace(request.FullName)
        ? contact?.FullName ?? account.FullName
        : request.FullName.Trim();
    var lifetimeMinutes = Math.Clamp(
        app.Configuration.GetValue("PasswordReset:LifetimeMinutes", 30),
        5,
        120);
    var token = passwordResetTokens.Create(
        account.Username,
        TimeSpan.FromMinutes(lifetimeMinutes));
    var frontendBaseUrl = (app.Configuration["Frontend:BaseUrl"]
        ?? "http://localhost:5173").TrimEnd('/');
    var activationUrl = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}&welcome=1";

    try
    {
        await emailSender.SendStudentInvitationAsync(
            recipientEmail,
            recipientName,
            activationUrl,
            studentCode);
    }
    catch (Exception exception)
    {
        passwordResetTokens.InvalidateForUsername(account.Username);
        app.Logger.LogError(exception, "Could not send student invitation email.");

        return Results.Problem(
            title: "Không gửi được email kích hoạt",
            detail: GetSafeEmailFailureDetail(exception),
            statusCode: StatusCodes.Status502BadGateway);
    }

    return Results.Ok(new
    {
        message = $"Đã gửi email kích hoạt tài khoản đến {recipientEmail}."
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

app.MapPost("/api/auth/accounts/{username}/access-link", async (
    string username,
    HttpRequest request,
    IHttpClientFactory httpClientFactory,
    PasswordResetTokenStore passwordResetTokens,
    PasswordResetEmailSender emailSender) =>
{
    if (!IsAdminRequest(request))
        return Results.Unauthorized();

    if (!users.TryGetValue(username, out var account) ||
        account.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        return Results.NotFound(new { message = "Account not found." });
    }

    var recipientEmail = account.Email;
    var recipientName = account.FullName;

    if (account.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
    {
        var contact = await TryResolveStudentContactAsync(
            account.StudentId,
            account.StudentCode ?? account.Username,
            httpClientFactory);

        if (contact != null)
        {
            recipientEmail = string.IsNullOrWhiteSpace(contact.Email)
                ? recipientEmail
                : contact.Email;
            recipientName = string.IsNullOrWhiteSpace(contact.FullName)
                ? recipientName
                : contact.FullName;
        }
    }

    if (string.IsNullOrWhiteSpace(recipientEmail))
    {
        return Results.BadRequest(new
        {
            message = "Tai khoan chua co email de gui link bao mat."
        });
    }

    if (!emailSender.IsConfigured)
    {
        return Results.Problem(
            title: "Dich vu email chua duoc cau hinh",
            detail: "Admin can cau hinh Gmail va App Password cho AuthService.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var lifetimeMinutes = Math.Clamp(
        app.Configuration.GetValue("PasswordReset:LifetimeMinutes", 30),
        5,
        120);
    var token = passwordResetTokens.Create(
        account.Username,
        TimeSpan.FromMinutes(lifetimeMinutes));
    var frontendBaseUrl = (app.Configuration["Frontend:BaseUrl"]
        ?? "http://localhost:5173").TrimEnd('/');
    var isActivation = account.AccountStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase);
    var accessUrl = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}" +
        (isActivation ? "&welcome=1" : string.Empty);

    try
    {
        await emailSender.SendAccountAccessLinkAsync(
            recipientEmail,
            recipientName,
            accessUrl,
            account.Username,
            isActivation);
    }
    catch (Exception exception)
    {
        passwordResetTokens.InvalidateForUsername(account.Username);
        app.Logger.LogError(exception, "Could not send account access link.");

        return Results.Problem(
            title: "Khong gui duoc email bao mat",
            detail: GetSafeEmailFailureDetail(exception),
            statusCode: StatusCodes.Status502BadGateway);
    }

    return Results.Ok(new
    {
        message = isActivation
            ? $"Da gui email kich hoat den {recipientEmail}."
            : $"Da gui link dat lai mat khau den {recipientEmail}."
    });
});

app.MapPost("/api/auth/accounts", (
    CreateStaffAccountRequest create,
    HttpRequest request) =>
{
    if (!IsAdminRequest(request))
        return Results.Unauthorized();

    var username = create.Username.Trim();
    var employeeCode = create.EmployeeCode.Trim();
    var email = create.Email?.Trim();

    if (string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(employeeCode) ||
        string.IsNullOrWhiteSpace(create.FullName) ||
        string.IsNullOrWhiteSpace(email))
    {
        return Results.BadRequest(new { message = "Tên đăng nhập, mã nhân viên, họ tên và email là bắt buộc." });
    }

    if (users.ContainsKey(username))
        return Results.Conflict(new { message = "Tên đăng nhập đã tồn tại." });

    if (users.Values.Any(user =>
        !string.IsNullOrWhiteSpace(user.EmployeeCode) &&
        user.EmployeeCode.Equals(employeeCode, StringComparison.OrdinalIgnoreCase)))
    {
        return Results.Conflict(new { message = "Mã nhân viên đã tồn tại." });
    }

    var staff = new DemoUser(
        username,
        PasswordHasher.HashTemporaryPassword(),
        "Staff",
        create.FullName.Trim(),
        EmployeeCode: employeeCode,
        Email: email,
        Phone: create.Phone?.Trim(),
        Department: create.Department?.Trim(),
        JobTitle: create.JobTitle?.Trim(),
        AssignedArea: create.AssignedArea?.Trim(),
        AccountStatus: NormalizeInitialAccountStatus(create.AccountStatus),
        Permissions: NormalizePermissions(create.Permissions));

    accountStore.Upsert(staff);
    return Results.Ok(new { data = ToAccountResponse(staff) });
});

app.MapGet("/api/auth/staff", (HttpRequest request) =>
{
    if (!IsOperationalRequest(request))
        return Results.Unauthorized();

    var staff = users.Values
        .Where(user => user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        .OrderBy(user => user.FullName)
        .Select(user => new
        {
            user.Username,
            user.FullName,
            user.EmployeeCode,
            user.Department,
            user.JobTitle,
            user.AssignedArea,
            user.AccountStatus,
            permissions = EffectivePermissions(user)
        });

    return Results.Ok(new { data = staff });
});

app.MapPut("/api/auth/accounts/{username}", (
    string username,
    UpdateAccountRequest update,
    HttpRequest request,
    PasswordResetTokenStore passwordResetTokens) =>
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

    if (!string.IsNullOrWhiteSpace(update.Password))
    {
        return Results.BadRequest(new
        {
            message = "Admin khong duoc dat ho mat khau. Hay gui link dat lai mat khau cho nguoi dung."
        });
    }

    var nextFullName = string.IsNullOrWhiteSpace(update.FullName)
        ? current.FullName
        : update.FullName.Trim();

    if (string.IsNullOrWhiteSpace(nextUsername))
    {
        return Results.BadRequest(new { message = "Username is required." });
    }

    if (!nextUsername.Equals(username, StringComparison.OrdinalIgnoreCase) &&
        users.ContainsKey(nextUsername))
    {
        return Results.Conflict(new { message = "Username already exists." });
    }

    var updated = current with
    {
        Username = nextUsername,
        PasswordHash = current.PasswordHash,
        FullName = nextFullName,
        StudentCode = current.StudentCode,
        EmployeeCode = current.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase)
            ? CoalesceUpdate(update.EmployeeCode, current.EmployeeCode)
            : current.EmployeeCode,
        Email = CoalesceUpdate(update.Email, current.Email),
        Phone = CoalesceUpdate(update.Phone, current.Phone),
        Department = CoalesceUpdate(update.Department, current.Department),
        JobTitle = CoalesceUpdate(update.JobTitle, current.JobTitle),
        AssignedArea = CoalesceUpdate(update.AssignedArea, current.AssignedArea),
        AccountStatus = string.IsNullOrWhiteSpace(update.AccountStatus)
            ? current.AccountStatus
            : NormalizeAccountStatus(update.AccountStatus),
        Permissions = update.Permissions == null
            ? current.Permissions
            : NormalizePermissions(update.Permissions)
    };

    accountStore.Replace(username, updated);
    passwordResetTokens.InvalidateForUsername(username);
    passwordResetTokens.InvalidateForUsername(nextUsername);

    return Results.Ok(new { data = ToAccountResponse(updated) });
});

app.MapDelete("/api/auth/accounts/{username}", async (
    string username,
    HttpRequest request,
    IHttpClientFactory httpClientFactory,
    PasswordResetTokenStore passwordResetTokens) =>
{
    if (!IsAdminRequest(request))
        return Results.Unauthorized();

    if (!users.TryGetValue(username, out var account) ||
        account.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        return Results.NotFound(new { message = "Account not found." });
    }

    if (account.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) &&
        account.StudentId.HasValue)
    {
        var deleteProfileResult = await DeleteStudentProfileAsync(
            account.StudentId.Value,
            request,
            httpClientFactory);

        if (!deleteProfileResult.Success)
        {
            return Results.Problem(
                title: "Student profile could not be deleted",
                detail: deleteProfileResult.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    accountStore.Remove(username);
    passwordResetTokens.InvalidateForUsername(username);

    return Results.Ok(new
    {
        message = "Account deleted successfully.",
        studentProfileDeleted = account.Role.Equals(
            "Student",
            StringComparison.OrdinalIgnoreCase)
    });
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

static bool IsOperationalRequest(HttpRequest request)
{
    var authorization = request.Headers.Authorization.ToString();
    const string bearerPrefix = "Bearer ";

    if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        return false;

    try
    {
        var token = authorization[bearerPrefix.Length..].Trim();
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
        var parts = raw.Split(':');
        return parts.Length >= 2 &&
            (parts[1].Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
             parts[1].Equals("Staff", StringComparison.OrdinalIgnoreCase));
    }
    catch
    {
        return false;
    }
}

static DemoUser? TryGetAuthenticatedUser(
    HttpRequest request,
    IReadOnlyDictionary<string, DemoUser> users)
{
    var authorization = request.Headers.Authorization.ToString();
    const string bearerPrefix = "Bearer ";

    if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        return null;

    var token = authorization[bearerPrefix.Length..].Trim();

    try
    {
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
        var parts = raw.Split(':');

        return parts.Length >= 2 && users.TryGetValue(parts[0], out var user)
            ? user
            : null;
    }
    catch
    {
        return null;
    }
}

static bool IsValidNewPassword(string password)
{
    return !string.IsNullOrWhiteSpace(password) &&
        password.Length >= 8 &&
        password.Any(char.IsLetter) &&
        password.Any(char.IsDigit);
}

static string GetSafeEmailFailureDetail(Exception exception)
{
    var error = exception.ToString().ToLowerInvariant();

    if (error.Contains("5.7.0") ||
        error.Contains("5.7.8") ||
        error.Contains("authentication") ||
        error.Contains("credentials"))
    {
        return "Gmail từ chối đăng nhập. Hãy bật xác minh 2 bước và tạo lại App Password 16 ký tự.";
    }

    if (error.Contains("5.1.1") ||
        error.Contains("mailbox unavailable") ||
        error.Contains("recipient"))
    {
        return "Địa chỉ email trong hồ sơ sinh viên không hợp lệ hoặc không nhận được thư.";
    }

    if (error.Contains("timed out") ||
        error.Contains("timeout") ||
        error.Contains("socket") ||
        error.Contains("connection") ||
        error.Contains("network"))
    {
        return "VPS không kết nối được smtp.gmail.com qua cổng 587. Hãy kiểm tra firewall hoặc nhà cung cấp VPS.";
    }

    return "Không gửi được email. Hãy kiểm tra Gmail, App Password và email trong hồ sơ sinh viên.";
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
            employeeCode = user.EmployeeCode,
            email = user.Email,
            phone = user.Phone,
            department = user.Department,
            jobTitle = user.JobTitle,
            assignedArea = user.AssignedArea,
            accountStatus = user.AccountStatus,
            permissions = EffectivePermissions(user),
            homePath = user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase)
                ? "/student/portal"
                : user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase)
                    ? "/employee/dashboard"
                    : "/student-service/dashboard"
        }
    };
}

static object ToAccountResponse(DemoUser user)
{
    return new
    {
        user.Username,
        user.Role,
        user.FullName,
        user.StudentId,
        user.StudentCode,
        user.EmployeeCode,
        user.Email,
        user.Phone,
        user.Department,
        user.JobTitle,
        user.AssignedArea,
        user.AccountStatus,
        user.PasswordChangedAt,
        user.LastLoginAt,
        user.FailedLoginCount,
        user.LockoutUntil,
        user.MustChangePassword,
        passwordConfigured = user.PasswordChangedAt.HasValue,
        securityState = SecurityState(user),
        permissions = EffectivePermissions(user)
    };
}

static string CoalesceUpdate(string? next, string? current) =>
    next == null ? current ?? string.Empty : next.Trim();

static string NormalizeAccountStatus(string? status)
{
    return status?.Trim().ToLowerInvariant() switch
    {
        "pending" => "Pending",
        "locked" => "Locked",
        "inactive" => "Inactive",
        _ => "Active"
    };
}

static string NormalizeInitialAccountStatus(string? status)
{
    var normalized = NormalizeAccountStatus(status);
    return normalized.Equals("Active", StringComparison.OrdinalIgnoreCase)
        ? "Pending"
        : normalized;
}

static string SecurityState(DemoUser user)
{
    if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTimeOffset.UtcNow)
        return "TemporarilyLocked";

    if (user.AccountStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        return "PendingActivation";

    if (!user.PasswordChangedAt.HasValue || user.MustChangePassword)
        return "NeedsPasswordSetup";

    return "PasswordSet";
}

static void RegisterFailedLogin(AccountStore accountStore, DemoUser user)
{
    var failedCount = user.FailedLoginCount + 1;
    var lockoutUntil = failedCount >= 5
        ? DateTimeOffset.UtcNow.AddMinutes(15)
        : user.LockoutUntil;

    accountStore.Upsert(user with
    {
        FailedLoginCount = failedCount,
        LockoutUntil = lockoutUntil
    });
}

static string[] NormalizePermissions(IEnumerable<string>? permissions)
{
    var allowed = DefaultStaffPermissions().ToHashSet(StringComparer.OrdinalIgnoreCase);
    return (permissions ?? [])
        .Where(permission => allowed.Contains(permission))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(permission => permission)
        .ToArray();
}

static string[] EffectivePermissions(DemoUser user)
{
    if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        return DefaultStaffPermissions();

    if (!user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        return [];

    return user.Permissions is { Length: > 0 }
        ? NormalizePermissions(user.Permissions)
        : DefaultStaffPermissions();
}

static string[] DefaultStaffPermissions() =>
[
    "view_students",
    "approve_registrations",
    "manage_contracts",
    "view_rooms",
    "manage_incidents",
    "manage_maintenance",
    "issue_billing",
    "confirm_payments"
];

static async Task<(bool Success, string Message)> DeleteStudentProfileAsync(
    long studentId,
    HttpRequest incomingRequest,
    IHttpClientFactory httpClientFactory)
{
    try
    {
        var client = httpClientFactory.CreateClient("Gateway");
        using var deleteRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/students/{studentId}");

        var authorization = incomingRequest.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(authorization))
        {
            deleteRequest.Headers.TryAddWithoutValidation("Authorization", authorization);
        }

        using var response = await client.SendAsync(deleteRequest);

        if (response.IsSuccessStatusCode ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (true, string.Empty);
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        return (false, $"ContractStudentService returned {(int)response.StatusCode}: {responseBody}");
    }
    catch (HttpRequestException exception)
    {
        return (false, exception.Message);
    }
    catch (TaskCanceledException exception)
    {
        return (false, exception.Message);
    }
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
                PasswordHasher.HashTemporaryPassword(),
                "Student",
                string.IsNullOrWhiteSpace(fullName) ? studentCode : fullName,
                GetInt64(student, "id"),
                studentCode,
                AccountStatus: "Pending"));
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

static async Task<StudentContact?> TryResolveStudentContactAsync(
    long? studentId,
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
            var id = GetInt64(student, "id");
            var code = GetString(student, "studentCode");

            if ((studentId.HasValue && studentId.Value > 0 && id == studentId.Value) ||
                studentCode.Equals(code, StringComparison.OrdinalIgnoreCase))
            {
                return new StudentContact(
                    GetString(student, "email"),
                    GetString(student, "fullName"));
            }
        }
    }
    catch (HttpRequestException)
    {
        return null;
    }
    catch (TaskCanceledException)
    {
        return null;
    }
    catch (JsonException)
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

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public sealed record ForgotPasswordRequest(string StudentCode);

public sealed record ResetPasswordRequest(
    string Token,
    string NewPassword);

public sealed record StudentAccountRequest(
    long StudentId,
    string StudentCode,
    string FullName);

public sealed record StudentAccountInviteRequest(
    long StudentId,
    string StudentCode,
    string FullName,
    string? Email);

public sealed record UpdateAccountRequest(
    string? Username,
    string? Password,
    string? FullName,
    string? EmployeeCode = null,
    string? Email = null,
    string? Phone = null,
    string? Department = null,
    string? JobTitle = null,
    string? AssignedArea = null,
    string? AccountStatus = null,
    string[]? Permissions = null);

public sealed record CreateStaffAccountRequest(
    string Username,
    string EmployeeCode,
    string FullName,
    string? Email,
    string? Phone,
    string? Department,
    string? JobTitle,
    string? AssignedArea,
    string? AccountStatus,
    string[]? Permissions);

public sealed record DemoUser(
    string Username,
    string PasswordHash,
    string Role,
    string FullName,
    long? StudentId = null,
    string? StudentCode = null,
    string? EmployeeCode = null,
    string? Email = null,
    string? Phone = null,
    string? Department = null,
    string? JobTitle = null,
    string? AssignedArea = null,
    string AccountStatus = "Active",
    string[]? Permissions = null,
    DateTimeOffset? PasswordChangedAt = null,
    DateTimeOffset? LastLoginAt = null,
    int FailedLoginCount = 0,
    DateTimeOffset? LockoutUntil = null,
    bool MustChangePassword = false);

public sealed record StudentContact(string Email, string FullName);
