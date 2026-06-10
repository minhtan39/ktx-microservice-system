namespace ContractStudentService.DTOs.Contract;

public class CreateContractDto
{
    public string ContractCode { get; set; } = string.Empty;

    public long StudentId { get; set; }

    public long RoomId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal DepositAmount { get; set; }

    public decimal MonthlyFee { get; set; }

    public string Terms { get; set; } = string.Empty;
}
