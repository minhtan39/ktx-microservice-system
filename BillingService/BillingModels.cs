public sealed class BillingData
{
    public List<BillingItem> ContractItems { get; set; } = [];
    public List<MonthlyInvoice> MonthlyInvoices { get; set; } = [];
    public List<PaymentHistoryEntry> PaymentHistory { get; set; } = [];
    public List<MaintenanceIncident> Incidents { get; set; } = [];
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
