using ContractStudentService.DTOs.Contract;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;

namespace ContractStudentService.Services;

public class ContractService : IContractService
{
    private readonly IContractRepository _contractRepository;
    private readonly IStudentRepository _studentRepository;

    public ContractService(
        IContractRepository contractRepository,
        IStudentRepository studentRepository)
    {
        _contractRepository = contractRepository;
        _studentRepository = studentRepository;
    }

    public async Task<IEnumerable<Contract>> GetAllAsync()
    {
        return await _contractRepository.GetAllAsync();
    }

    public async Task<Contract?> GetByIdAsync(long id)
    {
        return await _contractRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Contract>> GetByStudentIdAsync(long studentId)
    {
        return await _contractRepository.GetByStudentIdAsync(studentId);
    }

    public async Task<Contract> CreateAsync(CreateContractDto dto)
    {
        var student = await _studentRepository.GetByIdAsync(dto.StudentId);

        if (student == null)
            throw new Exception("Student không tồn tại.");

        var contract = new Contract
        {
            ContractCode = string.IsNullOrWhiteSpace(dto.ContractCode)
                ? $"HD-{DateTime.Now:yyyyMMddHHmmss}"
                : dto.ContractCode,
            StudentId = dto.StudentId,
            RoomId = dto.RoomId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            DepositAmount = dto.DepositAmount,
            MonthlyFee = dto.MonthlyFee,
            Terms = string.IsNullOrWhiteSpace(dto.Terms)
                ? "Sinh viên đóng tiền đúng hạn, tuân thủ nội quy ký túc xá và bàn giao phòng khi kết thúc hợp đồng."
                : dto.Terms,
            Status = "Active"
        };

        return await _contractRepository.CreateAsync(contract);
    }

    public async Task<Contract?> UpdateAsync(long id, UpdateContractDto dto)
    {
        var existingContract = await _contractRepository.GetByIdAsync(id);

        if (existingContract == null)
            return null;

        existingContract.ContractCode = dto.ContractCode;
        existingContract.StudentId = dto.StudentId;
        existingContract.RoomId = dto.RoomId;
        existingContract.StartDate = dto.StartDate;
        existingContract.EndDate = dto.EndDate;
        existingContract.DepositAmount = dto.DepositAmount;
        existingContract.MonthlyFee = dto.MonthlyFee;
        existingContract.Terms = dto.Terms;
        existingContract.Status = dto.Status;

        return await _contractRepository.UpdateAsync(existingContract);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await _contractRepository.DeleteAsync(id);
    }

    public async Task<Contract?> CancelAsync(long id)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        contract.Status = "Cancelled";

        var student = await _studentRepository.GetByIdAsync(contract.StudentId);
        if (student != null)
        {
            student.Status = "Pending";
            await _studentRepository.UpdateAsync(student);
        }

        return await _contractRepository.UpdateAsync(contract);
    }

    public async Task<Contract?> ExpireAsync(long id)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        contract.Status = "Expired";

        var student = await _studentRepository.GetByIdAsync(contract.StudentId);
        if (student != null)
        {
            student.Status = "CheckedOut";
            student.ResidenceHistory = string.IsNullOrWhiteSpace(student.ResidenceHistory)
                ? $"Đã kết thúc hợp đồng {contract.ContractCode}"
                : $"{student.ResidenceHistory}; Đã kết thúc hợp đồng {contract.ContractCode}";

            await _studentRepository.UpdateAsync(student);
        }

        return await _contractRepository.UpdateAsync(contract);
    }
}
