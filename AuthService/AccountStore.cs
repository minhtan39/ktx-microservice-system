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
        var loadResult = LoadAccounts(_dataFilePath);
        var migrationRequired = loadResult.MigrationRequired;

        foreach (var user in loadResult.Accounts)
        {
            var passwordHash = user.Password;

            if (!PasswordHasher.IsHash(passwordHash) && !string.IsNullOrWhiteSpace(passwordHash))
            {
                passwordHash = PasswordHasher.Hash(passwordHash);
                migrationRequired = true;
            }

            var normalized = user with
            {
                Password = passwordHash,
                AccountStatus = string.IsNullOrWhiteSpace(user.AccountStatus)
                    ? "Active"
                    : user.AccountStatus,
                Permissions = user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase) &&
                    (user.Permissions == null || user.Permissions.Length == 0)
                        ? new[]
                        {
                            "view_students",
                            "approve_registrations",
                            "manage_contracts",
                            "view_rooms",
                            "manage_incidents",
                            "manage_maintenance",
                            "issue_billing",
                            "confirm_payments"
                        }
                        : user.Permissions
            };

            loadedAccounts[normalized.Username] = normalized;
        }

        Users = new ConcurrentDictionary<string, DemoUser>(
            loadedAccounts,
            StringComparer.OrdinalIgnoreCase);

        if (migrationRequired)
            Persist();
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

    private static LoadedAccounts LoadAccounts(string dataFilePath)
    {
        if (!File.Exists(dataFilePath))
            return new LoadedAccounts(Array.Empty<DemoUser>(), false);

        try
        {
            var json = File.ReadAllText(dataFilePath);
            var storedAccounts = JsonSerializer.Deserialize<StoredDemoUser[]>(json, JsonOptions)
                ?? Array.Empty<StoredDemoUser>();
            var migrationRequired = false;
            var accounts = new List<DemoUser>();

            foreach (var stored in storedAccounts)
            {
                if (string.IsNullOrWhiteSpace(stored.Username) ||
                    string.IsNullOrWhiteSpace(stored.Role))
                {
                    migrationRequired = true;
                    continue;
                }

                var username = stored.Username.Trim();
                var role = stored.Role.Trim();
                var studentCode = stored.StudentCode?.Trim();
                var accountStatus = string.IsNullOrWhiteSpace(stored.AccountStatus)
                    ? "Active"
                    : stored.AccountStatus;
                var rawPassword = !string.IsNullOrWhiteSpace(stored.PasswordHash)
                    ? stored.PasswordHash
                    : stored.Password;
                var passwordChangedAt = stored.PasswordChangedAt;
                var isDefaultStudentPassword = role.Equals("Student", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(stored.Password) &&
                    (stored.Password.Equals(username, StringComparison.OrdinalIgnoreCase) ||
                     (!string.IsNullOrWhiteSpace(studentCode) &&
                      stored.Password.Equals(studentCode, StringComparison.OrdinalIgnoreCase)));

                if (isDefaultStudentPassword)
                {
                    rawPassword = string.Empty;
                    accountStatus = "Pending";
                    passwordChangedAt = null;
                    migrationRequired = true;
                }

                if (!string.IsNullOrWhiteSpace(stored.Password))
                {
                    migrationRequired = true;

                    if (!isDefaultStudentPassword)
                        passwordChangedAt ??= DateTimeOffset.UtcNow;
                }

                var passwordHash = string.IsNullOrWhiteSpace(rawPassword)
                    ? PasswordHasher.HashTemporaryPassword()
                    : rawPassword;

                if (!PasswordHasher.IsHash(passwordHash))
                {
                    passwordHash = PasswordHasher.Hash(passwordHash);
                    migrationRequired = true;
                    passwordChangedAt ??= DateTimeOffset.UtcNow;
                }

                accounts.Add(new DemoUser(
                    username,
                    passwordHash,
                    role,
                    string.IsNullOrWhiteSpace(stored.FullName) ? username : stored.FullName.Trim(),
                    stored.StudentId,
                    studentCode,
                    stored.EmployeeCode,
                    stored.Email,
                    stored.Phone,
                    stored.Department,
                    stored.JobTitle,
                    stored.AssignedArea,
                    accountStatus,
                    stored.Permissions,
                    passwordChangedAt,
                    stored.LastLoginAt,
                    stored.FailedLoginCount,
                    stored.LockoutUntil,
                    stored.MustChangePassword));
            }

            return new LoadedAccounts(accounts, migrationRequired);
        }
        catch (JsonException)
        {
            var backupPath = $"{dataFilePath}.invalid-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Move(dataFilePath, backupPath, true);
            return new LoadedAccounts(Array.Empty<DemoUser>(), false);
        }
    }

    private sealed record LoadedAccounts(
        IReadOnlyCollection<DemoUser> Accounts,
        bool MigrationRequired);

    private sealed record StoredDemoUser
    {
        public string Username { get; init; } = string.Empty;
        public string? Password { get; init; }
        public string? PasswordHash { get; init; }
        public string Role { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public long? StudentId { get; init; }
        public string? StudentCode { get; init; }
        public string? EmployeeCode { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? Department { get; init; }
        public string? JobTitle { get; init; }
        public string? AssignedArea { get; init; }
        public string AccountStatus { get; init; } = "Active";
        public string[]? Permissions { get; init; }
        public DateTimeOffset? PasswordChangedAt { get; init; }
        public DateTimeOffset? LastLoginAt { get; init; }
        public int FailedLoginCount { get; init; }
        public DateTimeOffset? LockoutUntil { get; init; }
        public bool MustChangePassword { get; init; }
    }
}
