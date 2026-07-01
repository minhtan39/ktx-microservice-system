using ContractStudentService.Data;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractStudentService.Repositories;

public class RoomRegistrationRepository : IRoomRegistrationRepository
{
	private readonly ApplicationDbContext _context;

	public RoomRegistrationRepository(ApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<IEnumerable<RoomRegistration>> GetAllAsync()
	{
		return await _context.RoomRegistrations.ToListAsync();
	}

	public async Task<RoomRegistration?> GetByIdAsync(long id)
	{
		return await _context.RoomRegistrations.FindAsync(id);
	}

	public async Task<RoomRegistration> CreateAsync(RoomRegistration registration)
	{
		_context.RoomRegistrations.Add(registration);

		await _context.SaveChangesAsync();

		return registration;
	}

	public async Task<RoomRegistration?> UpdateAsync(RoomRegistration registration)
	{
		var existingRegistration =
			await _context.RoomRegistrations.FindAsync(registration.Id);

		if (existingRegistration == null)
			return null;

		existingRegistration.StudentId = registration.StudentId;
		existingRegistration.BuildingName = registration.BuildingName;
		existingRegistration.RoomType = registration.RoomType;
		existingRegistration.PriorityType = registration.PriorityType;
		existingRegistration.PriorityScore = registration.PriorityScore;
		existingRegistration.PriorityNote = registration.PriorityNote;
		existingRegistration.StartDate = registration.StartDate;
		existingRegistration.EndDate = registration.EndDate;
		existingRegistration.Status = registration.Status;
		existingRegistration.AssignedRoomId = registration.AssignedRoomId;
		existingRegistration.AssignmentMode = registration.AssignmentMode;
		existingRegistration.AssignmentNote = registration.AssignmentNote;
		existingRegistration.AssignedAt = registration.AssignedAt;
		existingRegistration.RejectionReason = registration.RejectionReason;
		existingRegistration.RejectedAt = registration.RejectedAt;

		await _context.SaveChangesAsync();

		return existingRegistration;
	}

	public async Task<bool> DeleteAsync(long id)
	{
		var registration =
			await _context.RoomRegistrations.FindAsync(id);

		if (registration == null)
			return false;

		_context.RoomRegistrations.Remove(registration);

		await _context.SaveChangesAsync();

		return true;
	}
}
