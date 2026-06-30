namespace ContractStudentService.DTOs.Integration;

public class OccupyRoomRequestDto
{
    public long StudentId { get; set; }

    public long RegistrationId { get; set; }

    public string ContractCode { get; set; } = string.Empty;

    public bool AllowMaintenance { get; set; }
}
