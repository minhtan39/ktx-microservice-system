using ContractStudentService.Data;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractStudentService.Repositories;

public class ContractRepository : IContractRepository
{
    private readonly ApplicationDbContext _context;

    public ContractRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Contract>> GetAllAsync()
    {
        return await _context.Contracts.ToListAsync();
    }

    public async Task<Contract?> GetByIdAsync(long id)
    {
        return await _context.Contracts.FindAsync(id);
    }

    public async Task<IEnumerable<Contract>> GetByStudentIdAsync(long studentId)
    {
        return await _context.Contracts
            .Where(contract => contract.StudentId == studentId)
            .OrderByDescending(contract => contract.CreatedAt)
            .ToListAsync();
    }

    public async Task<Contract> CreateAsync(Contract contract)
    {
        _context.Contracts.Add(contract);

        await _context.SaveChangesAsync();

        return contract;
    }

    public async Task<Contract?> UpdateAsync(Contract contract)
    {
        _context.Contracts.Update(contract);

        await _context.SaveChangesAsync();

        return contract;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var contract = await _context.Contracts.FindAsync(id);

        if (contract == null)
            return false;

        _context.Contracts.Remove(contract);

        await _context.SaveChangesAsync();

        return true;
    }
}
