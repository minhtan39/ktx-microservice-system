namespace ContractStudentService.DTOs.Student;

public class CreateStudentDto
{
    public string StudentCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string CCCD { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string SchoolName { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public string FacultyName { get; set; } = string.Empty;

    public bool Gender { get; set; }
}
