namespace ContractStudentService.DTOs.Integration;

public class ContractBillingRequestDto
{
    public long ContractId { get; set; }

    public string ContractCode { get; set; } = string.Empty;

    public long StudentId { get; set; }

    public long RoomId { get; set; }

    public decimal DepositAmount { get; set; }

    public decimal MonthlyFee { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
