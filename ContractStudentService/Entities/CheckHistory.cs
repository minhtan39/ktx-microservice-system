namespace ContractStudentService.Entities;

public class CheckHistory
{
    public long Id { get; set; }

    public long ContractId { get; set; }

    public string CheckType { get; set; } = string.Empty;

    public DateTime CheckTime { get; set; }

    public string Note { get; set; } = string.Empty;
}