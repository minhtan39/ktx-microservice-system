using System.Collections.Concurrent;
using System.Text.Json;

public sealed class AccountStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _dataFilePath;
    private readonly object _writeLock = new();

    public AccountStore(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredPath = configuration["Accounts:DataFile"];

        _dataFilePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "data", "accounts.json")
            : Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath);

        var loadedAccounts = new Dictionary<string, DemoUser>(StringComparer.OrdinalIgnoreCase);

        foreach (var user in LoadAccounts(_dataFilePath))
        {
            loadedAccounts[user.Username] = user;
        }

        Users = new ConcurrentDictionary<string, DemoUser>(
            loadedAccounts,
            StringComparer.OrdinalIgnoreCase);
    }

    public ConcurrentDictionary<string, DemoUser> Users { get; }

    public void Persist()
    {
        lock (_writeLock)
        {
            PersistUnsafe();
        }
    }

    public void Upsert(DemoUser user)
    {
        lock (_writeLock)
        {
            Users[user.Username] = user;
            PersistUnsafe();
        }
    }

    public void Replace(string currentUsername, DemoUser updated)
    {
        lock (_writeLock)
        {
            if (!updated.Username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                Users.TryRemove(currentUsername, out _);
            }

            Users[updated.Username] = updated;
            PersistUnsafe();
        }
    }

    public bool Remove(string username)
    {
        lock (_writeLock)
        {
            if (!Users.TryRemove(username, out _))
                return false;

            PersistUnsafe();
            return true;
        }
    }

    public void AddMissingStudents(IEnumerable<DemoUser> students)
    {
        lock (_writeLock)
        {
            var changed = false;

            foreach (var student in students)
            {
                var alreadyExists = Users.Values.Any(user =>
                    user.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) &&
                    ((student.StudentId.HasValue && user.StudentId == student.StudentId) ||
                     (!string.IsNullOrWhiteSpace(student.StudentCode) &&
                      user.StudentCode?.Equals(
                          student.StudentCode,
                          StringComparison.OrdinalIgnoreCase) == true)));

                if (alreadyExists || Users.ContainsKey(student.Username))
                    continue;

                Users[student.Username] = student;
                changed = true;
            }

            if (changed)
                PersistUnsafe();
        }
    }

    private void PersistUnsafe()
    {
        var directory = Path.GetDirectoryName(_dataFilePath)
            ?? throw new InvalidOperationException("Account data directory is invalid.");

        Directory.CreateDirectory(directory);

        var accounts = Users.Values
            .OrderBy(user => user.Role)
            .ThenBy(user => user.Username)
            .ToArray();

        var temporaryPath = $"{_dataFilePath}.tmp";
        var json = JsonSerializer.Serialize(accounts, JsonOptions);

        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, _dataFilePath, true);
    }

    private static IEnumerable<DemoUser> LoadAccounts(string dataFilePath)
    {
        if (!File.Exists(dataFilePath))
            return Array.Empty<DemoUser>();

        try
        {
            var json = File.ReadAllText(dataFilePath);
            return JsonSerializer.Deserialize<DemoUser[]>(json, JsonOptions)
                ?? Array.Empty<DemoUser>();
        }
        catch (JsonException)
        {
            var backupPath = $"{dataFilePath}.invalid-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Move(dataFilePath, backupPath, true);
            return Array.Empty<DemoUser>();
        }
    }
}
