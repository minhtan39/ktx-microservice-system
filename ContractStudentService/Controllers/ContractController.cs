using ContractStudentService.DTOs.Contract;
using ContractStudentService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractStudentService.Controllers;

[Authorize]
[ApiController]
[Route("api/contracts")]
[Route("api/[controller]")]
public class ContractController : ControllerBase
{
    private readonly IContractService _contractService;

    public ContractController(IContractService contractService)
    {
        _contractService = contractService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetAll()
    {
        var contracts = await _contractService.GetAllAsync();
        return Ok(contracts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var contract = await _contractService.GetByIdAsync(id);

        if (contract == null)
            return NotFound();

        if (!User.IsOperationsUser() &&
            User.CurrentStudentId() != contract.StudentId)
        {
            return Forbid();
        }

        return Ok(contract);
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetByStudentId(long studentId)
    {
        if (!User.IsOperationsUser() &&
            User.CurrentStudentId() != studentId)
        {
            return Forbid();
        }

        var contracts = await _contractService.GetByStudentIdAsync(studentId);

        return Ok(contracts);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateContractDto dto)
    {
        var createdContract = await _contractService.CreateAsync(dto);

        return Ok(createdContract);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, UpdateContractDto dto)
    {
        var updatedContract = await _contractService.UpdateAsync(id, dto);

        if (updatedContract == null)
            return NotFound();

        return Ok(updatedContract);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var deleted = await _contractService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return Ok(new
        {
            message = "Contract deleted successfully"
        });
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(long id)
    {
        var contract = await _contractService.CancelAsync(id);

        if (contract == null)
            return NotFound();

        return Ok(contract);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id}/expire")]
    public async Task<IActionResult> Expire(long id)
    {
        var contract = await _contractService.ExpireAsync(id);

        if (contract == null)
            return NotFound();

        return Ok(contract);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id}/renew")]
    public async Task<IActionResult> Renew(
        long id,
        RenewContractDto dto)
    {
        var contract = await _contractService.RenewAsync(id, dto);

        if (contract == null)
            return NotFound();

        return Ok(contract);
    }

    [HttpPost("{id}/sign")]
    public async Task<IActionResult> Sign(
        long id,
        SignContractDto dto)
    {
        var existing = await _contractService.GetByIdAsync(id);

        if (existing == null)
            return NotFound();

        if (!User.IsOperationsUser() &&
            User.CurrentStudentId() != existing.StudentId)
        {
            return Forbid();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var contract = await _contractService.SignAsync(id, dto, ipAddress);

        return Ok(contract);
    }
}
