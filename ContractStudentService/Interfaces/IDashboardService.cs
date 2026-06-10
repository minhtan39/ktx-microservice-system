using ContractStudentService.DTOs.Dashboard;

namespace ContractStudentService.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatisticsDto> GetStatisticsAsync();
}