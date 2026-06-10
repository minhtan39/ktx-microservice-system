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

var billingItems = new List<BillingItem>();
var incidents = SeedIncidents();

app.MapGet("/health", () => Results.Ok(new
{
    service = "BillingService",
    status = "Healthy"
}));

app.MapGet("/api/billing/contracts", () =>
{
    return Results.Ok(billingItems);
});

app.MapGet("/api/billing/contracts/{contractId:long}", (long contractId) =>
{
    var items = billingItems
        .Where(item => item.ContractId == contractId)
        .ToList();

    return Results.Ok(new
    {
        contractId,
        totalAmount = items.Sum(item => item.Amount),
        items
    });
});

app.MapPost("/api/billing/contracts", (ContractBillingRequest request) =>
{
    if (billingItems.Any(item => item.ContractId == request.ContractId))
    {
        var existing = billingItems
            .Where(item => item.ContractId == request.ContractId)
            .ToList();

        return Results.Ok(new
        {
            message = "Billing already created for this contract.",
            contractId = request.ContractId,
            totalAmount = existing.Sum(item => item.Amount),
            items = existing
        });
    }

    var dueDate = DateTime.UtcNow.Date.AddDays(7);

    billingItems.Add(new BillingItem(
        billingItems.Count + 1,
        request.ContractId,
        request.ContractCode,
        request.StudentId,
        "Deposit",
        request.DepositAmount,
        dueDate,
        "Unpaid"));

    billingItems.Add(new BillingItem(
        billingItems.Count + 1,
        request.ContractId,
        request.ContractCode,
        request.StudentId,
        "FirstMonthRoomFee",
        request.MonthlyFee,
        dueDate,
        "Unpaid"));

    var createdItems = billingItems
        .Where(item => item.ContractId == request.ContractId)
        .ToList();

    return Results.Ok(new
    {
        message = "Billing items created from contract.",
        contractId = request.ContractId,
        totalAmount = createdItems.Sum(item => item.Amount),
        items = createdItems
    });
});

app.MapGet("/api/incidents", (long? studentId, string? status) =>
{
    var query = incidents.AsEnumerable();

    if (studentId.HasValue)
    {
        query = query.Where(item => item.StudentId == studentId.Value);
    }

    if (!string.IsNullOrWhiteSpace(status))
    {
        query = query.Where(item => item.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
    }

    return Results.Ok(query
        .OrderByDescending(item => item.CreatedAt)
        .ToList());
});

app.MapGet("/api/incidents/{id:long}", (long id) =>
{
    var incident = incidents.FirstOrDefault(item => item.Id == id);
    return incident == null
        ? Results.NotFound(new { message = "Incident request not found." })
        : Results.Ok(incident);
});

app.MapPost("/api/incidents", (CreateIncidentRequest request) =>
{
    if (request.StudentId <= 0)
    {
        return Results.BadRequest(new { message = "studentId is required." });
    }

    if (string.IsNullOrWhiteSpace(request.RoomName) ||
        string.IsNullOrWhiteSpace(request.Description))
    {
        return Results.BadRequest(new { message = "roomName and description are required." });
    }

    var incident = new MaintenanceIncident(
        incidents.Count == 0 ? 1 : incidents.Max(item => item.Id) + 1,
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

    incidents.Add(incident);

    return Results.Ok(incident);
});

app.MapMethods(
    "/api/incidents/{id:long}/status",
    new[] { "PATCH", "PUT" },
    (long id, UpdateIncidentStatusRequest request) =>
    {
        var index = incidents.FindIndex(item => item.Id == id);

        if (index < 0)
        {
            return Results.NotFound(new { message = "Incident request not found." });
        }

        var nextStatus = NormalizeIncidentStatus(request.Status);

        if (nextStatus == null)
        {
            return Results.BadRequest(new { message = "Status must be new, processing, done, or rejected." });
        }

        var current = incidents[index];
        var updated = current with
        {
            Status = nextStatus,
            HandledBy = string.IsNullOrWhiteSpace(request.HandledBy) ? current.HandledBy : request.HandledBy.Trim(),
            StaffNote = string.IsNullOrWhiteSpace(request.StaffNote) ? current.StaffNote : request.StaffNote.Trim(),
            UpdatedAt = DateTime.UtcNow
        };

        incidents[index] = updated;

        return Results.Ok(updated);
    });

app.MapGet("/api/maintenance", () => Results.Ok(incidents
    .OrderByDescending(item => item.CreatedAt)
    .ToList()));

app.MapPost("/api/maintenance", (CreateIncidentRequest request) =>
{
    if (request.StudentId <= 0)
    {
        return Results.BadRequest(new { message = "studentId is required." });
    }

    var incident = new MaintenanceIncident(
        incidents.Count == 0 ? 1 : incidents.Max(item => item.Id) + 1,
        request.StudentId,
        string.IsNullOrWhiteSpace(request.StudentCode) ? $"SV-{request.StudentId}" : request.StudentCode.Trim(),
        string.IsNullOrWhiteSpace(request.StudentName) ? "Student" : request.StudentName.Trim(),
        string.IsNullOrWhiteSpace(request.RoomName) ? "-" : request.RoomName.Trim(),
        string.IsNullOrWhiteSpace(request.Building) ? "-" : request.Building.Trim(),
        string.IsNullOrWhiteSpace(request.Category) ? "Other" : request.Category.Trim(),
        string.IsNullOrWhiteSpace(request.Description) ? "Maintenance request" : request.Description.Trim(),
        "new",
        DateTime.UtcNow,
        null,
        null,
        null);

    incidents.Add(incident);

    return Results.Ok(incident);
});

app.Run();

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

static List<MaintenanceIncident> SeedIncidents()
{
    return new List<MaintenanceIncident>
    {
        new(
            1,
            2,
            "SV20260001",
            "Nguyen Van A",
            "101",
            "A",
            "Electric",
            "Den hoc bi hong, can kiem tra lai cong tac va bong den.",
            "new",
            DateTime.UtcNow.AddHours(-6),
            null,
            null,
            null),
        new(
            2,
            2,
            "SV20260001",
            "Nguyen Van A",
            "101",
            "A",
            "Water",
            "Voi nuoc nha ve sinh ri nuoc lien tuc.",
            "processing",
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddHours(-3),
            "nhanvien",
            "Da tiep nhan, cho ky thuat xu ly.")
    };
}

public sealed record ContractBillingRequest(
    long ContractId,
    string ContractCode,
    long StudentId,
    long RoomId,
    decimal DepositAmount,
    decimal MonthlyFee,
    DateTime StartDate,
    DateTime EndDate);

public sealed record BillingItem(
    int Id,
    long ContractId,
    string ContractCode,
    long StudentId,
    string FeeType,
    decimal Amount,
    DateTime DueDate,
    string Status);

public sealed record CreateIncidentRequest(
    long StudentId,
    string? StudentCode,
    string? StudentName,
    string RoomName,
    string? Building,
    string? Category,
    string Description);

public sealed record UpdateIncidentStatusRequest(
    string Status,
    string? HandledBy,
    string? StaffNote);

public sealed record MaintenanceIncident(
    long Id,
    long StudentId,
    string StudentCode,
    string StudentName,
    string RoomName,
    string Building,
    string Category,
    string Description,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? HandledBy,
    string? StaffNote);
