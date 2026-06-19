namespace ContractStudentService.Entities;

public class Contract
{
    public long Id { get; set; }

    public string ContractCode { get; set; } = string.Empty;

    public long StudentId { get; set; }

    public long RoomId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal DepositAmount { get; set; }

    public decimal MonthlyFee { get; set; }

    public string Terms { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";

    public DateTime? SignedAt { get; set; }

    public string SignatureFullName { get; set; } = string.Empty;

    public string SignatureStudentCode { get; set; } = string.Empty;

    public string SignatureHash { get; set; } = string.Empty;

    public string SignatureIpAddress { get; set; } = string.Empty;

    public int RenewalCount { get; set; }

    public DateTime? LastRenewedAt { get; set; }

    public string RenewalNote { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
