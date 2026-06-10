using ContractStudentService.DTOs.Integration;
using ContractStudentService.Entities;

namespace ContractStudentService.Interfaces;

public interface IRoomGatewayClient
{
    Task<AvailableRoomDto?> FindAvailableRoomAsync(
        RoomRegistration registration,
        Student student,
        long? requestedRoomId);

    Task<AvailableRoomDto> OccupyRoomAsync(
        long roomId,
        long studentId,
        long registrationId,
        string contractCode);
}
