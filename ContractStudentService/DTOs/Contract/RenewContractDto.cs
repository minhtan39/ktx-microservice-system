namespace ContractStudentService.DTOs.Contract;

public class RenewContractDto
{
    public DateTime NewEndDate { get; set; }

    public string Note { get; set; } = string.Empty;
}
