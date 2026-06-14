public sealed class BillingData
{
    public List<BillingItem> ContractItems { get; set; } = [];
    public List<MonthlyInvoice> MonthlyInvoices { get; set; } = [];
    public List<PaymentHistoryEntry> PaymentHistory { get; set; } = [];
    public List<MaintenanceIncident> Incidents { get; set; } = [];
    public List<MaintenancePlan> MaintenancePlans { get; set; } = [];
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
    long Id,
    long ContractId,
    string ContractCode,
    long StudentId,
    string FeeType,
    decimal Amount,
    DateTime DueDate,
    string Status);

public sealed class MonthlyInvoice
{
    public long Id { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public long ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public long StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public long RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal RoomFee { get; set; }
    public int PreviousElectricityReading { get; set; }
    public int CurrentElectricityReading { get; set; }
    public int ElectricityUsage { get; set; }
    public decimal ElectricityRate { get; set; }
    public decimal ElectricityAmount { get; set; }
    public int PreviousWaterReading { get; set; }
    public int CurrentWaterReading { get; set; }
    public int WaterUsage { get; set; }
    public decimal WaterRate { get; set; }
    public decimal WaterAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Unpaid";
    public string PaymentCode { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime? EmailSentAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal? PaidAmount { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaymentMethod { get; set; }
}

public sealed record CreateMonthlyInvoiceRequest(
    long ContractId,
    string? ContractCode,
    long StudentId,
    string? StudentCode,
    string? StudentName,
    string? StudentEmail,
    long RoomId,
    string? RoomName,
    string BillingPeriod,
    decimal RoomFee,
    int PreviousElectricityReading,
    int CurrentElectricityReading,
    int PreviousWaterReading,
    int CurrentWaterReading,
    DateTime? DueDate,
    string? IssuedBy);

public sealed record MarkInvoicePaidRequest(
    decimal? Amount,
    string? ReferenceCode,
    string? ConfirmedBy);

public sealed record PaymentHistoryEntry(
    long Id,
    long InvoiceId,
    string InvoiceCode,
    long StudentId,
    string StudentCode,
    string StudentName,
    string BillingPeriod,
    decimal Amount,
    string Method,
    string ReferenceCode,
    string ConfirmedBy,
    DateTime PaidAt);

public sealed record CreateIncidentRequest(
    long StudentId,
    string? StudentCode,
    string? StudentName,
    string RoomName,
    string? Building,
    string? Category,
    string Description,
    string? Priority = null,
    DateTime? PreferredVisitAt = null,
    string? ContactPhone = null,
    List<string>? ImageUrls = null);

public sealed record UpdateIncidentStatusRequest(
    string Status,
    string? HandledBy,
    string? StaffNote,
    string? RootCause = null,
    string? Resolution = null,
    decimal? MaterialCost = null,
    decimal? LaborCost = null,
    List<string>? AfterImageUrls = null);

public sealed record AssignIncidentRequest(
    string AssignedTo,
    string? AssignedName,
    string? Priority,
    DateTime? ScheduledAt,
    DateTime? DueAt,
    string? Note);

public sealed record StudentIncidentActionRequest(
    string? Note);

public sealed class MaintenanceIncident
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal";
    public string Status { get; set; } = "new";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PreferredVisitAt { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? DueAt { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedName { get; set; }
    public string? HandledBy { get; set; }
    public string? StaffNote { get; set; }
    public string? ContactPhone { get; set; }
    public string? RootCause { get; set; }
    public string? Resolution { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal ActualCost => MaterialCost + LaborCost;
    public DateTime? CompletedAt { get; set; }
    public DateTime? StudentConfirmedAt { get; set; }
    public DateTime? ReopenedAt { get; set; }
    public List<string> ImageUrls { get; set; } = [];
    public List<string> AfterImageUrls { get; set; } = [];
    public List<IncidentTimelineEntry> Timeline { get; set; } = [];
}

public sealed record IncidentTimelineEntry(
    DateTime At,
    string Action,
    string Status,
    string Actor,
    string? Note);

public sealed record CreateMaintenancePlanRequest(
    string Title,
    string AssetCode,
    string AssetName,
    string Location,
    string Category,
    string Frequency,
    DateTime NextDueDate,
    string? AssignedTo,
    string? AssignedName,
    List<string>? Checklist,
    string? Notes);

public sealed record UpdateMaintenancePlanRequest(
    string? Status,
    string? AssignedTo,
    string? AssignedName,
    DateTime? NextDueDate,
    List<string>? CompletedItems,
    decimal? Cost,
    string? Notes,
    string? UpdatedBy);

public sealed class MaintenancePlan
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public string Frequency { get; set; } = "Monthly";
    public DateTime NextDueDate { get; set; }
    public string Status { get; set; } = "scheduled";
    public string? AssignedTo { get; set; }
    public string? AssignedName { get; set; }
    public List<string> Checklist { get; set; } = [];
    public List<string> CompletedItems { get; set; } = [];
    public decimal Cost { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
