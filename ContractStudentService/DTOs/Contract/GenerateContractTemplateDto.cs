namespace ContractStudentService.DTOs.Contract;

public class GenerateContractTemplateDto
{
    public string ParentOrganization { get; set; } = string.Empty;

    public string DormitoryName { get; set; } = string.Empty;

    public string RepresentativeName { get; set; } = string.Empty;

    public string RepresentativeTitle { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string BankAccount { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string PlaceOfSigning { get; set; } = string.Empty;

    public DateTime? IssueDate { get; set; }
}
