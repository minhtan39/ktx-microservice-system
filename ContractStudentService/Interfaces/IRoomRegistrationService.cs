using ContractStudentService.Entities;

namespace ContractStudentService.Interfaces;

public interface IRoomRegistrationService
{
    Task<IEnumerable<RoomRegistration>> GetAllAsync();

    Task<RoomRegistration?> GetByIdAsync(long id);

    Task<RoomRegistration> CreateAsync(RoomRegistration registration);

    Task<RoomRegistration?> UpdateAsync(long id, RoomRegistration registration);

    Task<bool> DeleteAsync(long id);

    Task<RoomRegistration?> ApproveAsync(long id, long? roomId);

    Task<RoomRegistration?> RejectAsync(long id, string reason);
}
