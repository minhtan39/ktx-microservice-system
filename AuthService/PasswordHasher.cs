using System.Security.Cryptography;

public static class PasswordHasher
{
    private const string Prefix = "$ktx-pbkdf2-sha256$v1$";
    private const int DefaultIterations = 120_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return string.Join(
            '$',
            Prefix.TrimEnd('$'),
            DefaultIterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public static string HashTemporaryPassword()
    {
        return Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }

    public static bool IsHash(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            value.StartsWith(Prefix, StringComparison.Ordinal);
    }

    public static bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        if (!IsHash(storedHash))
            return string.Equals(storedHash, password, StringComparison.Ordinal);

        var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 5 ||
            !int.TryParse(parts[2], out var iterations) ||
            iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[3]);
            var expectedHash = Convert.FromBase64String(parts[4]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
