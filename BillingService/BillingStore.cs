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
        new(
            1,
            2,
            "SV20260001",
            "Nguyễn Văn A",
            "101",
            "A",
            "Electric",
            "Đèn học bị hỏng, cần kiểm tra lại công tắc và bóng đèn.",
            "new",
            DateTime.UtcNow.AddHours(-6),
            null,
            null,
            null),
        new(
            2,
            2,
            "SV20260001",
            "Nguyễn Văn A",
            "101",
            "A",
            "Water",
            "Vòi nước nhà vệ sinh rỉ nước liên tục.",
            "processing",
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddHours(-3),
            "nhanvien",
            "Đã tiếp nhận, chờ kỹ thuật xử lý.")
    ];
}
