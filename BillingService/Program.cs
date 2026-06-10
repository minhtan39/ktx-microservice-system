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

app.Run();

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
