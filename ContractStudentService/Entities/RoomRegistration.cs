namespace ContractStudentService.Entities;

public class RoomRegistration
{
    public long Id { get; set; }

    public long StudentId { get; set; }

    public string BuildingName { get; set; } = string.Empty;

    public string RoomType { get; set; } = string.Empty;

    public string PriorityType { get; set; } = string.Empty;

    public int PriorityScore { get; set; }

    public string PriorityNote { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = "Pending";

    public long? AssignedRoomId { get; set; }

    public string AssignmentMode { get; set; } = string.Empty;

    public string AssignmentNote { get; set; } = string.Empty;

    public DateTime? AssignedAt { get; set; }

    public string RejectionReason { get; set; } = string.Empty;

    public DateTime? RejectedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
