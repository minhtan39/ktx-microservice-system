namespace ContractStudentService.DTOs.Contract;

public class RenewContractDto
{
    public DateTime EndDate { get; set; }

    public string Note { get; set; } = string.Empty;
}
