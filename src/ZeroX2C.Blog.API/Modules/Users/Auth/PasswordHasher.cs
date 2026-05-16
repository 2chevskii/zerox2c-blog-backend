using System.Buffers.Binary;
using System.Security.Cryptography;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    private const byte FormatVersion = 1;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 210_000;
    private const int HeaderSize = 7;

    public byte[] HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        var payload = new byte[HeaderSize + salt.Length + hash.Length];
        payload[0] = FormatVersion;
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(1, 4), Iterations);
        payload[5] = checked((byte)salt.Length);
        payload[6] = checked((byte)hash.Length);
        salt.CopyTo(payload.AsSpan(HeaderSize));
        hash.CopyTo(payload.AsSpan(HeaderSize + salt.Length));

        return payload;
    }

    public bool VerifyPassword(string password, byte[] passwordHash)
    {
        if (passwordHash.Length < HeaderSize || passwordHash[0] != FormatVersion)
        {
            return false;
        }

        var iterations = BinaryPrimitives.ReadInt32BigEndian(passwordHash.AsSpan(1, 4));
        var saltLength = passwordHash[5];
        var hashLength = passwordHash[6];
        var expectedLength = HeaderSize + saltLength + hashLength;

        if (iterations <= 0 || passwordHash.Length != expectedLength)
        {
            return false;
        }

        var salt = passwordHash.AsSpan(HeaderSize, saltLength);
        var expectedHash = passwordHash.AsSpan(HeaderSize + saltLength, hashLength);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            hashLength
        );

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
