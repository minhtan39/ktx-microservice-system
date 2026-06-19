using ContractStudentService.DTOs.CheckHistory;
using ContractStudentService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractStudentService.Controllers;

[Authorize(Roles = "Admin,Staff")]
[ApiController]
[Route("api/[controller]")]
public class CheckHistoryController : ControllerBase
{
    private readonly ICheckHistoryService _checkHistoryService;

    public CheckHistoryController(ICheckHistoryService checkHistoryService)
    {
        _checkHistoryService = checkHistoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var histories = await _checkHistoryService.GetAllAsync();

        return Ok(histories);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var history = await _checkHistoryService.GetByIdAsync(id);

        if (history == null)
            return NotFound();

        return Ok(history);
    }

    [HttpGet("contract/{contractId}")]
    public async Task<IActionResult> GetByContractId(long contractId)
    {
        var histories = await _checkHistoryService.GetByContractIdAsync(contractId);

        return Ok(histories);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCheckHistoryDto dto)
    {
        var createdHistory = await _checkHistoryService.CreateAsync(dto);

        return Ok(createdHistory);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, UpdateCheckHistoryDto dto)
    {
        var updatedHistory = await _checkHistoryService.UpdateAsync(id, dto);

        if (updatedHistory == null)
            return NotFound();

        return Ok(updatedHistory);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var deleted = await _checkHistoryService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return Ok(new
        {
            message = "CheckHistory deleted successfully"
        });
    }
}
