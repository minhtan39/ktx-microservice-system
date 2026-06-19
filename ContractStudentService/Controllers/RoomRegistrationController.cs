using ContractStudentService.Entities;
using ContractStudentService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractStudentService.Controllers;

[Authorize]
[ApiController]
[Route("api/registrations")]
[Route("api/[controller]")]
public class RoomRegistrationController : ControllerBase
{
    private readonly IRoomRegistrationService _service;

    public RoomRegistrationController(
        IRoomRegistrationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var registrations = await _service.GetAllAsync();

        if (!User.IsOperationsUser())
        {
            var studentId = User.CurrentStudentId();

            if (!studentId.HasValue)
                return Unauthorized();

            registrations = registrations
                .Where(registration => registration.StudentId == studentId.Value)
                .ToList();
        }

        return Ok(registrations);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var registration = await _service.GetByIdAsync(id);

        if (registration == null)
            return NotFound();

        if (!User.IsOperationsUser() &&
            User.CurrentStudentId() != registration.StudentId)
        {
            return Forbid();
        }

        return Ok(registration);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        RoomRegistration registration)
    {
        if (!User.IsOperationsUser())
        {
            var studentId = User.CurrentStudentId();

            if (!studentId.HasValue)
                return Unauthorized();

            registration.StudentId = studentId.Value;
        }

        var created =
            await _service.CreateAsync(registration);

        return Ok(created);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        long id,
        RoomRegistration registration)
    {
        var updated =
            await _service.UpdateAsync(id, registration);

        if (updated == null)
            return NotFound();

        return Ok(updated);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return Ok(new
        {
            message = "Registration deleted successfully"
        });
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(
        long id,
        [FromQuery] long? roomId)
    {
        var registration =
            await _service.ApproveAsync(id, roomId);

        if (registration == null)
            return NotFound();

        return Ok(registration);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> Reject(
        long id,
        RejectRoomRegistrationRequest request)
    {
        var registration =
            await _service.RejectAsync(id, request.Reason);

        if (registration == null)
            return NotFound();

        return Ok(registration);
    }
}

public sealed record RejectRoomRegistrationRequest(string Reason);
