using ContractStudentService.Entities;

namespace ContractStudentService.Interfaces;

public interface IContractRepository
{
    Task<IEnumerable<Contract>> GetAllAsync();

    Task<Contract?> GetByIdAsync(long id);

    Task<IEnumerable<Contract>> GetByStudentIdAsync(long studentId);

    Task<Contract> CreateAsync(Contract contract);

    Task<Contract?> UpdateAsync(Contract contract);

    Task<bool> DeleteAsync(long id);
}
