using ContractStudentService.DTOs.Student;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;

namespace ContractStudentService.Services;

public class StudentService : IStudentService
{
    private const string DefaultSchoolName = "Trường quản lý ký túc xá";

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
        await ValidateUniqueStudentAsync(null, dto.StudentCode, dto.CCCD, dto.Email);

        var student = new Student
        {
            StudentCode = Normalize(dto.StudentCode).ToUpperInvariant(),
            FullName = Normalize(dto.FullName),
            CCCD = Normalize(dto.CCCD),
            Phone = Normalize(dto.Phone),
            Email = Normalize(dto.Email),
            SchoolName = NormalizeOrDefault(dto.SchoolName, DefaultSchoolName),
            ClassName = Normalize(dto.ClassName),
            FacultyName = Normalize(dto.FacultyName),
            ResidenceHistory = Normalize(dto.ResidenceHistory),
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

        await ValidateUniqueStudentAsync(id, existingStudent.StudentCode, dto.CCCD, dto.Email);

        existingStudent.FullName = Normalize(dto.FullName);
        existingStudent.CCCD = Normalize(dto.CCCD);
        existingStudent.Phone = Normalize(dto.Phone);
        existingStudent.Email = Normalize(dto.Email);
        existingStudent.SchoolName = NormalizeOrDefault(dto.SchoolName, DefaultSchoolName);
        existingStudent.ClassName = Normalize(dto.ClassName);
        existingStudent.FacultyName = Normalize(dto.FacultyName);
        existingStudent.ResidenceHistory = Normalize(dto.ResidenceHistory);
        existingStudent.Gender = dto.Gender;

        var updatedStudent = await _studentRepository.UpdateAsync(existingStudent);

        return updatedStudent == null ? null : MapToDto(updatedStudent);
    }

    private async Task ValidateUniqueStudentAsync(
        long? currentStudentId,
        string studentCode,
        string cccd,
        string email)
    {
        var students = await _studentRepository.GetAllAsync();

        var normalizedCode = Normalize(studentCode);
        var normalizedCccd = Normalize(cccd);
        var normalizedEmail = Normalize(email);

        if (students.Any(student =>
            (!currentStudentId.HasValue || student.Id != currentStudentId.Value) &&
            student.StudentCode.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("MSSV đã tồn tại trong hệ thống.");
        }

        if (students.Any(student =>
            (!currentStudentId.HasValue || student.Id != currentStudentId.Value) &&
            student.CCCD.Equals(normalizedCccd, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("CCCD đã tồn tại trong hệ thống.");
        }

        if (students.Any(student =>
            (!currentStudentId.HasValue || student.Id != currentStudentId.Value) &&
            student.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Email đã tồn tại trong hệ thống.");
        }
    }

    private static string Normalize(string value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static string NormalizeOrDefault(string value, string fallback)
    {
        var normalized = Normalize(value);

        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
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
