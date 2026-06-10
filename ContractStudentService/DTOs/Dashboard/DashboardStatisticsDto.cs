namespace ContractStudentService.DTOs.Dashboard;

public class DashboardStatisticsDto
{
    public int TotalStudents { get; set; }

    public int TotalContracts { get; set; }

    public int TotalRegistrations { get; set; }

    public int TotalCheckHistories { get; set; }

    public int ActiveContracts { get; set; }

    public int PendingRegistrations { get; set; }

    public int ApprovedRegistrations { get; set; }

    public int ExpiredContracts { get; set; }
}
