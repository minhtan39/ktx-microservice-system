using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public sealed class PasswordResetTokenStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _dataFilePath;
    private readonly object _sync = new();
    private readonly Dictionary<string, PasswordResetTokenRecord> _tokens;

    public PasswordResetTokenStore(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var configuredPath = configuration["PasswordReset:DataFile"];

        _dataFilePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "data", "password-reset-tokens.json")
            : Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath);

        _tokens = LoadTokens(_dataFilePath)
            .Where(token => token.ExpiresAtUtc > DateTimeOffset.UtcNow)
            .ToDictionary(token => token.TokenHash, StringComparer.Ordinal);
    }

    public string Create(string username, TimeSpan lifetime)
    {
        var rawToken = ToBase64Url(RandomNumberGenerator.GetBytes(32));
        var record = new PasswordResetTokenRecord(
            HashToken(rawToken),
            username,
            DateTimeOffset.UtcNow.Add(lifetime));

        lock (_sync)
        {
            RemoveExpiredUnsafe();

            foreach (var tokenHash in _tokens.Values
                .Where(token => token.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                .Select(token => token.TokenHash)
                .ToArray())
            {
                _tokens.Remove(tokenHash);
            }

            _tokens[record.TokenHash] = record;
            PersistUnsafe();
        }

        return rawToken;
    }

    public bool IsValid(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return false;

        lock (_sync)
        {
            var changed = RemoveExpiredUnsafe();
            var isValid = _tokens.ContainsKey(HashToken(rawToken));

            if (changed)
                PersistUnsafe();

            return isValid;
        }
    }

    public string? Consume(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return null;

        lock (_sync)
        {
            RemoveExpiredUnsafe();
            var tokenHash = HashToken(rawToken);

            if (!_tokens.Remove(tokenHash, out var record))
            {
                PersistUnsafe();
                return null;
            }

            PersistUnsafe();
            return record.Username;
        }
    }

    public void InvalidateForUsername(string username)
    {
        lock (_sync)
        {
            var tokenHashes = _tokens.Values
                .Where(token => token.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                .Select(token => token.TokenHash)
                .ToArray();

            foreach (var tokenHash in tokenHashes)
                _tokens.Remove(tokenHash);

            if (tokenHashes.Length > 0)
                PersistUnsafe();
        }
    }

    private bool RemoveExpiredUnsafe()
    {
        var expired = _tokens.Values
            .Where(token => token.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            .Select(token => token.TokenHash)
            .ToArray();

        foreach (var tokenHash in expired)
            _tokens.Remove(tokenHash);

        return expired.Length > 0;
    }

    private void PersistUnsafe()
    {
        var directory = Path.GetDirectoryName(_dataFilePath)
            ?? throw new InvalidOperationException("Password reset data directory is invalid.");

        Directory.CreateDirectory(directory);

        var temporaryPath = $"{_dataFilePath}.tmp";
        var json = JsonSerializer.Serialize(
            _tokens.Values.OrderBy(token => token.ExpiresAtUtc).ToArray(),
            JsonOptions);

        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, _dataFilePath, true);
    }

    private static IEnumerable<PasswordResetTokenRecord> LoadTokens(string dataFilePath)
    {
        if (!File.Exists(dataFilePath))
            return Array.Empty<PasswordResetTokenRecord>();

        try
        {
            var json = File.ReadAllText(dataFilePath);
            return JsonSerializer.Deserialize<PasswordResetTokenRecord[]>(json, JsonOptions)
                ?? Array.Empty<PasswordResetTokenRecord>();
        }
        catch (JsonException)
        {
            var backupPath = $"{dataFilePath}.invalid-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Move(dataFilePath, backupPath, true);
            return Array.Empty<PasswordResetTokenRecord>();
        }
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static string ToBase64Url(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

public sealed record PasswordResetTokenRecord(
    string TokenHash,
    string Username,
    DateTimeOffset ExpiresAtUtc);
