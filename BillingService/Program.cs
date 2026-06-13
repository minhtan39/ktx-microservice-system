using System.Globalization;
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
    BillingStore store) =>
{
    var invoices = store.Read(data => data.MonthlyInvoices
        .Where(invoice => !studentId.HasValue || invoice.StudentId == studentId.Value)
        .Where(invoice => string.IsNullOrWhiteSpace(status) ||
            invoice.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
        .Where(invoice => string.IsNullOrWhiteSpace(period) ||
            invoice.BillingPeriod.Equals(period, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(invoice => invoice.IssuedAt)
        .ToList());

    return Results.Ok(invoices);
});

app.MapGet("/api/billing/monthly-invoices/{id:long}", (long id, BillingStore store) =>
{
    var invoice = store.Read(data => data.MonthlyInvoices.FirstOrDefault(item => item.Id == id));
    return invoice == null
        ? Results.NotFound(new { message = "Không tìm thấy phiếu thanh toán." })
        : Results.Ok(invoice);
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

app.MapGet("/api/billing/payment-history", (long? studentId, BillingStore store) =>
{
    var history = store.Read(data => data.PaymentHistory
        .Where(item => !studentId.HasValue || item.StudentId == studentId.Value)
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

app.MapGet("/api/incidents", (long? studentId, string? status, BillingStore store) =>
{
    var incidents = store.Read(data => data.Incidents
        .Where(item => !studentId.HasValue || item.StudentId == studentId.Value)
        .Where(item => string.IsNullOrWhiteSpace(status) ||
            item.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
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

app.MapPost("/api/incidents", (CreateIncidentRequest request, BillingStore store) =>
    CreateIncident(request, store));

app.MapMethods(
    "/api/incidents/{id:long}/status",
    ["PATCH", "PUT"],
    (long id, UpdateIncidentStatusRequest request, BillingStore store) =>
    {
        var nextStatus = NormalizeIncidentStatus(request.Status);

        if (nextStatus == null)
            return Results.BadRequest(new { message = "Status must be new, processing, done, or rejected." });

        var updated = store.Write(data =>
        {
            var index = data.Incidents.FindIndex(item => item.Id == id);

            if (index < 0)
                return null;

            var current = data.Incidents[index];
            var incident = current with
            {
                Status = nextStatus,
                HandledBy = string.IsNullOrWhiteSpace(request.HandledBy) ? current.HandledBy : request.HandledBy.Trim(),
                StaffNote = string.IsNullOrWhiteSpace(request.StaffNote) ? current.StaffNote : request.StaffNote.Trim(),
                UpdatedAt = DateTime.UtcNow
            };

            data.Incidents[index] = incident;
            return incident;
        });

        return updated == null
            ? Results.NotFound(new { message = "Incident request not found." })
            : Results.Ok(updated);
    });

app.MapGet("/api/maintenance", (BillingStore store) =>
    Results.Ok(store.Read(data => data.Incidents
        .OrderByDescending(item => item.CreatedAt)
        .ToList())));

app.MapPost("/api/maintenance", (CreateIncidentRequest request, BillingStore store) =>
    CreateIncident(request, store));

app.Run();

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

static IResult CreateIncident(CreateIncidentRequest request, BillingStore store)
{
    if (request.StudentId <= 0)
        return Results.BadRequest(new { message = "studentId is required." });

    if (string.IsNullOrWhiteSpace(request.RoomName) || string.IsNullOrWhiteSpace(request.Description))
        return Results.BadRequest(new { message = "roomName and description are required." });

    var incident = store.Write(data =>
    {
        var nextId = data.Incidents.Count == 0 ? 1 : data.Incidents.Max(item => item.Id) + 1;
        var created = new MaintenanceIncident(
            nextId,
            request.StudentId,
            string.IsNullOrWhiteSpace(request.StudentCode) ? $"SV-{request.StudentId}" : request.StudentCode.Trim(),
            string.IsNullOrWhiteSpace(request.StudentName) ? "Student" : request.StudentName.Trim(),
            request.RoomName.Trim(),
            string.IsNullOrWhiteSpace(request.Building) ? "-" : request.Building.Trim(),
            string.IsNullOrWhiteSpace(request.Category) ? "Other" : request.Category.Trim(),
            request.Description.Trim(),
            "new",
            DateTime.UtcNow,
            null,
            null,
            null);

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
        "processing" or "approved" or "inprogress" or "in-progress" => "processing",
        "done" or "completed" or "complete" => "done",
        "rejected" or "cancelled" => "rejected",
        _ => null
    };
}

public sealed record IncomingPayment(decimal Amount, string Content, string ReferenceCode);
