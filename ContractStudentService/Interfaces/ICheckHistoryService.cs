using ContractStudentService.DTOs.CheckHistory;
using ContractStudentService.Entities;

namespace ContractStudentService.Interfaces;

public interface ICheckHistoryService
{
    Task<IEnumerable<CheckHistory>> GetAllAsync();

    Task<CheckHistory?> GetByIdAsync(long id);
    Task<IEnumerable<CheckHistory>> GetByContractIdAsync(long contractId);

    Task<CheckHistory> CreateAsync(CreateCheckHistoryDto dto);

    Task<CheckHistory?> UpdateAsync(long id, UpdateCheckHistoryDto dto);

    Task<bool> DeleteAsync(long id);
}