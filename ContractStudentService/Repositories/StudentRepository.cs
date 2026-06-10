using ContractStudentService.Data;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractStudentService.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly ApplicationDbContext _context;

    public StudentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Student>> GetAllAsync()
    {
        return await _context.Students.ToListAsync();
    }

    public async Task<Student?> GetByIdAsync(long id)
    {
        return await _context.Students.FindAsync(id);
    }

    public async Task<Student> CreateAsync(Student student)
    {
        _context.Students.Add(student);

        await _context.SaveChangesAsync();

        return student;
    }

    public async Task<Student?> UpdateAsync(Student student)
    {
        var existingStudent = await _context.Students.FindAsync(student.Id);

        if (existingStudent == null)
            return null;

        existingStudent.FullName = student.FullName;
        existingStudent.Phone = student.Phone;
        existingStudent.Email = student.Email;
        existingStudent.SchoolName = student.SchoolName;
        existingStudent.ClassName = student.ClassName;
        existingStudent.FacultyName = student.FacultyName;
        existingStudent.ResidenceHistory = student.ResidenceHistory;
        existingStudent.Gender = student.Gender;
        existingStudent.Status = student.Status;
        existingStudent.RiskScore = student.RiskScore;

        await _context.SaveChangesAsync();

        return existingStudent;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var student = await _context.Students.FindAsync(id);

        if (student == null)
            return false;

        _context.Students.Remove(student);

        await _context.SaveChangesAsync();

        return true;
    }
}
