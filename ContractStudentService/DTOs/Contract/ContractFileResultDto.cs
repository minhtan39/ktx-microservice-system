namespace ContractStudentService.DTOs.Contract;

public sealed record ContractFileResultDto(
    Stream Stream,
    string FileName,
    string ContentType);
