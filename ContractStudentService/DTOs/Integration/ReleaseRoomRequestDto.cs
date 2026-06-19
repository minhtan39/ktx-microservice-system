namespace ContractStudentService.DTOs.Integration;

public class ReleaseRoomRequestDto
{
    public long StudentId { get; set; }

    public long? RegistrationId { get; set; }

    public string ContractCode { get; set; } = string.Empty;
}
