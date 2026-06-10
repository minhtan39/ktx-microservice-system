using ContractStudentService.Entities;

namespace ContractStudentService.Interfaces;

public interface IStudentRepository
{
    Task<List<Student>> GetAllAsync();

    Task<Student?> GetByIdAsync(long id);

    Task<Student> CreateAsync(Student student);

    Task<Student?> UpdateAsync(Student student);

    Task<bool> DeleteAsync(long id);
}