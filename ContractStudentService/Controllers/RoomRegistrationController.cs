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

        return Ok(registrations);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var registration = await _service.GetByIdAsync(id);

        if (registration == null)
            return NotFound();

        return Ok(registration);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        RoomRegistration registration)
    {
        var created =
            await _service.CreateAsync(registration);

        return Ok(created);
    }

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

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> Reject(long id)
    {
        var registration =
            await _service.RejectAsync(id);

        if (registration == null)
            return NotFound();

        return Ok(registration);
    }
}
