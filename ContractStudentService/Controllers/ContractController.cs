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

        return Ok(contract);
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetByStudentId(long studentId)
    {
        var contracts = await _contractService.GetByStudentIdAsync(studentId);

        return Ok(contracts);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateContractDto dto)
    {
        var createdContract = await _contractService.CreateAsync(dto);

        return Ok(createdContract);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, UpdateContractDto dto)
    {
        var updatedContract = await _contractService.UpdateAsync(id, dto);

        if (updatedContract == null)
            return NotFound();

        return Ok(updatedContract);
    }

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

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(long id)
    {
        var contract = await _contractService.CancelAsync(id);

        if (contract == null)
            return NotFound();

        return Ok(contract);
    }

    [HttpPut("{id}/expire")]
    public async Task<IActionResult> Expire(long id)
    {
        var contract = await _contractService.ExpireAsync(id);

        if (contract == null)
            return NotFound();

        return Ok(contract);
    }
}
