using ContractStudentService.DTOs.Contract;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace ContractStudentService.Services;

public class ContractService : IContractService
{
    private readonly IContractRepository _contractRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IRoomGatewayClient _roomGatewayClient;

    public ContractService(
        IContractRepository contractRepository,
        IStudentRepository studentRepository,
        IRoomGatewayClient roomGatewayClient)
    {
        _contractRepository = contractRepository;
        _studentRepository = studentRepository;
        _roomGatewayClient = roomGatewayClient;
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
            Status = "PendingSignature"
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

        if (!IsActiveOrPendingSignature(contract.Status))
            throw new InvalidOperationException("Chỉ được hủy hợp đồng đang hiệu lực hoặc đang chờ ký.");

        await _roomGatewayClient.ReleaseRoomAsync(
            contract.RoomId,
            contract.StudentId,
            contract.ContractCode);

        contract.Status = "Cancelled";

        var student = await _studentRepository.GetByIdAsync(contract.StudentId);
        if (student != null)
        {
            student.Status = "Pending";
            student.ResidenceHistory = string.IsNullOrWhiteSpace(student.ResidenceHistory)
                ? $"Đã hủy hợp đồng {contract.ContractCode}"
                : $"{student.ResidenceHistory}; Đã hủy hợp đồng {contract.ContractCode}";

            await _studentRepository.UpdateAsync(student);
        }

        return await _contractRepository.UpdateAsync(contract);
    }

    public async Task<Contract?> ExpireAsync(long id)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        if (!contract.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ được kết thúc hợp đồng đang hiệu lực.");

        await _roomGatewayClient.ReleaseRoomAsync(
            contract.RoomId,
            contract.StudentId,
            contract.ContractCode);

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

    public async Task<Contract?> RenewAsync(long id, RenewContractDto dto)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        if (!contract.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ được gia hạn hợp đồng đang hiệu lực.");

        if (dto.NewEndDate.Date <= contract.EndDate.Date)
            throw new InvalidOperationException("Ngày kết thúc mới phải sau ngày kết thúc hiện tại.");

        contract.EndDate = dto.NewEndDate.Date;
        contract.RenewalCount++;
        contract.LastRenewedAt = DateTime.UtcNow;
        contract.RenewalNote = string.IsNullOrWhiteSpace(dto.Note)
            ? $"Gia hạn lần {contract.RenewalCount}"
            : dto.Note.Trim();
        contract.Terms = string.IsNullOrWhiteSpace(contract.Terms)
            ? $"Hợp đồng được gia hạn đến ngày {contract.EndDate:dd/MM/yyyy}."
            : $"{contract.Terms} Gia hạn lần {contract.RenewalCount} đến ngày {contract.EndDate:dd/MM/yyyy}.";

        return await _contractRepository.UpdateAsync(contract);
    }

    public async Task<Contract?> SignAsync(
        long id,
        SignContractDto dto,
        string ipAddress)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        if (!IsActiveOrPendingSignature(contract.Status))
            throw new InvalidOperationException("Chỉ được ký hợp đồng đang hiệu lực hoặc đang chờ ký.");

        if (contract.SignedAt.HasValue)
            throw new InvalidOperationException("Hợp đồng này đã được ký online.");

        if (!dto.AcceptedTerms)
            throw new InvalidOperationException("Sinh viên phải xác nhận đã đọc và đồng ý điều khoản hợp đồng.");

        if (string.IsNullOrWhiteSpace(dto.FullName) ||
            string.IsNullOrWhiteSpace(dto.StudentCode))
        {
            throw new InvalidOperationException("Vui lòng nhập đầy đủ họ tên và MSSV để ký hợp đồng.");
        }

        var student = await _studentRepository.GetByIdAsync(contract.StudentId);
        var submittedStudentCode = dto.StudentCode.Trim();

        if (student == null ||
            !string.Equals(
                student.StudentCode.Trim(),
                submittedStudentCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("MSSV ký hợp đồng không khớp với hồ sơ sinh viên.");
        }

        var signedAt = DateTime.UtcNow;

        contract.SignatureFullName = dto.FullName.Trim();
        contract.SignatureStudentCode = submittedStudentCode.ToUpperInvariant();
        contract.SignatureIpAddress = ipAddress;
        contract.SignedAt = signedAt;
        contract.SignatureHash = BuildSignatureHash(contract, signedAt);
        contract.Status = "Active";

        var updatedContract = await _contractRepository.UpdateAsync(contract);

        student.Status = "Active";
        await _studentRepository.UpdateAsync(student);

        return updatedContract;
    }

    private static bool IsActiveOrPendingSignature(string status)
    {
        return status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("PendingSignature", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSignatureHash(
        Contract contract,
        DateTime signedAt)
    {
        var raw = string.Join(
            "|",
            contract.Id,
            contract.ContractCode,
            contract.StudentId,
            contract.SignatureStudentCode,
            signedAt.ToString("O"),
            contract.EndDate.ToString("O"));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));

        return Convert.ToHexString(bytes);
    }
}
