using ContractStudentService.Common;
using ContractStudentService.DTOs.Student;
using ContractStudentService.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ContractStudentService.Controllers;

[Authorize]
[ApiController]
[Route("api/students")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var students = await _studentService.GetAllAsync();

        if (!User.IsOperationsUser())
        {
            var studentId = User.CurrentStudentId();

            if (!studentId.HasValue)
                return Unauthorized();

            students = students
                .Where(student => student.Id == studentId.Value)
                .ToList();
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Students retrieved successfully",
            Data = students
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var student = await _studentService.GetByIdAsync(id);

        if (student == null)
            return NotFound();

        if (!User.IsOperationsUser() && User.CurrentStudentId() != id)
            return Forbid();

        return Ok(student);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateStudentDto dto)
    {
        var createdStudent = await _studentService.CreateAsync(dto);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Student created successfully",
            Data = createdStudent
        });
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, UpdateStudentDto dto)
    {
        var updatedStudent = await _studentService.UpdateAsync(id, dto);

        if (updatedStudent == null)
            return NotFound();

        return Ok(updatedStudent);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var deleted = await _studentService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return Ok(new
        {
            message = "Student deleted successfully"
        });
    }
}
