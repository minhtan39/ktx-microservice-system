using ContractStudentService.DTOs.CheckHistory;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;

namespace ContractStudentService.Services;

public class CheckHistoryService : ICheckHistoryService
{
    private readonly ICheckHistoryRepository _checkHistoryRepository;
    private readonly IContractRepository _contractRepository;

    public CheckHistoryService(
        ICheckHistoryRepository checkHistoryRepository,
        IContractRepository contractRepository)
    {
        _checkHistoryRepository = checkHistoryRepository;
        _contractRepository = contractRepository;
    }

    public async Task<IEnumerable<CheckHistory>> GetAllAsync()
    {
        return await _checkHistoryRepository.GetAllAsync();
    }

    public async Task<CheckHistory?> GetByIdAsync(long id)
    {
        return await _checkHistoryRepository.GetByIdAsync(id);
    }

    public async Task<CheckHistory> CreateAsync(CreateCheckHistoryDto dto)
    {
        var contract = await _contractRepository.GetByIdAsync(dto.ContractId);

        if (contract == null)
            throw new Exception("Contract không tồn tại.");

        var history = new CheckHistory
        {
            ContractId = dto.ContractId,
            CheckType = dto.CheckType,
            Note = dto.Note,
            CheckTime = DateTime.UtcNow
        };

        return await _checkHistoryRepository.CreateAsync(history);
    }

    public async Task<CheckHistory?> UpdateAsync(long id, UpdateCheckHistoryDto dto)
    {
        var history = await _checkHistoryRepository.GetByIdAsync(id);

        if (history == null)
            return null;

        history.CheckType = dto.CheckType;
        history.Note = dto.Note;

        return await _checkHistoryRepository.UpdateAsync(history);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await _checkHistoryRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<CheckHistory>> GetByContractIdAsync(long contractId)
    {
        return await _checkHistoryRepository.GetByContractIdAsync(contractId);
    }
}