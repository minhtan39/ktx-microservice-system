using ContractStudentService.DTOs.Contract;
using ContractStudentService.Entities;
using Microsoft.AspNetCore.Http;

namespace ContractStudentService.Interfaces;

public interface IContractService
{
    Task<IEnumerable<Contract>> GetAllAsync();

    Task<Contract?> GetByIdAsync(long id);

    Task<IEnumerable<Contract>> GetByStudentIdAsync(long studentId);

    Task<Contract> CreateAsync(CreateContractDto dto);

    Task<Contract?> UpdateAsync(long id, UpdateContractDto dto);

    Task<bool> DeleteAsync(long id);

    Task<Contract?> CancelAsync(long id);

    Task<Contract?> ExpireAsync(long id);

    Task<Contract?> RenewAsync(long id, RenewContractDto dto);

    Task<Contract?> SignAsync(long id, SignContractDto dto, string ipAddress);

    Task<Contract?> UploadTemplateAsync(long id, IFormFile file);

    Task<ContractFileResultDto?> GetTemplateFileAsync(long id);

    Task<ContractFileResultDto?> GetSignedFileAsync(long id);
}
