namespace ContractStudentService.DTOs.CheckHistory;

public class CreateCheckHistoryDto
{
    public long ContractId { get; set; }

    public string CheckType { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}