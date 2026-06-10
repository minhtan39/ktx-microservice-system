
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;

namespace ContractStudentService.Services;

public class RoomRegistrationService : IRoomRegistrationService
{
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

        registration.PriorityScore = CalculatePriorityScore(
            registration.PriorityType,
            student.RiskScore);
        registration.Status = "Pending";

        return await _repository.CreateAsync(registration);
    }

    public async Task<RoomRegistration?> UpdateAsync(
        long id,
        RoomRegistration registration)
    {
        var existing = await _repository.GetByIdAsync(id);

        if (existing == null)
            return null;

        var student = await _studentRepository.GetByIdAsync(registration.StudentId);

        if (student == null)
            throw new Exception("Student không tồn tại.");

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
        long? roomId)
    {
        var registration = await _repository.GetByIdAsync(id);

        if (registration == null)
            return null;

        if (registration.Status == "Approved")
            throw new Exception("Đơn đăng ký đã được duyệt.");

        var student = await _studentRepository.GetByIdAsync(registration.StudentId);

        if (student == null)
            throw new Exception("Student không tồn tại.");

        var assignedRoom = await _roomGatewayClient.FindAvailableRoomAsync(
            registration,
            student,
            roomId);

        if (assignedRoom == null)
            throw new Exception("Không tìm thấy phòng phù hợp hoặc phòng đã hết giường.");

        var contractCode = $"HD-{DateTime.Now:yyyyMMddHHmmss}";

        assignedRoom = await _roomGatewayClient.OccupyRoomAsync(
            assignedRoom.RoomId,
            student.Id,
            registration.Id,
            contractCode);

        registration.Status = "Approved";
        registration.AssignedRoomId = assignedRoom.RoomId;

        await _repository.UpdateAsync(registration);

        // Tự động tạo Contract
        var contract = new Contract
        {
            ContractCode = contractCode,
            StudentId = registration.StudentId,
            RoomId = assignedRoom.RoomId,
            StartDate = registration.StartDate,
            EndDate = registration.EndDate,
            DepositAmount = 500000,
            MonthlyFee = assignedRoom.MonthlyFee,
            Terms = BuildDefaultTerms(registration),
            Status = "Active"
        };

        var createdContract = await _contractRepository.CreateAsync(contract);

        await _billingGatewayClient.CreateContractBillingAsync(createdContract);

        student.Status = "Active";
        student.ResidenceHistory =
            $"Phòng {assignedRoom.RoomId}, {assignedRoom.BuildingName}, {registration.StartDate:dd/MM/yyyy} - {registration.EndDate:dd/MM/yyyy}";

        await _studentRepository.UpdateAsync(student);

        return registration;
    }

    public async Task<RoomRegistration?> RejectAsync(long id)
    {
        var registration = await _repository.GetByIdAsync(id);

        if (registration == null)
            return null;

        registration.Status = "Rejected";

        return await _repository.UpdateAsync(registration);
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

    private static string BuildDefaultTerms(RoomRegistration registration)
    {
        return "Sinh viên sử dụng phòng đúng thời hạn, đóng tiền đúng hạn, "
            + "không tự ý chuyển phòng, tuân thủ nội quy ký túc xá và bàn giao "
            + $"phòng khi kết thúc hợp đồng ngày {registration.EndDate:dd/MM/yyyy}.";
    }

}
