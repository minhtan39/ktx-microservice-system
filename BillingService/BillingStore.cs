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

}
