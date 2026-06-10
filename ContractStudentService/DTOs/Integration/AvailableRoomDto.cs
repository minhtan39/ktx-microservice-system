namespace ContractStudentService.DTOs.Integration;

public class AvailableRoomDto
{
    public long RoomId { get; set; }

    public string BuildingName { get; set; } = string.Empty;

    public string RoomType { get; set; } = string.Empty;

    public bool Gender { get; set; }

    public int Capacity { get; set; }

    public int OccupiedBeds { get; set; }

    public int AvailableBeds { get; set; }

    public decimal MonthlyFee { get; set; }

    public string Status { get; set; } = string.Empty;
}
