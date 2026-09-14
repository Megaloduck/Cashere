using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Cashere.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Cashere.Data.Services;

// PBKDF2 (HMAC-SHA256) password hashing. Stored format is
// "{iterations}.{saltBase64}.{hashBase64}" - self-describing so the
// iteration count can be raised later without invalidating hashes created
// under a lower count; Verify() always re-derives using whatever count is
// embedded in the stored hash, never today's IterationCount constant.
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int IterationCount = 100_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, IterationCount, HashSizeBytes);
        return $"{IterationCount}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        byte[] salt, expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedHash = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            // Covers old plaintext hashes too (e.g. the original seeded
            // "admin") - not valid base64, so this safely returns false
            // instead of throwing.
            return false;
        }

        var actualHash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, iterations, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}