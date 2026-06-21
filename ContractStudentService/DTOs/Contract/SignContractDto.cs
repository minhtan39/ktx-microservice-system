namespace ContractStudentService.DTOs.Contract;

public class SignContractDto
{
    public string FullName { get; set; } = string.Empty;

    public string StudentCode { get; set; } = string.Empty;

    public string SignatureImageDataUrl { get; set; } = string.Empty;

    public bool AcceptedTerms { get; set; }
}
