using System.Globalization;
using System.Net.Http.Json;
using System.Text;
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
builder.Services.AddHttpClient("RoomService", client =>
{
    var roomServiceBaseUrl = builder.Configuration["Integration:RoomServiceBaseUrl"]
        ?? "http://room-service:8080";

    client.BaseAddress = new Uri(roomServiceBaseUrl);
});

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
        var (_, _, billingDays) = GetBillingPeriodRange(request.BillingPeriod);

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
            BatchCode = string.Empty,
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
            RoomElectricityUsage = electricityUsage,
            RoomElectricityAmount = electricityUsage * ElectricityRate,
            RoomWaterUsage = waterUsage,
            RoomWaterAmount = waterUsage * WaterRate,
            RoomOccupantCount = 1,
            OccupancyDays = billingDays,
            BillingDays = billingDays,
            AllocationMethod = "Manual",
            AllocationNote = "Phiếu lập trực tiếp cho từng sinh viên.",
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

app.MapPost("/api/billing/monthly-invoices/room/preview", (
    CreateRoomMonthlyInvoicesRequest request) =>
{
    var previewResult = BuildRoomInvoicePreview(request);

    return previewResult.Error == null
        ? Results.Ok(new
        {
            roomId = request.RoomId,
            roomName = NormalizeRoomName(request.RoomName, request.RoomId),
            billingPeriod = request.BillingPeriod,
            electricityUsage = previewResult.ElectricityUsage,
            electricityAmount = previewResult.ElectricityAmount,
            waterUsage = previewResult.WaterUsage,
            waterAmount = previewResult.WaterAmount,
            occupantCount = previewResult.Allocations.Count,
            totalAmount = previewResult.Allocations.Sum(item => item.TotalAmount),
            allocationMethod = "OccupancyDays",
            allocations = previewResult.Allocations
        })
        : Results.BadRequest(new { message = previewResult.Error });
});

app.MapPost("/api/billing/monthly-invoices/room/issue", async Task<IResult> (
    CreateRoomMonthlyInvoicesRequest request,
    BillingStore store,
    PaymentQrBuilder qrBuilder,
    BillingEmailSender emailSender) =>
{
    var previewResult = BuildRoomInvoicePreview(request);

    if (previewResult.Error != null)
        return Results.BadRequest(new { message = previewResult.Error });

    var roomName = NormalizeRoomName(request.RoomName, request.RoomId);
    var periodDigits = request.BillingPeriod.Replace("-", string.Empty);
    var safeRoom = request.RoomId.ToString(CultureInfo.InvariantCulture);
    var batchCode = $"PB-{periodDigits}-P{safeRoom}";
    var issuedBy = string.IsNullOrWhiteSpace(request.IssuedBy) ? "Nhân viên" : request.IssuedBy.Trim();
    var issuedAt = DateTime.UtcNow;
    var dueDate = request.DueDate?.Date ?? DateTime.UtcNow.Date.AddDays(7);
    var createdInvoices = new List<MonthlyInvoice>();
    var returnedInvoices = new List<MonthlyInvoice>();

    store.Write(data =>
    {
        var nextId = data.MonthlyInvoices.Count == 0
            ? 1
            : data.MonthlyInvoices.Max(item => item.Id) + 1;

        foreach (var allocation in previewResult.Allocations)
        {
            var existing = data.MonthlyInvoices.FirstOrDefault(item =>
                item.ContractId == allocation.ContractId &&
                item.BillingPeriod.Equals(request.BillingPeriod, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                returnedInvoices.Add(existing);
                continue;
            }

            var paymentCode = BuildPaymentCode(request.BillingPeriod, allocation.StudentCode, nextId);
            var invoice = new MonthlyInvoice
            {
                Id = nextId,
                InvoiceCode = $"PT-{periodDigits}-P{safeRoom}-{nextId:0000}",
                ContractId = allocation.ContractId,
                ContractCode = allocation.ContractCode,
                StudentId = allocation.StudentId,
                StudentCode = allocation.StudentCode,
                StudentName = allocation.StudentName,
                StudentEmail = allocation.StudentEmail,
                RoomId = request.RoomId,
                RoomName = roomName,
                BillingPeriod = request.BillingPeriod,
                BatchCode = batchCode,
                RoomFee = allocation.RoomFee,
                PreviousElectricityReading = request.PreviousElectricityReading,
                CurrentElectricityReading = request.CurrentElectricityReading,
                ElectricityUsage = allocation.ElectricityUsage,
                ElectricityRate = ElectricityRate,
                ElectricityAmount = allocation.ElectricityAmount,
                PreviousWaterReading = request.PreviousWaterReading,
                CurrentWaterReading = request.CurrentWaterReading,
                WaterUsage = allocation.WaterUsage,
                WaterRate = WaterRate,
                WaterAmount = allocation.WaterAmount,
                RoomElectricityUsage = previewResult.ElectricityUsage,
                RoomElectricityAmount = previewResult.ElectricityAmount,
                RoomWaterUsage = previewResult.WaterUsage,
                RoomWaterAmount = previewResult.WaterAmount,
                RoomOccupantCount = previewResult.Allocations.Count,
                OccupancyDays = allocation.OccupancyDays,
                BillingDays = allocation.BillingDays,
                AllocationMethod = "OccupancyDays",
                AllocationNote = allocation.AllocationNote,
                TotalAmount = allocation.TotalAmount,
                DueDate = dueDate,
                Status = "Unpaid",
                PaymentCode = paymentCode,
                QrCodeUrl = qrBuilder.Build(allocation.TotalAmount, paymentCode),
                IssuedBy = issuedBy,
                IssuedAt = issuedAt
            };

            data.MonthlyInvoices.Add(invoice);
            createdInvoices.Add(invoice);
            returnedInvoices.Add(invoice);
            nextId++;
        }

        var invoiceIds = returnedInvoices.Select(item => item.Id).Distinct().OrderBy(id => id).ToList();
        var existingBatchIndex = data.RoomInvoiceBatches.FindIndex(item =>
            item.BatchCode.Equals(batchCode, StringComparison.OrdinalIgnoreCase));
        var batch = new RoomMonthlyInvoiceBatch(
            existingBatchIndex >= 0 ? data.RoomInvoiceBatches[existingBatchIndex].Id : NextBatchId(data),
            batchCode,
            request.RoomId,
            roomName,
            request.BillingPeriod,
            request.PreviousElectricityReading,
            request.CurrentElectricityReading,
            previewResult.ElectricityUsage,
            previewResult.ElectricityAmount,
            request.PreviousWaterReading,
            request.CurrentWaterReading,
            previewResult.WaterUsage,
            previewResult.WaterAmount,
            previewResult.Allocations.Count,
            returnedInvoices.Sum(item => item.TotalAmount),
            issuedBy,
            issuedAt,
            invoiceIds);

        if (existingBatchIndex >= 0)
            data.RoomInvoiceBatches[existingBatchIndex] = batch;
        else
            data.RoomInvoiceBatches.Add(batch);

        return true;
    });

    var emailResults = new List<object>();

    foreach (var invoice in createdInvoices)
    {
        var result = await TrySendInvoiceEmailAsync(invoice, store, emailSender);
        emailResults.Add(new
        {
            invoiceId = invoice.Id,
            invoiceCode = invoice.InvoiceCode,
            sent = result.Sent,
            error = result.Error
        });
    }

    return Results.Ok(new
    {
        message = createdInvoices.Count == 0
            ? "Phòng này đã có đủ phiếu thanh toán trong tháng đã chọn."
            : $"Đã phát hành {createdInvoices.Count} phiếu thanh toán cho phòng {roomName}.",
        batchCode,
        created = createdInvoices.Count,
        skipped = returnedInvoices.Count - createdInvoices.Count,
        invoices = returnedInvoices.OrderBy(item => item.StudentName).ToList(),
        emailResults
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
    async Task<IResult> (
        long id,
        AssignIncidentRequest request,
        HttpRequest httpRequest,
        BillingStore store,
        IHttpClientFactory httpClientFactory) =>
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

        var existing = store.Read(data => data.Incidents.FirstOrDefault(item => item.Id == id));

        if (existing == null)
            return Results.NotFound(new { message = "Incident request not found." });

        var roomSyncResult = await SyncRoomForIncidentAsync(
            existing,
            "assigned",
            store,
            httpClientFactory);

        if (!roomSyncResult.Success)
            return Results.Problem(roomSyncResult.Message, statusCode: StatusCodes.Status502BadGateway);

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
    async Task<IResult> (
        long id,
        UpdateIncidentStatusRequest request,
        HttpRequest httpRequest,
        BillingStore store,
        IHttpClientFactory httpClientFactory) =>
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

        var roomSyncResult = await SyncRoomForIncidentAsync(
            existing,
            nextStatus,
            store,
            httpClientFactory);

        if (!roomSyncResult.Success)
            return Results.Problem(roomSyncResult.Message, statusCode: StatusCodes.Status502BadGateway);

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

app.MapPost("/api/incidents/{id:long}/confirm", async Task<IResult> (
    long id,
    StudentIncidentActionRequest request,
    HttpRequest httpRequest,
    BillingStore store,
    IHttpClientFactory httpClientFactory) =>
{
    var identity = GetRequestIdentity(httpRequest);
    var existing = store.Read(data => data.Incidents.FirstOrDefault(item => item.Id == id));

    if (existing == null)
        return Results.NotFound();

    if (!identity.IsAdmin && !identity.IsStudentOwner(existing))
        return Results.Unauthorized();

    if (existing.Status != "completed")
        return Results.BadRequest(new { message = "Yêu cầu chưa ở trạng thái hoàn thành." });

    var roomSyncResult = await SyncRoomForIncidentAsync(
        existing,
        "confirmed",
        store,
        httpClientFactory);

    if (!roomSyncResult.Success)
        return Results.Problem(roomSyncResult.Message, statusCode: StatusCodes.Status502BadGateway);

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

app.MapPost("/api/incidents/{id:long}/reopen", async Task<IResult> (
    long id,
    StudentIncidentActionRequest request,
    HttpRequest httpRequest,
    BillingStore store,
    IHttpClientFactory httpClientFactory) =>
{
    var identity = GetRequestIdentity(httpRequest);
    var existing = store.Read(data => data.Incidents.FirstOrDefault(item => item.Id == id));

    if (existing == null)
        return Results.NotFound();

    if (!identity.IsAdmin && !identity.IsStudentOwner(existing))
        return Results.Unauthorized();

    if (existing.Status is not ("completed" or "confirmed"))
        return Results.BadRequest(new { message = "Yêu cầu chưa thể mở lại." });

    var roomSyncResult = await SyncRoomForIncidentAsync(
        existing,
        "reopened",
        store,
        httpClientFactory);

    if (!roomSyncResult.Success)
        return Results.Problem(roomSyncResult.Message, statusCode: StatusCodes.Status502BadGateway);

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
    async Task<IResult> (
        long id,
        UpdateMaintenancePlanRequest request,
        HttpRequest httpRequest,
        BillingStore store,
        IHttpClientFactory httpClientFactory) =>
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

        var nextStatus = NormalizeMaintenanceStatus(request.Status) ?? existing.Status;
        var nextCompletedItems = request.CompletedItems ?? existing.CompletedItems;

        if (nextStatus == "completed" &&
            existing.Checklist.Count > 0 &&
            !existing.Checklist.All(item => nextCompletedItems.Contains(item)))
        {
            return Results.BadRequest(new { message = "Vui lòng hoàn thành toàn bộ checklist trước khi kết thúc bảo trì." });
        }

        var roomSyncResult = await SyncRoomForMaintenanceAsync(
            existing,
            nextStatus,
            store,
            httpClientFactory);

        if (!roomSyncResult.Success)
            return Results.Problem(roomSyncResult.Message, statusCode: StatusCodes.Status502BadGateway);

        var plan = store.Write(data =>
        {
            var current = data.MaintenancePlans.FirstOrDefault(item => item.Id == id);
            if (current == null) return null;

            current.Status = nextStatus;
            if (identity.IsAdmin)
            {
                current.AssignedTo = string.IsNullOrWhiteSpace(request.AssignedTo) ? current.AssignedTo : request.AssignedTo.Trim();
                current.AssignedName = string.IsNullOrWhiteSpace(request.AssignedName) ? current.AssignedName : request.AssignedName.Trim();
            }
            current.NextDueDate = request.NextDueDate ?? current.NextDueDate;
            current.CompletedItems = nextCompletedItems;
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

static RoomPreviewBuildResult BuildRoomInvoicePreview(CreateRoomMonthlyInvoicesRequest request)
{
    var validationError = ValidateRoomInvoiceRequest(request);

    if (validationError != null)
        return new RoomPreviewBuildResult(validationError, 0, 0, 0, 0, []);

    var (periodStart, periodEndExclusive, billingDays) = GetBillingPeriodRange(request.BillingPeriod);
    var electricityUsage = request.CurrentElectricityReading - request.PreviousElectricityReading;
    var waterUsage = request.CurrentWaterReading - request.PreviousWaterReading;
    var electricityAmount = electricityUsage * ElectricityRate;
    var waterAmount = waterUsage * WaterRate;
    var normalizedOccupants = request.Occupants!
        .Where(item => item.ContractId > 0 && item.StudentId > 0)
        .Select(item =>
        {
            var occupancyDays = CalculateOccupancyDays(item.StartDate, item.EndDate, periodStart, periodEndExclusive);
            return new RoomOccupantAllocationInput(item, occupancyDays);
        })
        .Where(item => item.OccupancyDays > 0)
        .ToList();

    if (normalizedOccupants.Count == 0)
    {
        return new RoomPreviewBuildResult(
            "Không có sinh viên nào ở phòng này trong kỳ thanh toán đã chọn.",
            electricityUsage,
            electricityAmount,
            waterUsage,
            waterAmount,
            []);
    }

    var missingEmail = normalizedOccupants
        .FirstOrDefault(item => string.IsNullOrWhiteSpace(item.Occupant.StudentEmail));

    if (missingEmail != null)
    {
        var name = missingEmail.Occupant.StudentName?.Trim()
            ?? missingEmail.Occupant.StudentCode?.Trim()
            ?? missingEmail.Occupant.StudentId.ToString(CultureInfo.InvariantCulture);
        return new RoomPreviewBuildResult(
            $"Sinh viên {name} chưa có email để nhận phiếu thanh toán.",
            electricityUsage,
            electricityAmount,
            waterUsage,
            waterAmount,
            []);
    }

    var weights = normalizedOccupants.Select(item => (decimal)item.OccupancyDays).ToList();
    var electricityShares = SplitAmount(electricityAmount, weights);
    var waterShares = SplitAmount(waterAmount, weights);
    var allocations = new List<RoomInvoiceAllocationPreview>();

    for (var index = 0; index < normalizedOccupants.Count; index++)
    {
        var input = normalizedOccupants[index];
        var occupant = input.Occupant;
        var roomFee = RoundMoney(occupant.RoomFee * input.OccupancyDays / billingDays);
        var electricityShare = electricityShares[index];
        var waterShare = waterShares[index];
        var electricityShareUsage = electricityAmount <= 0
            ? 0
            : RoundUsage(electricityShare / ElectricityRate);
        var waterShareUsage = waterAmount <= 0
            ? 0
            : RoundUsage(waterShare / WaterRate);
        var allocationNote = input.OccupancyDays == billingDays
            ? $"Ở đủ {billingDays} ngày trong kỳ, chia điện nước theo {normalizedOccupants.Count} người."
            : $"Ở {input.OccupancyDays}/{billingDays} ngày trong kỳ, điện nước và tiền phòng được phân bổ theo ngày ở.";

        allocations.Add(new RoomInvoiceAllocationPreview(
            occupant.ContractId,
            occupant.ContractCode?.Trim() ?? $"HD-{occupant.ContractId}",
            occupant.StudentId,
            occupant.StudentCode?.Trim() ?? $"SV{occupant.StudentId}",
            occupant.StudentName?.Trim() ?? "Sinh viên",
            occupant.StudentEmail?.Trim() ?? string.Empty,
            roomFee,
            input.OccupancyDays,
            billingDays,
            electricityShareUsage,
            electricityShare,
            waterShareUsage,
            waterShare,
            roomFee + electricityShare + waterShare,
            allocationNote));
    }

    return new RoomPreviewBuildResult(
        null,
        electricityUsage,
        electricityAmount,
        waterUsage,
        waterAmount,
        allocations);
}

static string? ValidateRoomInvoiceRequest(CreateRoomMonthlyInvoicesRequest request)
{
    if (request.RoomId <= 0)
        return "Vui lòng chọn phòng cần phát hành hóa đơn.";

    if (!Regex.IsMatch(request.BillingPeriod ?? string.Empty, @"^\d{4}-(0[1-9]|1[0-2])$"))
        return "Kỳ thanh toán phải có định dạng yyyy-MM.";

    if (request.CurrentElectricityReading < request.PreviousElectricityReading)
        return "Chỉ số điện mới phải lớn hơn hoặc bằng chỉ số cũ.";

    if (request.CurrentWaterReading < request.PreviousWaterReading)
        return "Chỉ số nước mới phải lớn hơn hoặc bằng chỉ số cũ.";

    if (request.Occupants == null || request.Occupants.Count == 0)
        return "Phòng chưa có sinh viên/hợp đồng đang ở để chia hóa đơn.";

    return null;
}

static (DateTime Start, DateTime EndExclusive, int Days) GetBillingPeriodRange(string period)
{
    var start = DateTime.ParseExact($"{period}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
    var endExclusive = start.AddMonths(1);
    return (start, endExclusive, (int)(endExclusive - start).TotalDays);
}

static int CalculateOccupancyDays(
    DateTime? startDate,
    DateTime? endDate,
    DateTime periodStart,
    DateTime periodEndExclusive)
{
    var start = (startDate?.Date ?? periodStart) > periodStart
        ? startDate!.Value.Date
        : periodStart;
    var endExclusive = endDate.HasValue
        ? endDate.Value.Date.AddDays(1)
        : periodEndExclusive;

    if (endExclusive > periodEndExclusive)
        endExclusive = periodEndExclusive;

    if (endExclusive <= start)
        return 0;

    return (int)(endExclusive - start).TotalDays;
}

static List<decimal> SplitAmount(decimal amount, IReadOnlyList<decimal> weights)
{
    if (weights.Count == 0)
        return [];

    var totalWeight = weights.Sum();
    if (totalWeight <= 0 || amount <= 0)
        return weights.Select(_ => 0m).ToList();

    var shares = new List<decimal>(weights.Count);
    var assigned = 0m;

    for (var index = 0; index < weights.Count; index++)
    {
        var share = index == weights.Count - 1
            ? amount - assigned
            : RoundMoney(amount * weights[index] / totalWeight);
        shares.Add(share);
        assigned += share;
    }

    return shares;
}

static decimal RoundMoney(decimal value) =>
    Math.Round(value, 0, MidpointRounding.AwayFromZero);

static decimal RoundUsage(decimal value) =>
    Math.Round(value, 2, MidpointRounding.AwayFromZero);

static string NormalizeRoomName(string? roomName, long roomId) =>
    string.IsNullOrWhiteSpace(roomName)
        ? roomId.ToString(CultureInfo.InvariantCulture)
        : roomName.Trim();

static long NextBatchId(BillingData data) =>
    data.RoomInvoiceBatches.Count == 0
        ? 1
        : data.RoomInvoiceBatches.Max(item => item.Id) + 1;

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

static async Task<RoomSyncResult> SyncRoomForIncidentAsync(
    MaintenanceIncident incident,
    string nextStatus,
    BillingStore store,
    IHttpClientFactory httpClientFactory)
{
    return await SyncRoomForWorkItemAsync(
        RoomReferenceFromIncident(incident),
        nextStatus,
        store,
        httpClientFactory,
        incident.Id,
        null);
}

static async Task<RoomSyncResult> SyncRoomForMaintenanceAsync(
    MaintenancePlan plan,
    string nextStatus,
    BillingStore store,
    IHttpClientFactory httpClientFactory)
{
    return await SyncRoomForWorkItemAsync(
        RoomReferenceFromMaintenance(plan),
        nextStatus,
        store,
        httpClientFactory,
        null,
        plan.Id);
}

static async Task<RoomSyncResult> SyncRoomForWorkItemAsync(
    RoomWorkReference? reference,
    string nextStatus,
    BillingStore store,
    IHttpClientFactory httpClientFactory,
    long? currentIncidentId,
    long? currentMaintenanceId)
{
    if (reference == null)
        return RoomSyncResult.Ok();

    if (!ShouldMarkRoomMaintenance(nextStatus) && !ShouldReleaseRoom(nextStatus))
        return RoomSyncResult.Ok();

    if (ShouldReleaseRoom(nextStatus) &&
        HasOtherOpenRoomWork(reference, store, currentIncidentId, currentMaintenanceId))
    {
        return RoomSyncResult.Ok();
    }

    var roomIdResult = await ResolveRoomIdAsync(reference, httpClientFactory);

    if (!roomIdResult.Success)
        return new RoomSyncResult(false, roomIdResult.Message);

    if (!roomIdResult.RoomId.HasValue)
        return RoomSyncResult.Ok();

    return await SetRoomStatusAsync(
        roomIdResult.RoomId.Value,
        ShouldMarkRoomMaintenance(nextStatus) ? "Maintenance" : "Auto",
        httpClientFactory);
}

static bool HasOtherOpenRoomWork(
    RoomWorkReference reference,
    BillingStore store,
    long? currentIncidentId,
    long? currentMaintenanceId)
{
    return store.Read(data =>
        data.Incidents.Any(item =>
            item.Id != currentIncidentId &&
            ShouldMarkRoomMaintenance(item.Status) &&
            SameRoomReference(RoomReferenceFromIncident(item), reference)) ||
        data.MaintenancePlans.Any(item =>
            item.Id != currentMaintenanceId &&
            ShouldMarkRoomMaintenance(item.Status) &&
            SameRoomReference(RoomReferenceFromMaintenance(item), reference)));
}

static bool ShouldMarkRoomMaintenance(string? status)
{
    return NormalizeWorkStatus(status) is "accepted" or "assigned" or "processing" or
        "waiting-materials" or "reopened" or "in-progress";
}

static bool ShouldReleaseRoom(string? status)
{
    return NormalizeWorkStatus(status) is "completed" or "confirmed" or "rejected" or "cancelled" or "scheduled";
}

static string NormalizeWorkStatus(string? status)
{
    return (status ?? string.Empty).Trim().ToLowerInvariant();
}

static RoomWorkReference? RoomReferenceFromIncident(MaintenanceIncident incident)
{
    if (string.IsNullOrWhiteSpace(incident.RoomName))
        return null;

    return new RoomWorkReference(CleanBuildingToken(incident.Building), incident.RoomName.Trim());
}

static RoomWorkReference? RoomReferenceFromMaintenance(MaintenancePlan plan)
{
    if (string.IsNullOrWhiteSpace(plan.Location))
        return null;

    var foldedLocation = FoldText(plan.Location);
    var roomMatch = Regex.Match(
        foldedLocation,
        @"\b(?:phong|room)\s*([A-Za-z]?\s*[-_/]?\s*\d{2,5})\b",
        RegexOptions.IgnoreCase);

    var compactRoomMatch = Regex.Match(
        foldedLocation,
        @"\b([A-Za-z])\s*[-_/]\s*(\d{2,5})\b",
        RegexOptions.IgnoreCase);

    if (!roomMatch.Success && !compactRoomMatch.Success)
        return null;

    var building = ExtractBuildingToken(foldedLocation);
    var roomName = string.Empty;

    if (compactRoomMatch.Success)
    {
        building = string.IsNullOrWhiteSpace(building) ? compactRoomMatch.Groups[1].Value : building;
        roomName = compactRoomMatch.Groups[2].Value;
    }
    else
    {
        roomName = roomMatch.Groups[1].Value;
    }

    return string.IsNullOrWhiteSpace(roomName)
        ? null
        : new RoomWorkReference(CleanBuildingToken(building), CleanRoomToken(roomName));
}

static async Task<RoomResolveResult> ResolveRoomIdAsync(
    RoomWorkReference reference,
    IHttpClientFactory httpClientFactory)
{
    var client = httpClientFactory.CreateClient("RoomService");
    var requestPath = string.IsNullOrWhiteSpace(reference.Building)
        ? "/api/rooms"
        : $"/api/rooms?buildingName={Uri.EscapeDataString(reference.Building)}";

    try
    {
        using var response = await client.GetAsync(requestPath);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync();
            return new RoomResolveResult(false, $"Không đọc được danh sách phòng từ RoomService: {detail}", null);
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var matchedRoom = EnumerateRoomElements(document.RootElement)
            .FirstOrDefault(element => SameRoomElement(element, reference));

        if (matchedRoom.ValueKind == JsonValueKind.Undefined)
            return new RoomResolveResult(true, string.Empty, null);

        var roomId = GetLong(matchedRoom, "roomId", "id");
        return roomId > 0
            ? new RoomResolveResult(true, string.Empty, roomId)
            : new RoomResolveResult(false, "RoomService trả về phòng nhưng thiếu roomId.", null);
    }
    catch (HttpRequestException ex)
    {
        return new RoomResolveResult(false, $"Không kết nối được RoomService: {ex.Message}", null);
    }
    catch (TaskCanceledException ex)
    {
        return new RoomResolveResult(false, $"RoomService phản hồi quá lâu: {ex.Message}", null);
    }
}

static async Task<RoomSyncResult> SetRoomStatusAsync(
    long roomId,
    string status,
    IHttpClientFactory httpClientFactory)
{
    var client = httpClientFactory.CreateClient("RoomService");
    using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/rooms/{roomId}/status")
    {
        Content = JsonContent.Create(new { status })
    };

    try
    {
        using var response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
            return RoomSyncResult.Ok();

        var detail = await response.Content.ReadAsStringAsync();
        return new RoomSyncResult(false, $"RoomService không cập nhật được trạng thái phòng {roomId}: {detail}");
    }
    catch (HttpRequestException ex)
    {
        return new RoomSyncResult(false, $"Không kết nối được RoomService khi cập nhật phòng {roomId}: {ex.Message}");
    }
    catch (TaskCanceledException ex)
    {
        return new RoomSyncResult(false, $"RoomService phản hồi quá lâu khi cập nhật phòng {roomId}: {ex.Message}");
    }
}

static IEnumerable<JsonElement> EnumerateRoomElements(JsonElement element)
{
    if (element.ValueKind == JsonValueKind.Array)
    {
        foreach (var child in element.EnumerateArray())
            yield return child;

        yield break;
    }

    if (element.ValueKind != JsonValueKind.Object)
        yield break;

    foreach (var propertyName in new[] { "data", "value", "items", "rooms" })
    {
        if (!element.TryGetProperty(propertyName, out var child))
            continue;

        foreach (var item in EnumerateRoomElements(child))
            yield return item;
    }
}

static bool SameRoomElement(JsonElement element, RoomWorkReference reference)
{
    var roomId = GetLong(element, "roomId", "id");
    var roomNumber = GetString(element, "roomNumber", "roomName", "name");
    var building = GetString(element, "buildingName", "building", "buildingCode");

    var sameBuilding = string.IsNullOrWhiteSpace(reference.Building) ||
        SameComparable(CleanBuildingToken(building), reference.Building);

    var sameRoom = SameComparable(CleanRoomToken(roomNumber), CleanRoomToken(reference.RoomName)) ||
        (long.TryParse(reference.RoomName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var requestedRoomId) &&
            roomId == requestedRoomId);

    return sameBuilding && sameRoom;
}

static bool SameRoomReference(RoomWorkReference? first, RoomWorkReference second)
{
    if (first == null)
        return false;

    var sameBuilding = string.IsNullOrWhiteSpace(first.Building) ||
        string.IsNullOrWhiteSpace(second.Building) ||
        SameComparable(first.Building, second.Building);

    return sameBuilding && SameComparable(CleanRoomToken(first.RoomName), CleanRoomToken(second.RoomName));
}

static long GetLong(JsonElement element, params string[] names)
{
    var value = GetString(element, names);
    return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
        ? number
        : 0;
}

static string ExtractBuildingToken(string value)
{
    var match = Regex.Match(value, @"\b(?:toa|building|nha)\s*([A-Za-z0-9]+)\b", RegexOptions.IgnoreCase);
    return match.Success ? match.Groups[1].Value : string.Empty;
}

static string CleanBuildingToken(string? value)
{
    var normalized = FoldText(value)
        .Replace("toa", string.Empty, StringComparison.OrdinalIgnoreCase)
        .Replace("building", string.Empty, StringComparison.OrdinalIgnoreCase)
        .Trim();

    return normalized is "-" or "." ? string.Empty : normalized;
}

static string CleanRoomToken(string? value)
{
    var normalized = FoldText(value);
    var match = Regex.Match(normalized, @"\d{1,5}");
    return match.Success ? match.Value : normalized;
}

static bool SameComparable(string? first, string? second)
{
    return string.Equals(
        NormalizeComparable(first),
        NormalizeComparable(second),
        StringComparison.OrdinalIgnoreCase);
}

static string NormalizeComparable(string? value)
{
    return Regex.Replace(FoldText(value), @"[\s\-_/]", string.Empty).ToLowerInvariant();
}

static string FoldText(string? value)
{
    var normalized = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
    var builder = new StringBuilder(normalized.Length);

    foreach (var character in normalized)
    {
        if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            builder.Append(character);
    }

    return builder.ToString().Normalize(NormalizationForm.FormC).Trim();
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

public sealed record RoomOccupantAllocationInput(
    RoomInvoiceOccupant Occupant,
    int OccupancyDays);

public sealed record RoomPreviewBuildResult(
    string? Error,
    decimal ElectricityUsage,
    decimal ElectricityAmount,
    decimal WaterUsage,
    decimal WaterAmount,
    List<RoomInvoiceAllocationPreview> Allocations);

public sealed record RoomSyncResult(bool Success, string Message)
{
    public static RoomSyncResult Ok() => new(true, string.Empty);
}

public sealed record RoomResolveResult(bool Success, string Message, long? RoomId);

public sealed record RoomWorkReference(string Building, string RoomName);

public sealed record RequestIdentity(string Username, string Role, string StudentCode)
{
    public bool IsAdmin => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    public bool IsOperational => IsAdmin || Role.Equals("Staff", StringComparison.OrdinalIgnoreCase);
    public bool IsStudentOwner(MaintenanceIncident incident) =>
        Role.Equals("Student", StringComparison.OrdinalIgnoreCase) &&
        StudentCode.Equals(incident.StudentCode, StringComparison.OrdinalIgnoreCase);
}
