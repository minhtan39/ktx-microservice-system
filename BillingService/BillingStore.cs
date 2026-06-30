using System.Text.Json;

public sealed class BillingStore
{
    private readonly string _filePath;
    private readonly object _gate = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private BillingData _data;

    public BillingStore(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _filePath = configuration["BillingData:FilePath"]
            ?? Path.Combine(environment.ContentRootPath, "data", "billing-data.json");
        _data = Load();

        if (_data.Incidents.Count == 0)
        {
            _data.Incidents = SeedIncidents();
            SaveUnsafe();
        }

        if (_data.MaintenancePlans.Count == 0)
        {
            _data.MaintenancePlans = SeedMaintenancePlans();
            SaveUnsafe();
        }

        if (_data.SystemAuditLogs.Count == 0)
        {
            _data.SystemAuditLogs = SeedSystemAuditLogs();
            SaveUnsafe();
        }
    }

    public T Read<T>(Func<BillingData, T> reader)
    {
        lock (_gate)
        {
            return reader(_data);
        }
    }

    public T Write<T>(Func<BillingData, T> writer)
    {
        lock (_gate)
        {
            var result = writer(_data);
            SaveUnsafe();
            return result;
        }
    }

    private BillingData Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new BillingData();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<BillingData>(json, _jsonOptions) ?? new BillingData();
        }
        catch
        {
            return new BillingData();
        }
    }

    private void SaveUnsafe()
    {
        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = $"{_filePath}.tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(_data, _jsonOptions));
        File.Move(tempPath, _filePath, true);
    }

    private static List<MaintenanceIncident> SeedIncidents() =>
    [
        new()
        {
            Id = 1,
            StudentId = 2,
            StudentCode = "SV20260001",
            StudentName = "Nguyễn Văn A",
            RoomName = "101",
            Building = "A",
            Category = "Electric",
            Description = "Đèn học bị hỏng, cần kiểm tra lại công tắc và bóng đèn.",
            Priority = "high",
            Status = "new",
            CreatedAt = DateTime.UtcNow.AddHours(-6),
            Timeline = [new(DateTime.UtcNow.AddHours(-6), "created", "new", "SV20260001", "Sinh viên gửi yêu cầu")]
        },
        new()
        {
            Id = 2,
            StudentId = 2,
            StudentCode = "SV20260001",
            StudentName = "Nguyễn Văn A",
            RoomName = "101",
            Building = "A",
            Category = "Water",
            Description = "Vòi nước nhà vệ sinh rỉ nước liên tục.",
            Priority = "normal",
            Status = "processing",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-3),
            AssignedTo = "nhanvien",
            AssignedName = "Nhân viên ký túc xá",
            HandledBy = "nhanvien",
            StaffNote = "Đã tiếp nhận, chờ kỹ thuật xử lý.",
            Timeline = [
                new(DateTime.UtcNow.AddDays(-1), "created", "new", "SV20260001", "Sinh viên gửi yêu cầu"),
                new(DateTime.UtcNow.AddHours(-3), "status-updated", "processing", "nhanvien", "Đã tiếp nhận, chờ kỹ thuật xử lý.")
            ]
        }
    ];

    private static List<MaintenancePlan> SeedMaintenancePlans() =>
    [
        new()
        {
            Id = 1,
            Title = "Kiểm tra bình chữa cháy tầng 1",
            AssetCode = "PCCC-A-01",
            AssetName = "Cụm bình chữa cháy",
            Location = "Tòa A - Tầng 1",
            Category = "Safety",
            Frequency = "Monthly",
            NextDueDate = DateTime.UtcNow.Date.AddDays(5),
            Status = "scheduled",
            AssignedTo = "nhanvien",
            AssignedName = "Nhân viên ký túc xá",
            Checklist = ["Kiểm tra niêm phong", "Kiểm tra áp suất", "Ghi nhận hạn sử dụng"],
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        }
    ];

    private static List<SystemAuditLog> SeedSystemAuditLogs() =>
    [
        new()
        {
            Id = 1,
            CreatedAt = DateTime.UtcNow.AddHours(-5),
            ActorId = "system",
            ActorName = "Hệ thống",
            ActorRole = "System",
            Module = "BillingService",
            Action = "Khởi tạo dữ liệu",
            Status = "Success",
            TargetType = "BillingData",
            TargetId = "seed",
            TargetName = "Dữ liệu demo",
            Description = "Tạo dữ liệu mẫu cho hóa đơn, ví sinh viên, sửa chữa và bảo trì.",
            MetadataJson = "{\"source\":\"seed\"}"
        },
        new()
        {
            Id = 2,
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            ActorId = "admin",
            ActorName = "Quản trị hệ thống",
            ActorRole = "Admin",
            Module = "Sửa chữa & bảo trì",
            Action = "Theo dõi yêu cầu",
            Status = "Success",
            TargetType = "MaintenanceIncident",
            TargetId = "1",
            TargetName = "Yêu cầu sửa chữa phòng 101",
            Description = "Admin mở trung tâm vận hành sửa chữa để kiểm tra các yêu cầu đang chờ xử lý.",
            MetadataJson = "{\"priority\":\"high\"}"
        }
    ];
}
