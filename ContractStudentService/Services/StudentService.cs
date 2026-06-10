using ContractStudentService.DTOs.Student;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;

namespace ContractStudentService.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;

    public StudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<List<StudentResponseDto>> GetAllAsync()
    {
        var students = await _studentRepository.GetAllAsync();

        return students.Select(MapToDto).ToList();
    }

    public async Task<StudentResponseDto?> GetByIdAsync(long id)
    {
        var student = await _studentRepository.GetByIdAsync(id);

        if (student == null)
            return null;

        return MapToDto(student);
    }

    public async Task<StudentResponseDto> CreateAsync(CreateStudentDto dto)
    {
        var student = new Student
        {
            StudentCode = dto.StudentCode,
            FullName = dto.FullName,
            CCCD = dto.CCCD,
            Phone = dto.Phone,
            Email = dto.Email,
            SchoolName = dto.SchoolName,
            ClassName = dto.ClassName,
            FacultyName = dto.FacultyName,
            Gender = dto.Gender
        };

        var createdStudent = await _studentRepository.CreateAsync(student);

        return MapToDto(createdStudent);
    }

    public async Task<StudentResponseDto?> UpdateAsync(long id, UpdateStudentDto dto)
    {
        var existingStudent = await _studentRepository.GetByIdAsync(id);

        if (existingStudent == null)
            return null;

        existingStudent.FullName = dto.FullName;
        existingStudent.Phone = dto.Phone;
        existingStudent.Email = dto.Email;
        existingStudent.SchoolName = dto.SchoolName;
        existingStudent.ClassName = dto.ClassName;
        existingStudent.FacultyName = dto.FacultyName;
        existingStudent.ResidenceHistory = dto.ResidenceHistory;
        existingStudent.Gender = dto.Gender;

        var updatedStudent = await _studentRepository.UpdateAsync(existingStudent);

        return updatedStudent == null ? null : MapToDto(updatedStudent);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await _studentRepository.DeleteAsync(id);
    }

    private static StudentResponseDto MapToDto(Student student)
    {
        return new StudentResponseDto
        {
            Id = student.Id,
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            CCCD = student.CCCD,
            Phone = student.Phone,
            Email = student.Email,
            SchoolName = student.SchoolName,
            ClassName = student.ClassName,
            FacultyName = student.FacultyName,
            Gender = student.Gender,
            Status = student.Status,
            RiskScore = student.RiskScore,
            ResidenceHistory = student.ResidenceHistory,
            CreatedAt = student.CreatedAt
        };
    }
}
