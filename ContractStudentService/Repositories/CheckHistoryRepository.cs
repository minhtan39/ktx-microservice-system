using ContractStudentService.Data;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractStudentService.Repositories;

public class CheckHistoryRepository : ICheckHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public CheckHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CheckHistory>> GetAllAsync()
    {
        return await _context.CheckHistories.ToListAsync();
    }

    public async Task<CheckHistory?> GetByIdAsync(long id)
    {
        return await _context.CheckHistories.FindAsync(id);
    }

    public async Task<IEnumerable<CheckHistory>> GetByContractIdAsync(long contractId)
    {
        return await _context.CheckHistories
            .Where(x => x.ContractId == contractId)
            .OrderByDescending(x => x.CheckTime)
            .ToListAsync();
    }

    public async Task<CheckHistory> CreateAsync(CheckHistory checkHistory)
    {
        _context.CheckHistories.Add(checkHistory);

        await _context.SaveChangesAsync();

        return checkHistory;
    }

    public async Task<CheckHistory?> UpdateAsync(CheckHistory checkHistory)
    {
        _context.CheckHistories.Update(checkHistory);

        await _context.SaveChangesAsync();

        return checkHistory;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var history = await _context.CheckHistories.FindAsync(id);

        if (history == null)
            return false;

        _context.CheckHistories.Remove(history);

        await _context.SaveChangesAsync();

        return true;
    }
}