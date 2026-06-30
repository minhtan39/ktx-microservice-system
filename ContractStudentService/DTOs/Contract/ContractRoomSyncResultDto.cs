namespace ContractStudentService.DTOs.Contract;

public class ContractRoomSyncResultDto
{
    public int Total { get; set; }

    public int Synced { get; set; }

    public int Failed { get; set; }

    public List<string> Errors { get; set; } = new();
}
