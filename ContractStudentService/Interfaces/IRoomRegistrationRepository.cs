using ContractStudentService.Entities;

namespace ContractStudentService.Interfaces;

public interface IRoomRegistrationRepository
{
    Task<IEnumerable<RoomRegistration>> GetAllAsync();

    Task<RoomRegistration?> GetByIdAsync(long id);

    Task<RoomRegistration> CreateAsync(RoomRegistration registration);

    Task<RoomRegistration?> UpdateAsync(RoomRegistration registration);

    Task<bool> DeleteAsync(long id);
}