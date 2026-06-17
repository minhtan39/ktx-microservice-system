namespace ContractStudentService.DTOs.Contract;

public class SignContractDto
{
    public string SignerName { get; set; } = string.Empty;

    public string Method { get; set; } = "Online";
}
