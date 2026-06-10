using ContractStudentService.Entities;

namespace ContractStudentService.Interfaces;

public interface ICheckHistoryRepository
{
    Task<IEnumerable<CheckHistory>> GetAllAsync();

    Task<CheckHistory?> GetByIdAsync(long id);

    Task<IEnumerable<CheckHistory>> GetByContractIdAsync(long contractId);

    Task<CheckHistory> CreateAsync(CheckHistory checkHistory);

    Task<CheckHistory?> UpdateAsync(CheckHistory checkHistory);

    Task<bool> DeleteAsync(long id);
}