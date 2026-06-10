using ContractStudentService.DTOs.Student;

namespace ContractStudentService.Interfaces;

public interface IStudentService
{
    Task<List<StudentResponseDto>> GetAllAsync();

    Task<StudentResponseDto?> GetByIdAsync(long id);

    Task<StudentResponseDto> CreateAsync(CreateStudentDto dto);

    Task<StudentResponseDto?> UpdateAsync(long id, UpdateStudentDto dto);

    Task<bool> DeleteAsync(long id);
}