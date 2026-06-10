using ContractStudentService.Data;
using ContractStudentService.DTOs.Dashboard;
using ContractStudentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractStudentService.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatisticsDto> GetStatisticsAsync()
    {
        return new DashboardStatisticsDto
        {
            TotalStudents = await _context.Students.CountAsync(),

            TotalContracts = await _context.Contracts.CountAsync(),

            TotalRegistrations = await _context.RoomRegistrations.CountAsync(),

            TotalCheckHistories = await _context.CheckHistories.CountAsync(),

            ActiveContracts = await _context.Contracts
                .CountAsync(c => c.Status == "Active"),

            PendingRegistrations = await _context.RoomRegistrations
                .CountAsync(r => r.Status == "Pending"),

            ApprovedRegistrations = await _context.RoomRegistrations
                .CountAsync(r => r.Status == "Approved"),

            ExpiredContracts = await _context.Contracts
                .CountAsync(c => c.Status == "Expired")
        };
    }
}
