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

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("{id}/template")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadTemplate(long id, [FromForm] IFormFile file)
    {
        if (file == null)
            return BadRequest(new { message = "Vui lòng chọn file PDF hợp đồng." });

        var contract = await _contractService.UploadTemplateAsync(id, file);

        if (contract == null)
            return NotFound();

        return Ok(contract);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("{id}/generate-template")]
    public async Task<IActionResult> GenerateTemplate(
        long id,
        GenerateContractTemplateDto dto)
    {
        var contract = await _contractService.GenerateTemplateAsync(id, dto);

        if (contract == null)
            return NotFound();

        return Ok(contract);
    }

    [HttpGet("{id}/template-file")]
    public async Task<IActionResult> DownloadTemplate(long id)
    {
        var existing = await _contractService.GetByIdAsync(id);

        if (existing == null)
            return NotFound();

        if (!User.IsOperationsUser() &&
            User.CurrentStudentId() != existing.StudentId)
        {
            return Forbid();
        }

        var file = await _contractService.GetTemplateFileAsync(id);

        if (file == null)
            return NotFound(new { message = "Hợp đồng chưa có file PDF mẫu." });

        return File(file.Stream, file.ContentType, file.FileName);
    }

    [HttpGet("{id}/signed-file")]
    public async Task<IActionResult> DownloadSignedFile(long id)
    {
        var existing = await _contractService.GetByIdAsync(id);

        if (existing == null)
            return NotFound();

        if (!User.IsOperationsUser() &&
            User.CurrentStudentId() != existing.StudentId)
        {
            return Forbid();
        }

        var file = await _contractService.GetSignedFileAsync(id);

        if (file == null)
            return NotFound(new { message = "Hợp đồng chưa có file PDF đã ký." });

        return File(file.Stream, file.ContentType, file.FileName);
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
