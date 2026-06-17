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

    public async Task<Contract?> RenewAsync(long id, RenewContractDto dto)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        if (!CanRenew(contract.Status))
            throw new Exception("Chỉ hợp đồng đang hiệu lực hoặc đã hết hạn mới được gia hạn. Hợp đồng đã hủy cần tạo hồ sơ/đơn xếp phòng mới.");

        if (dto.EndDate.Date <= contract.EndDate.Date)
            throw new Exception("Ngày gia hạn mới phải sau ngày kết thúc hiện tại.");

        if (dto.EndDate.Date <= DateTime.UtcNow.Date)
            throw new Exception("Ngày kết thúc mới phải sau ngày hiện tại để hợp đồng có hiệu lực sau gia hạn.");

        await EnsureNoOtherCurrentContractAsync(contract);

        var oldEndDate = contract.EndDate;
        var oldStatus = contract.Status;
        contract.EndDate = dto.EndDate;
        contract.Status = "Active";
        contract.Terms = AppendContractNote(
            contract.Terms,
            BuildRenewalNote(oldStatus, oldEndDate, dto));

        var student = await _studentRepository.GetByIdAsync(contract.StudentId);
        if (student != null)
        {
            student.Status = "Active";
            student.ResidenceHistory = string.IsNullOrWhiteSpace(student.ResidenceHistory)
                ? $"Gia hạn hợp đồng {contract.ContractCode} đến {dto.EndDate:dd/MM/yyyy}"
                : $"{student.ResidenceHistory}; Gia hạn hợp đồng {contract.ContractCode} đến {dto.EndDate:dd/MM/yyyy}";

            await _studentRepository.UpdateAsync(student);
        }

        return await _contractRepository.UpdateAsync(contract);
    }

    public async Task<Contract?> SignAsync(long id, SignContractDto dto)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        if (!contract.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Chỉ hợp đồng đang hiệu lực mới được ký online.");

        if (HasOnlineSignature(contract.Terms))
            throw new Exception("Hợp đồng đã được ký online.");

        var student = await _studentRepository.GetByIdAsync(contract.StudentId);

        if (student == null)
            throw new Exception("Student không tồn tại.");

        var signerName = dto.SignerName.Trim();

        if (string.IsNullOrWhiteSpace(signerName))
            throw new Exception("Vui lòng nhập họ tên người ký.");

        if (!signerName.Equals(student.FullName, StringComparison.OrdinalIgnoreCase))
            throw new Exception("Họ tên người ký phải trùng với họ tên sinh viên trong hồ sơ.");

        var method = string.IsNullOrWhiteSpace(dto.Method)
            ? "Online"
            : dto.Method.Trim();

        contract.Terms = AppendContractNote(
            contract.Terms,
            $"Ký điện tử: {signerName}, phương thức {method}, thời điểm {DateTime.Now:dd/MM/yyyy HH:mm}.");

        return await _contractRepository.UpdateAsync(contract);
    }

    private static bool HasOnlineSignature(string terms)
    {
        return terms.Contains("Ký điện tử:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanRenew(string status)
    {
        return status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("Expired", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildRenewalNote(
        string oldStatus,
        DateTime oldEndDate,
        RenewContractDto dto)
    {
        var action = oldStatus.Equals("Expired", StringComparison.OrdinalIgnoreCase)
            ? "Gia hạn và hồi hiệu lực hợp đồng"
            : "Gia hạn hợp đồng";
        var note = string.IsNullOrWhiteSpace(dto.Note)
            ? string.Empty
            : $" {dto.Note.Trim()}";

        return $"{action} từ {oldEndDate:dd/MM/yyyy} đến {dto.EndDate:dd/MM/yyyy}.{note}".Trim();
    }

    private async Task EnsureNoOtherCurrentContractAsync(Contract contract)
    {
        var today = DateTime.UtcNow.Date;
        var studentContracts = await _contractRepository.GetByStudentIdAsync(contract.StudentId);
        var hasOtherCurrentContract = studentContracts.Any(existing =>
            existing.Id != contract.Id &&
            existing.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) &&
            existing.EndDate.Date >= today);

        if (hasOtherCurrentContract)
        {
            throw new Exception("Sinh viên đã có hợp đồng khác đang hiệu lực. Không thể hồi hiệu lực/gia hạn hợp đồng cũ.");
        }
    }

    private static string AppendContractNote(string terms, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return terms;

        var baseTerms = string.IsNullOrWhiteSpace(terms)
            ? "Sinh viên đóng tiền đúng hạn, tuân thủ nội quy ký túc xá và bàn giao phòng khi kết thúc hợp đồng."
            : terms.Trim();

        return $"{baseTerms}\n\n[{DateTime.Now:dd/MM/yyyy HH:mm}] {note}";
    }
}
