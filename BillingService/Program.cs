using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

const decimal ElectricityRate = 4_000m;
const decimal WaterRate = 20_000m;

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
builder.Services.AddSingleton<BillingStore>();
builder.Services.AddSingleton<BillingEmailSender>();
builder.Services.AddSingleton<PaymentQrBuilder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors("AllowGateway");

app.MapGet("/health", (BillingStore store, PaymentQrBuilder qrBuilder, BillingEmailSender emailSender) =>
    Results.Ok(new
    {
        service = "BillingService",
        status = "Healthy",
        monthlyInvoices = store.Read(data => data.MonthlyInvoices.Count),
        emailConfigured = emailSender.IsConfigured,
        paymentQrConfigured = qrBuilder.IsConfigured
    }));

app.MapGet("/api/billing/contracts", (BillingStore store) =>
    Results.Ok(store.Read(data => data.ContractItems.ToList())));

app.MapGet("/api/billing/contracts/{contractId:long}", (long contractId, BillingStore store) =>
{
    var items = store.Read(data => data.ContractItems
        .Where(item => item.ContractId == contractId)
        .ToList());

    return Results.Ok(new
    {
        contractId,
        totalAmount = items.Sum(item => item.Amount),
        items
    });
});

app.MapPost("/api/billing/contracts", (ContractBillingRequest request, BillingStore store) =>
{
    var result = store.Write(data =>
    {
        var existing = data.ContractItems
            .Where(item => item.ContractId == request.ContractId)
            .ToList();

        if (existing.Count > 0)
            return (Created: false, Items: existing);

        var dueDate = DateTime.UtcNow.Date.AddDays(7);
        var nextId = data.ContractItems.Count == 0
            ? 1
            : data.ContractItems.Max(item => item.Id) + 1;

        var created = new List<BillingItem>
        {
            new(
                nextId,
                request.ContractId,
                request.ContractCode,
                request.StudentId,
                "Deposit",
                request.DepositAmount,
                dueDate,
                "Unpaid"),
            new(
                nextId + 1,
                request.ContractId,
                request.ContractCode,
                request.StudentId,
                "FirstMonthRoomFee",
                request.MonthlyFee,
                dueDate,
                "Unpaid")
        };

        data.ContractItems.AddRange(created);
        return (Created: true, Items: created);
    });

    return Results.Ok(new
    {
        message = result.Created
            ? "Billing items created from contract."
            : "Billing already created for this contract.",
        contractId = request.ContractId,
        totalAmount = result.Items.Sum(item => item.Amount),
        items = result.Items
    });
});

app.MapGet("/api/billing/monthly-invoices", (
    long? studentId,
    string? status,
    string? period,
    HttpRequest httpRequest,
    BillingStore store) =>
{
    var effectiveStudentId = ResolveStudentScope(httpRequest, studentId);

    var invoices = store.Read(data => data.MonthlyInvoices
        .Where(invoice => !effectiveStudentId.HasValue || invoice.StudentId == effectiveStudentId.Value)
        .Where(invoice => string.IsNullOrWhiteSpace(status) ||
            invoice.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
        .Where(invoice => string.IsNullOrWhiteSpace(period) ||
            invoice.BillingPeriod.Equals(period, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(invoice => invoice.IssuedAt)
        .ToList());

    return Results.Ok(invoices);
});

app.MapGet("/api/billing/monthly-invoices/{id:long}", (
    long id,
    HttpRequest httpRequest,
    BillingStore store) =>
{
    var invoice = store.Read(data => data.MonthlyInvoices.FirstOrDefault(item => item.Id == id));

    if (invoice == null)
        return Results.NotFound(new { message = "Không tìm thấy phiếu thanh toán." });

    var effectiveStudentId = ResolveStudentScope(httpRequest, null);

    if (effectiveStudentId.HasValue && invoice.StudentId != effectiveStudentId.Value)
        return Results.Forbid();

    return Results.Ok(invoice);
});

app.MapPost("/api/billing/monthly-invoices", async Task<IResult> (
    CreateMonthlyInvoiceRequest request,
    BillingStore store,
    PaymentQrBuilder qrBuilder,
    BillingEmailSender emailSender) =>
{
    var validationError = ValidateInvoiceRequest(request);

    if (validationError != null)
        return Results.BadRequest(new { message = validationError });

    MonthlyInvoice? invoice = null;
    var duplicate = false;

    store.Write(data =>
    {
        var existing = data.MonthlyInvoices.FirstOrDefault(item =>
            item.ContractId == request.ContractId &&
            item.BillingPeriod.Equals(request.BillingPeriod, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            invoice = existing;
            duplicate = true;
            return false;
        }

        var nextId = data.MonthlyInvoices.Count == 0
            ? 1
            : data.MonthlyInvoices.Max(item => item.Id) + 1;
        var electricityUsage = request.CurrentElectricityReading - request.PreviousElectricityReading;
        var waterUsage = request.CurrentWaterReading - request.PreviousWaterReading;
        var paymentCode = BuildPaymentCode(request.BillingPeriod, request.StudentCode, nextId);
        var total = request.RoomFee + electricityUsage * ElectricityRate + waterUsage * WaterRate;

        invoice = new MonthlyInvoice
        {
            Id = nextId,
            InvoiceCode = $"PT-{request.BillingPeriod.Replace("-", string.Empty)}-{nextId:0000}",
            ContractId = request.ContractId,
            ContractCode = request.ContractCode?.Trim() ?? string.Empty,
            StudentId = request.StudentId,
            StudentCode = request.StudentCode?.Trim() ?? $"SV{request.StudentId}",
            StudentName = request.StudentName?.Trim() ?? "Sinh viên",
            StudentEmail = request.StudentEmail?.Trim() ?? string.Empty,
            RoomId = request.RoomId,
            RoomName = request.RoomName?.Trim() ?? request.RoomId.ToString(CultureInfo.InvariantCulture),
            BillingPeriod = request.BillingPeriod,
            RoomFee = request.RoomFee,
            PreviousElectricityReading = request.PreviousElectricityReading,
            CurrentElectricityReading = request.CurrentElectricityReading,
            ElectricityUsage = electricityUsage,
            ElectricityRate = ElectricityRate,
            ElectricityAmount = electricityUsage * ElectricityRate,
            PreviousWaterReading = request.PreviousWaterReading,
            CurrentWaterReading = request.CurrentWaterReading,
            WaterUsage = waterUsage,
            WaterRate = WaterRate,
            WaterAmount = waterUsage * WaterRate,
            TotalAmount = total,
            DueDate = request.DueDate?.Date ?? DateTime.UtcNow.Date.AddDays(7),
            Status = "Unpaid",
            PaymentCode = paymentCode,
            QrCodeUrl = qrBuilder.Build(total, paymentCode),
            IssuedBy = string.IsNullOrWhiteSpace(request.IssuedBy) ? "Nhân viên" : request.IssuedBy.Trim(),
            IssuedAt = DateTime.UtcNow
        };

        data.MonthlyInvoices.Add(invoice);
        return true;
    });

    if (duplicate)
    {
        return Results.Conflict(new
        {
            message = "Hợp đồng này đã có phiếu thanh toán trong tháng đã chọn.",
            invoice
        });
    }

    var emailResult = await TrySendInvoiceEmailAsync(invoice!, store, emailSender);

    return Results.Ok(new
    {
        message = emailResult.Sent
            ? "Đã phát hành phiếu và gửi email cho sinh viên."
            : "Đã phát hành phiếu nhưng chưa gửi được email.",
        invoice,
        emailSent = emailResult.Sent,
        emailError = emailResult.Error
    });
});

app.MapPost("/api/billing/monthly-invoices/{id:long}/resend-email", async Task<IResult> (
    long id,
    BillingStore store,
    BillingEmailSender emailSender) =>
{
    var invoice = store.Read(data => data.MonthlyInvoices.FirstOrDefault(item => item.Id == id));

    if (invoice == null)
        return Results.NotFound(new { message = "Không tìm thấy phiếu thanh toán." });

    var result = await TrySendInvoiceEmailAsync(invoice, store, emailSender);

    return result.Sent
        ? Results.Ok(new { message = "Đã gửi lại phiếu thanh toán qua email." })
        : Results.Problem(
            title: "Không gửi được email",
            detail: result.Error,
            statusCode: StatusCodes.Status502BadGateway);
});

app.MapPost("/api/billing/monthly-invoices/{id:long}/mark-paid", (
    long id,
    MarkInvoicePaidRequest request,
    BillingStore store) =>
{
    var updated = store.Write(data => CompleteInvoicePayment(
        data,
        id,
        request.Amount,
        request.ReferenceCode,
        "Manual",
        string.IsNullOrWhiteSpace(request.ConfirmedBy) ? "Nhân viên" : request.ConfirmedBy.Trim()));

    return updated == null
        ? Results.NotFound(new { message = "Không tìm thấy phiếu thanh toán." })
        : Results.Ok(new { message = "Đã xác nhận thanh toán.", invoice = updated });
});

app.MapGet("/api/billing/payment-history", (
    long? studentId,
    HttpRequest httpRequest,
    BillingStore store) =>
{
    var effectiveStudentId = ResolveStudentScope(httpRequest, studentId);

    var history = store.Read(data => data.PaymentHistory
        .Where(item => !effectiveStudentId.HasValue || item.StudentId == effectiveStudentId.Value)
        .OrderByDescending(item => item.PaidAt)
        .ToList());

    return Results.Ok(history);
});

app.MapPost("/api/billing/webhooks/payment", async Task<IResult> (
    HttpRequest request,
    BillingStore store,
    IConfiguration configuration) =>
{
    var configuredSecret = configuration["Payment:WebhookSecret"]?.Trim();

    if (string.IsNullOrWhiteSpace(configuredSecret))
    {
        return Results.Problem(
            title: "Webhook chưa được cấu hình",
            detail: "Admin cần cấu hình PAYMENT_WEBHOOK_SECRET cho BillingService.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var suppliedSecret = request.Headers["X-Webhook-Secret"].FirstOrDefault()
        ?? request.Query["secret"].FirstOrDefault();

    if (!configuredSecret.Equals(suppliedSecret, StringComparison.Ordinal))
        return Results.Unauthorized();

    using var body = await JsonDocument.ParseAsync(request.Body);
    var payments = ExtractIncomingPayments(body.RootElement).ToList();
    var completed = new List<MonthlyInvoice>();

    store.Write(data =>
    {
        foreach (var payment in payments)
        {
            var invoice = data.MonthlyInvoices.FirstOrDefault(item =>
                item.Status.Equals("Unpaid", StringComparison.OrdinalIgnoreCase) &&
                payment.Content.Contains(item.PaymentCode, StringComparison.OrdinalIgnoreCase) &&
                payment.Amount >= item.TotalAmount);

            if (invoice == null)
                continue;

            var paidInvoice = CompleteInvoicePayment(
                data,
                invoice.Id,
                payment.Amount,
                payment.ReferenceCode,
                "BankWebhook",
                "Ngân hàng");

            if (paidInvoice != null)
                completed.Add(paidInvoice);
        }

        return true;
    });

    return Results.Ok(new
    {
        success = true,
        received = payments.Count,
        completed = completed.Select(item => item.InvoiceCode).ToList()
    });
});

app.MapGet("/api/incidents", (
    long? studentId,
    string? status,
    string? assignedTo,
    string? priority,
    BillingStore store) =>
{
    var incidents = store.Read(data => data.Incidents
        .Where(item => !studentId.HasValue || item.StudentId == studentId.Value)
        .Where(item => string.IsNullOrWhiteSpace(status) ||
            item.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
        .Where(item => string.IsNullOrWhiteSpace(assignedTo) ||
            string.Equals(item.AssignedTo, assignedTo, StringComparison.OrdinalIgnoreCase))
        .Where(item => string.IsNullOrWhiteSpace(priority) ||
            item.Priority.Equals(priority, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(item => item.CreatedAt)
        .ToList());

    return Results.Ok(incidents);
});

app.MapGet("/api/incidents/{id:long}", (long id, BillingStore store) =>
{
    var incident = store.Read(data => data.Incidents.FirstOrDefault(item => item.Id == id));
    return incident == null
        ? Results.NotFound(new { message = "Incident request not found." })
        : Results.Ok(incident);
});

app.MapPost("/api/incidents", (
    CreateIncidentRequest request,
    HttpRequest httpRequest,
    BillingStore store) =>
    CreateIncident(request, GetRequestIdentity(httpRequest), store));

app.MapMethods(
    "/api/incidents/{id:long}/assign",
    ["PATCH", "PUT"],
    (long id, AssignIncidentRequest request, HttpRequest httpRequest, BillingStore store) =>
    {
        var identity = GetRequestIdentity(httpRequest);

        if (!identity.IsOperational)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.AssignedTo))
            return Results.BadRequest(new { message = "Vui lòng chọn nhân viên phụ trách." });

        if (!identity.IsAdmin &&
            !request.AssignedTo.Trim().Equals(identity.Username, StringComparison.OrdinalIgnoreCase))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var updated = store.Write(data =>
        {
            var incident = data.Incidents.FirstOrDefault(item => item.Id == id);
            if (incident == null) return null;

            incident.AssignedTo = request.AssignedTo.Trim();
            incident.AssignedName = string.IsNullOrWhiteSpace(request.AssignedName)
                ? request.AssignedTo.Trim()
                : request.AssignedName.Trim();
            incident.Priority = NormalizePriority(request.Priority) ?? incident.Priority;
            incident.ScheduledAt = request.ScheduledAt ?? incident.ScheduledAt;
            incident.DueAt = request.DueAt ?? incident.DueAt;
            incident.Status = incident.Status == "new" ? "assigned" : incident.Status;
            incident.UpdatedAt = DateTime.UtcNow;
            AddIncidentTimeline(incident, "assigned", identity.Username, request.Note);
            return incident;
        });

        return updated == null
            ? Results.NotFound(new { message = "Incident request not found." })
            : Results.Ok(updated);
    });

app.MapMethods(
    "/api/incidents/{id:long}/status",
    ["PATCH", "PUT"],
    (long id, UpdateIncidentStatusRequest request, HttpRequest httpRequest, BillingStore store) =>
    {
        var identity = GetRequestIdentity(httpRequest);

        if (!identity.IsOperational)
            return Results.Unauthorized();

        var nextStatus = NormalizeIncidentStatus(request.Status);

        if (nextStatus == null)
            return Results.BadRequest(new { message = "Trạng thái yêu cầu không hợp lệ." });

        var existing = store.Read(data => data.Incidents.FirstOrDefault(item => item.Id == id));

        if (existing == null)
            return Results.NotFound(new { message = "Incident request not found." });

        if (!identity.IsAdmin &&
            !string.Equals(existing.AssignedTo, identity.Username, StringComparison.OrdinalIgnoreCase))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (nextStatus == "completed" &&
            string.IsNullOrWhiteSpace(request.Resolution) &&
            string.IsNullOrWhiteSpace(existing.Resolution))
        {
            return Results.BadRequest(new { message = "Vui lòng nhập kết quả xử lý trước khi hoàn thành." });
        }

        var updated = store.Write(data =>
        {
            var incident = data.Incidents.FirstOrDefault(item => item.Id == id);
            if (incident == null) return null;

            incident.Status = nextStatus;
            incident.HandledBy = string.IsNullOrWhiteSpace(request.HandledBy)
                ? identity.Username
                : request.HandledBy.Trim();
            incident.StaffNote = string.IsNullOrWhiteSpace(request.StaffNote)
                ? incident.StaffNote
                : request.StaffNote.Trim();
            incident.RootCause = string.IsNullOrWhiteSpace(request.RootCause)
                ? incident.RootCause
                : request.RootCause.Trim();
            incident.Resolution = string.IsNullOrWhiteSpace(request.Resolution)
                ? incident.Resolution
                : request.Resolution.Trim();
            incident.MaterialCost = request.MaterialCost ?? incident.MaterialCost;
            incident.LaborCost = request.LaborCost ?? incident.LaborCost;
            incident.AfterImageUrls = request.AfterImageUrls ?? incident.AfterImageUrls;
            incident.UpdatedAt = DateTime.UtcNow;

            if (nextStatus == "completed")
                incident.CompletedAt = DateTime.UtcNow;

            AddIncidentTimeline(incident, "status-updated", identity.Username, request.StaffNote);
            return incident;
        });

        return updated == null
            ? Results.NotFound(new { message = "Incident request not found." })
            : Results.Ok(updated);
    });

app.MapPost("/api/incidents/{id:long}/confirm", (
    long id,
    StudentIncidentActionRequest request,
    HttpRequest httpRequest,
    BillingStore store) =>
{
    var identity = GetRequestIdentity(httpRequest);
    var updated = store.Write(data =>
    {
        var incident = data.Incidents.FirstOrDefault(item => item.Id == id);
        if (incident == null) return null;
        if (!identity.IsAdmin && !identity.IsStudentOwner(incident)) return new MaintenanceIncident { Id = -1 };
        if (incident.Status != "completed") return new MaintenanceIncident { Id = -2 };

        incident.Status = "confirmed";
        incident.StudentConfirmedAt = DateTime.UtcNow;
        incident.UpdatedAt = DateTime.UtcNow;
        AddIncidentTimeline(incident, "student-confirmed", identity.Username, request.Note);
        return incident;
    });

    if (updated?.Id == -1) return Results.Unauthorized();
    if (updated?.Id == -2) return Results.BadRequest(new { message = "Yêu cầu chưa ở trạng thái hoàn thành." });
    return updated == null ? Results.NotFound() : Results.Ok(updated);
});

app.MapPost("/api/incidents/{id:long}/reopen", (
    long id,
    StudentIncidentActionRequest request,
    HttpRequest httpRequest,
    BillingStore store) =>
{
    var identity = GetRequestIdentity(httpRequest);
    var updated = store.Write(data =>
    {
        var incident = data.Incidents.FirstOrDefault(item => item.Id == id);
        if (incident == null) return null;
        if (!identity.IsAdmin && !identity.IsStudentOwner(incident)) return new MaintenanceIncident { Id = -1 };
        if (incident.Status is not ("completed" or "confirmed")) return new MaintenanceIncident { Id = -2 };

        incident.Status = "reopened";
        incident.ReopenedAt = DateTime.UtcNow;
        incident.UpdatedAt = DateTime.UtcNow;
        AddIncidentTimeline(incident, "student-reopened", identity.Username, request.Note);
        return incident;
    });

    if (updated?.Id == -1) return Results.Unauthorized();
    if (updated?.Id == -2) return Results.BadRequest(new { message = "Yêu cầu chưa thể mở lại." });
    return updated == null ? Results.NotFound() : Results.Ok(updated);
});

app.MapGet("/api/maintenance", (string? assignedTo, string? status, BillingStore store) =>
    Results.Ok(store.Read(data => data.MaintenancePlans
        .Where(item => string.IsNullOrWhiteSpace(assignedTo) ||
            string.Equals(item.AssignedTo, assignedTo, StringComparison.OrdinalIgnoreCase))
        .Where(item => string.IsNullOrWhiteSpace(status) ||
            item.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
        .OrderBy(item => item.NextDueDate)
        .ToList())));

app.MapPost("/api/maintenance", (
    CreateMaintenancePlanRequest request,
    HttpRequest httpRequest,
    BillingStore store) =>
{
    var identity = GetRequestIdentity(httpRequest);
    if (!identity.IsAdmin) return Results.StatusCode(StatusCodes.Status403Forbidden);

    if (string.IsNullOrWhiteSpace(request.Title) ||
        string.IsNullOrWhiteSpace(request.AssetName) ||
        string.IsNullOrWhiteSpace(request.Location))
    {
        return Results.BadRequest(new { message = "Vui lòng nhập tên công việc, thiết bị và vị trí." });
    }

    var plan = store.Write(data =>
    {
        var nextId = data.MaintenancePlans.Count == 0 ? 1 : data.MaintenancePlans.Max(item => item.Id) + 1;
        var created = new MaintenancePlan
        {
            Id = nextId,
            Title = request.Title.Trim(),
            AssetCode = request.AssetCode?.Trim() ?? string.Empty,
            AssetName = request.AssetName.Trim(),
            Location = request.Location.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Other" : request.Category.Trim(),
            Frequency = string.IsNullOrWhiteSpace(request.Frequency) ? "Monthly" : request.Frequency.Trim(),
            NextDueDate = request.NextDueDate,
            AssignedTo = request.AssignedTo?.Trim(),
            AssignedName = request.AssignedName?.Trim(),
            Checklist = request.Checklist ?? [],
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = identity.Username
        };
        data.MaintenancePlans.Add(created);
        return created;
    });

    return Results.Ok(plan);
});

app.MapMethods(
    "/api/maintenance/{id:long}",
    ["PATCH", "PUT"],
    (long id, UpdateMaintenancePlanRequest request, HttpRequest httpRequest, BillingStore store) =>
    {
        var identity = GetRequestIdentity(httpRequest);
        if (!identity.IsOperational) return Results.Unauthorized();

        var existing = store.Read(data => data.MaintenancePlans.FirstOrDefault(item => item.Id == id));
        if (existing == null) return Results.NotFound();

        if (!identity.IsAdmin &&
            !string.Equals(existing.AssignedTo, identity.Username, StringComparison.OrdinalIgnoreCase))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var plan = store.Write(data =>
        {
            var current = data.MaintenancePlans.FirstOrDefault(item => item.Id == id);
            if (current == null) return null;

            current.Status = NormalizeMaintenanceStatus(request.Status) ?? current.Status;
            if (identity.IsAdmin)
            {
                current.AssignedTo = string.IsNullOrWhiteSpace(request.AssignedTo) ? current.AssignedTo : request.AssignedTo.Trim();
                current.AssignedName = string.IsNullOrWhiteSpace(request.AssignedName) ? current.AssignedName : request.AssignedName.Trim();
            }
            current.NextDueDate = request.NextDueDate ?? current.NextDueDate;
            current.CompletedItems = request.CompletedItems ?? current.CompletedItems;
            current.Cost = request.Cost ?? current.Cost;
            current.Notes = string.IsNullOrWhiteSpace(request.Notes) ? current.Notes : request.Notes.Trim();
            current.UpdatedAt = DateTime.UtcNow;
            current.UpdatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? identity.Username : request.UpdatedBy.Trim();

            if (current.Status == "completed")
            {
                current.LastCompletedAt = DateTime.UtcNow;
                current.NextDueDate = request.NextDueDate ?? CalculateNextMaintenanceDate(current.NextDueDate, current.Frequency);
            }

            return current;
        });

        return plan == null ? Results.NotFound() : Results.Ok(plan);
    });

app.Run();

static long? ResolveStudentScope(HttpRequest request, long? requestedStudentId)
{
    var authorization = request.Headers.Authorization.ToString();
    const string bearerPrefix = "Bearer ";

    if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        return requestedStudentId;

    try
    {
        var token = authorization[bearerPrefix.Length..].Trim();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var role = jwt.Claims.FirstOrDefault(claim =>
            claim.Type == ClaimTypes.Role || claim.Type == "role")?.Value;

        if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
            return requestedStudentId;

        var rawStudentId = jwt.Claims.FirstOrDefault(claim => claim.Type == "studentId")?.Value;

        return long.TryParse(rawStudentId, out var studentId)
            ? studentId
            : requestedStudentId;
    }
    catch
    {
        return requestedStudentId;
    }
}

static string? ValidateInvoiceRequest(CreateMonthlyInvoiceRequest request)
{
    if (request.ContractId <= 0 || request.StudentId <= 0)
        return "Vui lòng chọn sinh viên và hợp đồng hợp lệ.";

    if (!Regex.IsMatch(request.BillingPeriod ?? string.Empty, @"^\d{4}-(0[1-9]|1[0-2])$"))
        return "Kỳ thanh toán phải có định dạng yyyy-MM.";

    if (request.RoomFee < 0)
        return "Tiền phòng không được âm.";

    if (request.CurrentElectricityReading < request.PreviousElectricityReading)
        return "Chỉ số điện mới phải lớn hơn hoặc bằng chỉ số cũ.";

    if (request.CurrentWaterReading < request.PreviousWaterReading)
        return "Chỉ số nước mới phải lớn hơn hoặc bằng chỉ số cũ.";

    if (string.IsNullOrWhiteSpace(request.StudentEmail))
        return "Hồ sơ sinh viên chưa có email để nhận phiếu thanh toán.";

    return null;
}

static string BuildPaymentCode(string period, string? studentCode, long id)
{
    var safeCode = Regex.Replace(studentCode ?? "SV", "[^A-Za-z0-9]", string.Empty)
        .ToUpperInvariant();
    return $"KTX{period.Replace("-", string.Empty)}{safeCode}{id}";
}

static async Task<(bool Sent, string? Error)> TrySendInvoiceEmailAsync(
    MonthlyInvoice invoice,
    BillingStore store,
    BillingEmailSender emailSender)
{
    if (!emailSender.IsConfigured)
        return (false, "BillingService chưa được cấu hình Gmail và App Password.");

    try
    {
        await emailSender.SendInvoiceAsync(invoice);
        store.Write(data =>
        {
            var saved = data.MonthlyInvoices.First(item => item.Id == invoice.Id);
            saved.EmailSentAt = DateTime.UtcNow;
            return true;
        });
        return (true, null);
    }
    catch (Exception exception)
    {
        return (false, GetSafeEmailFailureDetail(exception));
    }
}

static string GetSafeEmailFailureDetail(Exception exception)
{
    var error = exception.ToString().ToLowerInvariant();

    if (error.Contains("authentication") || error.Contains("credentials") || error.Contains("5.7.8"))
        return "Gmail từ chối đăng nhập. Hãy kiểm tra App Password 16 ký tự.";

    if (error.Contains("recipient") || error.Contains("5.1.1") || error.Contains("mailbox unavailable"))
        return "Email trong hồ sơ sinh viên không hợp lệ hoặc không nhận được thư.";

    if (error.Contains("timeout") || error.Contains("socket") || error.Contains("connection"))
        return "VPS không kết nối được máy chủ Gmail qua cổng 587.";

    return "Không gửi được email phiếu thanh toán. Hãy kiểm tra cấu hình Gmail.";
}

static MonthlyInvoice? CompleteInvoicePayment(
    BillingData data,
    long invoiceId,
    decimal? amount,
    string? referenceCode,
    string method,
    string confirmedBy)
{
    var invoice = data.MonthlyInvoices.FirstOrDefault(item => item.Id == invoiceId);

    if (invoice == null)
        return null;

    if (invoice.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
        return invoice;

    var paidAt = DateTime.UtcNow;
    var paidAmount = amount ?? invoice.TotalAmount;
    var reference = string.IsNullOrWhiteSpace(referenceCode)
        ? $"MANUAL-{paidAt:yyyyMMddHHmmss}"
        : referenceCode.Trim();

    invoice.Status = "Paid";
    invoice.PaidAt = paidAt;
    invoice.PaidAmount = paidAmount;
    invoice.PaymentReference = reference;
    invoice.PaymentMethod = method;

    var nextHistoryId = data.PaymentHistory.Count == 0
        ? 1
        : data.PaymentHistory.Max(item => item.Id) + 1;

    data.PaymentHistory.Add(new PaymentHistoryEntry(
        nextHistoryId,
        invoice.Id,
        invoice.InvoiceCode,
        invoice.StudentId,
        invoice.StudentCode,
        invoice.StudentName,
        invoice.BillingPeriod,
        paidAmount,
        method,
        reference,
        confirmedBy,
        paidAt));

    return invoice;
}

static IEnumerable<IncomingPayment> ExtractIncomingPayments(JsonElement element)
{
    if (element.ValueKind == JsonValueKind.Array)
    {
        foreach (var child in element.EnumerateArray())
        foreach (var payment in ExtractIncomingPayments(child))
            yield return payment;

        yield break;
    }

    if (element.ValueKind != JsonValueKind.Object)
        yield break;

    var amount = GetDecimal(element, "transferAmount", "amount", "transactionAmount");
    var content = GetString(element, "content", "description", "transactionContent", "remark");

    if (amount > 0 && !string.IsNullOrWhiteSpace(content))
    {
        yield return new IncomingPayment(
            amount,
            content,
            GetString(element, "referenceCode", "reference", "transactionId", "code"));
    }

    foreach (var property in element.EnumerateObject())
    {
        if (property.Value.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            continue;

        foreach (var payment in ExtractIncomingPayments(property.Value))
            yield return payment;
    }
}

static string GetString(JsonElement element, params string[] names)
{
    foreach (var property in element.EnumerateObject())
    {
        if (names.Any(name => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return property.Value.ToString();
    }

    return string.Empty;
}

static decimal GetDecimal(JsonElement element, params string[] names)
{
    var value = GetString(element, names);
    return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)
        ? amount
        : 0;
}

static IResult CreateIncident(CreateIncidentRequest request, RequestIdentity identity, BillingStore store)
{
    if (request.StudentId <= 0)
        return Results.BadRequest(new { message = "studentId is required." });

    if (string.IsNullOrWhiteSpace(request.RoomName) || string.IsNullOrWhiteSpace(request.Description))
        return Results.BadRequest(new { message = "roomName and description are required." });

    var incident = store.Write(data =>
    {
        var nextId = data.Incidents.Count == 0 ? 1 : data.Incidents.Max(item => item.Id) + 1;
        var created = new MaintenanceIncident
        {
            Id = nextId,
            StudentId = request.StudentId,
            StudentCode = string.IsNullOrWhiteSpace(request.StudentCode) ? $"SV-{request.StudentId}" : request.StudentCode.Trim(),
            StudentName = string.IsNullOrWhiteSpace(request.StudentName) ? "Student" : request.StudentName.Trim(),
            RoomName = request.RoomName.Trim(),
            Building = string.IsNullOrWhiteSpace(request.Building) ? "-" : request.Building.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Other" : request.Category.Trim(),
            Description = request.Description.Trim(),
            Priority = NormalizePriority(request.Priority) ?? "normal",
            Status = "new",
            CreatedAt = DateTime.UtcNow,
            PreferredVisitAt = request.PreferredVisitAt,
            ContactPhone = request.ContactPhone?.Trim(),
            ImageUrls = request.ImageUrls ?? []
        };

        AddIncidentTimeline(created, "created", identity.Username, "Sinh viên gửi yêu cầu sửa chữa");

        data.Incidents.Add(created);
        return created;
    });

    return Results.Ok(incident);
}

static string? NormalizeIncidentStatus(string status)
{
    var normalized = status.Trim().ToLowerInvariant();

    return normalized switch
    {
        "new" or "pending" => "new",
        "accepted" or "received" => "accepted",
        "assigned" => "assigned",
        "processing" or "approved" or "inprogress" or "in-progress" => "processing",
        "waiting-materials" or "waitingmaterials" or "waiting" => "waiting-materials",
        "done" or "completed" or "complete" => "completed",
        "confirmed" => "confirmed",
        "reopened" => "reopened",
        "rejected" => "rejected",
        "cancelled" or "canceled" => "cancelled",
        _ => null
    };
}

static string? NormalizePriority(string? priority)
{
    return priority?.Trim().ToLowerInvariant() switch
    {
        "low" => "low",
        "normal" or "medium" => "normal",
        "high" => "high",
        "urgent" or "critical" => "urgent",
        _ => null
    };
}

static string? NormalizeMaintenanceStatus(string? status)
{
    return status?.Trim().ToLowerInvariant() switch
    {
        "scheduled" => "scheduled",
        "in-progress" or "processing" => "in-progress",
        "waiting-materials" or "waiting" => "waiting-materials",
        "completed" or "done" => "completed",
        "cancelled" or "canceled" => "cancelled",
        _ => null
    };
}

static void AddIncidentTimeline(MaintenanceIncident incident, string action, string actor, string? note)
{
    incident.Timeline ??= [];
    incident.Timeline.Add(new IncidentTimelineEntry(
        DateTime.UtcNow,
        action,
        incident.Status,
        string.IsNullOrWhiteSpace(actor) ? "system" : actor,
        string.IsNullOrWhiteSpace(note) ? null : note.Trim()));
}

static DateTime CalculateNextMaintenanceDate(DateTime current, string frequency)
{
    return frequency.Trim().ToLowerInvariant() switch
    {
        "weekly" => current.AddDays(7),
        "quarterly" => current.AddMonths(3),
        "yearly" or "annual" => current.AddYears(1),
        _ => current.AddMonths(1)
    };
}

static RequestIdentity GetRequestIdentity(HttpRequest request)
{
    var authorization = request.Headers.Authorization.ToString();
    const string bearerPrefix = "Bearer ";

    if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        return new RequestIdentity("anonymous", string.Empty, string.Empty);

    try
    {
        var token = authorization[bearerPrefix.Length..].Trim();
        var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
        var parts = raw.Split(':');
        return new RequestIdentity(
            parts.ElementAtOrDefault(0) ?? "anonymous",
            parts.ElementAtOrDefault(1) ?? string.Empty,
            parts.ElementAtOrDefault(2) ?? string.Empty);
    }
    catch
    {
        return new RequestIdentity("anonymous", string.Empty, string.Empty);
    }
}

public sealed record IncomingPayment(decimal Amount, string Content, string ReferenceCode);

public sealed record RequestIdentity(string Username, string Role, string StudentCode)
{
    public bool IsAdmin => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    public bool IsOperational => IsAdmin || Role.Equals("Staff", StringComparison.OrdinalIgnoreCase);
    public bool IsStudentOwner(MaintenanceIncident incident) =>
        Role.Equals("Student", StringComparison.OrdinalIgnoreCase) &&
        StudentCode.Equals(incident.StudentCode, StringComparison.OrdinalIgnoreCase);
}
