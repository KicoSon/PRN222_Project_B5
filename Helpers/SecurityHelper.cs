using System;
using System.Security.Cryptography;

namespace StudentPartTime.Helpers;

public static class SecurityHelper
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 10000;

    public static string HashPassword(string password)
    {
        using (var algorithm = new Rfc2898DeriveBytes(
            password,
            SaltSize,
            Iterations,
            HashAlgorithmName.SHA256))
        {
            var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
            var salt = Convert.ToBase64String(algorithm.Salt);
            return $"PBKDF2${Iterations}${salt}${key}";
        }
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword)) return false;

        if (!hashedPassword.StartsWith("PBKDF2$"))
        {
            // Fallback for seeded users with plain text passwords
            return password == hashedPassword;
        }

        var parts = hashedPassword.Split('$');
        if (parts.Length != 4) return false;

        if (!int.TryParse(parts[1], out int iterations)) return false;
        var salt = Convert.FromBase64String(parts[2]);
        var key = parts[3];

        using (var algorithm = new Rfc2898DeriveBytes(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256))
        {
            var keyToCheck = Convert.ToBase64String(algorithm.GetBytes(KeySize));
            return key == keyToCheck;
        }
    }
}
