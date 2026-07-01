using System.Collections.Concurrent;
using ContractStudentService.DTOs.Integration;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;

namespace ContractStudentService.Services;

public class RoomRegistrationService : IRoomRegistrationService
{
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> ApprovalLocks = new();

    private readonly IRoomRegistrationRepository _repository;
    private readonly IContractRepository _contractRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IRoomGatewayClient _roomGatewayClient;
    private readonly IBillingGatewayClient _billingGatewayClient;

    public RoomRegistrationService(
        IRoomRegistrationRepository repository,
        IContractRepository contractRepository,
        IStudentRepository studentRepository,
        IRoomGatewayClient roomGatewayClient,
        IBillingGatewayClient billingGatewayClient)
    {
        _repository = repository;
        _contractRepository = contractRepository;
        _studentRepository = studentRepository;
        _roomGatewayClient = roomGatewayClient;
        _billingGatewayClient = billingGatewayClient;
    }

    public async Task<IEnumerable<RoomRegistration>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<RoomRegistration?> GetByIdAsync(long id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<RoomRegistration> CreateAsync(
        RoomRegistration registration)
    {
        var student = await _studentRepository.GetByIdAsync(registration.StudentId);

        if (student == null)
            throw new Exception("Student không tồn tại.");

        ValidateRegistrationDates(registration.StartDate, registration.EndDate);
        await EnsureStudentCanRegisterAsync(registration.StudentId);

        registration.PriorityScore = CalculatePriorityScore(
            registration.PriorityType,
            student.RiskScore);
        registration.Status = "Pending";
        registration.AssignedRoomId = null;
        registration.RejectionReason = string.Empty;
        registration.RejectedAt = null;

        return await _repository.CreateAsync(registration);
    }

    public async Task<RoomRegistration?> UpdateAsync(
        long id,
        RoomRegistration registration)
    {
        var existing = await _repository.GetByIdAsync(id);

        if (existing == null)
            return null;

        if (!existing.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ được cập nhật đơn đang chờ duyệt.");

        var student = await _studentRepository.GetByIdAsync(registration.StudentId);

        if (student == null)
            throw new Exception("Student không tồn tại.");

        ValidateRegistrationDates(registration.StartDate, registration.EndDate);

        existing.StudentId = registration.StudentId;
        existing.BuildingName = registration.BuildingName;
        existing.RoomType = registration.RoomType;
        existing.PriorityType = registration.PriorityType;
        existing.PriorityScore = CalculatePriorityScore(
            registration.PriorityType,
            student.RiskScore);
        existing.PriorityNote = registration.PriorityNote;
        existing.StartDate = registration.StartDate;
        existing.EndDate = registration.EndDate;

        return await _repository.UpdateAsync(existing);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<RoomRegistration?> ApproveAsync(
        long id,
        long? roomId,
        string? assignmentNote)
    {
        var approvalLock = ApprovalLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await approvalLock.WaitAsync();

        try
        {
            var registration = await _repository.GetByIdAsync(id);

        if (registration == null)
            return null;

        if (registration.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            return registration;

        if (!registration.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ được duyệt đơn đang chờ xử lý.");

        var student = await _studentRepository.GetByIdAsync(registration.StudentId);

        if (student == null)
            throw new Exception("Student không tồn tại.");

        await EnsureStudentHasNoActiveContractAsync(student.Id);

        var assignedRoom = await _roomGatewayClient.FindAvailableRoomAsync(
            registration,
            student,
            roomId);

        if (assignedRoom == null)
            throw new Exception("Không tìm thấy phòng phù hợp hoặc phòng đã hết giường.");

        var normalizedAssignmentNote = NormalizeAssignmentNote(assignmentNote);
        var assignmentMode = roomId.HasValue ? "Manual" : "Automatic";
        var isOverride = roomId.HasValue && IsAssignmentOverride(registration, assignedRoom);

        if (isOverride && string.IsNullOrWhiteSpace(normalizedAssignmentNote))
            throw new InvalidOperationException("Vui lòng nhập lý do khi xếp phòng khác nguyện vọng ban đầu.");

        if (string.IsNullOrWhiteSpace(normalizedAssignmentNote))
            normalizedAssignmentNote = BuildAssignmentNote(
                registration,
                assignedRoom,
                assignmentMode);

        var contractCode = $"HD-{DateTime.Now:yyyyMMddHHmmss}";

        Contract? createdContract = null;
        var roomOccupied = false;

        try
        {
            assignedRoom = await _roomGatewayClient.OccupyRoomAsync(
                assignedRoom.RoomId,
                student.Id,
                registration.Id,
                contractCode);
            roomOccupied = true;

            var contract = new Contract
            {
                ContractCode = contractCode,
                StudentId = registration.StudentId,
                RoomId = assignedRoom.RoomId,
                StartDate = registration.StartDate,
                EndDate = registration.EndDate,
                DepositAmount = 500000,
                MonthlyFee = assignedRoom.MonthlyFee,
                Terms = BuildDefaultTerms(registration, normalizedAssignmentNote),
                Status = "PendingSignature"
            };

            createdContract = await _contractRepository.CreateAsync(contract);

            await _billingGatewayClient.CreateContractBillingAsync(createdContract);

            registration.Status = "Approved";
            registration.AssignedRoomId = assignedRoom.RoomId;
            registration.AssignmentMode = assignmentMode;
            registration.AssignmentNote = normalizedAssignmentNote;
            registration.AssignedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(registration);

            student.Status = "PendingSignature";
            student.ResidenceHistory =
                $"Phòng {assignedRoom.RoomId}, {assignedRoom.BuildingName}, {registration.StartDate:dd/MM/yyyy} - {registration.EndDate:dd/MM/yyyy}";

            await _studentRepository.UpdateAsync(student);

            return registration;
        }
        catch
        {
            if (createdContract != null)
                await _contractRepository.DeleteAsync(createdContract.Id);

            if (roomOccupied)
            {
                await _roomGatewayClient.ReleaseRoomAsync(
                    assignedRoom.RoomId,
                    student.Id,
                    contractCode);
            }

            throw;
        }
        }
        finally
        {
            approvalLock.Release();
        }
    }

    public async Task<RoomRegistration?> RejectAsync(long id, string reason)
    {
        var registration = await _repository.GetByIdAsync(id);

        if (registration == null)
            return null;

        if (!registration.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ được từ chối đơn đang chờ xử lý.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Vui lòng nhập lý do từ chối đơn đăng ký.");

        registration.Status = "Rejected";
        registration.RejectionReason = reason.Trim();
        registration.RejectedAt = DateTime.UtcNow;

        return await _repository.UpdateAsync(registration);
    }

    private async Task EnsureStudentCanRegisterAsync(long studentId)
    {
        var registrations = await _repository.GetAllAsync();
        var blockingRegistrationStatuses = new HashSet<string>(
            new[] { "Pending" },
            StringComparer.OrdinalIgnoreCase);

        if (registrations.Any(item =>
            item.StudentId == studentId &&
            blockingRegistrationStatuses.Contains(item.Status)))
        {
            throw new InvalidOperationException("Sinh viên đã có đơn đăng ký đang chờ duyệt.");
        }

        var contracts = await _contractRepository.GetByStudentIdAsync(studentId);
        var blockingContractStatuses = new HashSet<string>(
            new[] { "Active", "PendingSignature" },
            StringComparer.OrdinalIgnoreCase);

        if (contracts.Any(item =>
            blockingContractStatuses.Contains(item.Status)))
        {
            throw new InvalidOperationException("Sinh viên đang có hợp đồng nội trú hiệu lực.");
        }
    }

    private async Task EnsureStudentHasNoActiveContractAsync(long studentId)
    {
        var contracts = await _contractRepository.GetByStudentIdAsync(studentId);
        var blockingContractStatuses = new HashSet<string>(
            new[] { "Active", "PendingSignature" },
            StringComparer.OrdinalIgnoreCase);

        if (contracts.Any(item =>
            blockingContractStatuses.Contains(item.Status)))
        {
            throw new InvalidOperationException("Sinh viên đang có hợp đồng nội trú hiệu lực hoặc đang chờ ký.");
        }
    }

    private static void ValidateRegistrationDates(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new InvalidOperationException("Ngày kết thúc phải sau ngày bắt đầu.");
    }

    private static int CalculatePriorityScore(
        string priorityType,
        double riskScore)
    {
        var score = priorityType.Trim().ToLowerInvariant() switch
        {
            "disabled" => 100,
            "policy" => 90,
            "poor" => 80,
            "far" => 70,
            "excellent" => 60,
            _ => 0
        };

        return score + (int)Math.Round(riskScore);
    }

    private static string NormalizeAssignmentNote(string? assignmentNote)
    {
        return string.IsNullOrWhiteSpace(assignmentNote)
            ? string.Empty
            : assignmentNote.Trim();
    }

    private static bool IsAssignmentOverride(
        RoomRegistration registration,
        AvailableRoomDto assignedRoom)
    {
        return !IsSameCode(registration.BuildingName, assignedRoom.BuildingName) ||
            !IsSameCode(registration.RoomType, assignedRoom.RoomType);
    }

    private static string BuildAssignmentNote(
        RoomRegistration registration,
        AvailableRoomDto assignedRoom,
        string assignmentMode)
    {
        var fitNote = IsAssignmentOverride(registration, assignedRoom)
            ? "Xếp phòng thay thế do phòng mong muốn không còn giường phù hợp."
            : "Xếp đúng nguyện vọng đăng ký.";

        return $"{assignmentMode}: {fitNote}";
    }

    private static bool IsSameCode(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
            return true;

        return string.Equals(
            first.Trim(),
            second.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildDefaultTerms(
        RoomRegistration registration,
        string assignmentNote)
    {
        return "Sinh viên sử dụng phòng đúng thời hạn, đóng tiền đúng hạn, "
            + "không tự ý chuyển phòng, tuân thủ nội quy ký túc xá và bàn giao "
            + $"phòng khi kết thúc hợp đồng ngày {registration.EndDate:dd/MM/yyyy}. "
            + $"Ghi chú xếp phòng: {assignmentNote}";
    }

}
