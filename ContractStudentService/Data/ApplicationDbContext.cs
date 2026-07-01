using ContractStudentService.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractStudentService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<CheckHistory> CheckHistories => Set<CheckHistory>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RoomRegistration> RoomRegistrations => Set<RoomRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contract>()
            .Property(c => c.DepositAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Contract>()
            .Property(c => c.MonthlyFee)
            .HasPrecision(18, 2);

        modelBuilder.Entity<RoomRegistration>()
            .Property(registration => registration.AssignmentMode)
            .HasMaxLength(32);

        modelBuilder.Entity<RoomRegistration>()
            .Property(registration => registration.AssignmentNote)
            .HasMaxLength(500);
    }
}
