// Port of teslamotors/vehicle-command internal/authentication/native.go (Apache-2.0).

using System.Security.Cryptography;
using System.Text;
using Signatures;

namespace CoilPro.Commands.Internal.Crypto;

/// <summary>
/// Symmetric keys shared by a client and one vehicle domain, derived via ECDH:
/// <c>K = SHA1(Sx)[..16]</c>, sub-keys <c>HMAC-SHA256(K, "session info")</c> and
/// <c>HMAC-SHA256(K, "authenticated command")</c>.
/// </summary>
internal sealed class SessionKeys
{
    /// <summary>Length of the AES-GCM key K.</summary>
    public const int SharedKeyLength = 16;

    /// <summary>AES-GCM nonce length required by the protocol.</summary>
    public const int NonceLength = 12;

    /// <summary>AES-GCM tag length.</summary>
    public const int GcmTagLength = 16;

    /// <summary>Length of an uncompressed P-256 point (<c>0x04 ‖ X ‖ Y</c>).</summary>
    public const int PublicKeyLength = 65;

    private static readonly byte[] LabelSessionInfo = Encoding.ASCII.GetBytes("session info");
    private static readonly byte[] LabelMessageAuth = Encoding.ASCII.GetBytes("authenticated command");

    private SessionKeys(byte[] sharedKey)
    {
        SharedKey = sharedKey;
        SessionInfoKey = HMACSHA256.HashData(sharedKey, LabelSessionInfo);
        CommandKey = HMACSHA256.HashData(sharedKey, LabelMessageAuth);
    }

    /// <summary>K: the 128-bit AES-GCM key.</summary>
    public byte[] SharedKey { get; }

    /// <summary><c>HMAC-SHA256(K, "session info")</c>: authenticates session info.</summary>
    public byte[] SessionInfoKey { get; }

    /// <summary><c>HMAC-SHA256(K, "authenticated command")</c>: key for <c>HMAC_PERSONALIZED</c> command tags.</summary>
    public byte[] CommandKey { get; }

    /// <summary>Builds the keys from a known K (tests and vehicle-side doubles).</summary>
    public static SessionKeys FromSharedKey(byte[] sharedKey)
    {
        if (sharedKey.Length != SharedKeyLength)
        {
            throw new ArgumentException($"K must be {SharedKeyLength} bytes.", nameof(sharedKey));
        }

        return new SessionKeys((byte[])sharedKey.Clone());
    }

    /// <summary>Derives the keys from our private key and the peer's 65-byte uncompressed public point.</summary>
    /// <exception cref="CryptographicException">The peer key is not a valid P-256 point.</exception>
    public static SessionKeys Derive(ECDiffieHellman local, ReadOnlySpan<byte> peerPublicKey)
    {
        var sx = DeriveSharedSecret(local, peerPublicKey);
        try
        {
            // SHA1 keeps compatibility with the vehicle firmware; only used to map a curve point to a key.
            return new SessionKeys(SHA1.HashData(sx)[..SharedKeyLength]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(sx);
        }
    }

    /// <summary><c>Sx</c>: the 32-byte big-endian x coordinate of the ECDH shared point (leading zeros kept).</summary>
    /// <exception cref="CryptographicException">The peer key is not a valid P-256 point.</exception>
    public static byte[] DeriveSharedSecret(ECDiffieHellman local, ReadOnlySpan<byte> peerPublicKey)
    {
        using var peer = ImportPublicKey(peerPublicKey);
        using var peerPublic = peer.PublicKey;
        var sx = local.DeriveRawSecretAgreement(peerPublic);
        if (sx.Length != 32)
        {
            throw new CryptographicException($"Unexpected ECDH shared secret length {sx.Length}.");
        }

        return sx;
    }

    /// <summary>Imports a 65-byte uncompressed P-256 point as a public-only key (validated by the platform).</summary>
    /// <exception cref="CryptographicException">Wrong length/format, or not a point on the curve.</exception>
    public static ECDiffieHellman ImportPublicKey(ReadOnlySpan<byte> point)
    {
        if (point.Length != PublicKeyLength || point[0] != 0x04)
        {
            throw new CryptographicException("Public key must be a 65-byte uncompressed P-256 point.");
        }

        var parameters = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = point[1..33].ToArray(), Y = point[33..].ToArray() },
        };
        parameters.Validate();
        return ECDiffieHellman.Create(parameters);
    }

    /// <summary>Exports the public half of <paramref name="key"/> as a 65-byte uncompressed point.</summary>
    public static byte[] ExportPublicKey(ECDiffieHellman key)
    {
        var q = key.ExportParameters(includePrivateParameters: false).Q;
        var point = new byte[PublicKeyLength];
        point[0] = 0x04;
        q.X!.CopyTo(point, 1);
        q.Y!.CopyTo(point, 33);
        return point;
    }

    /// <summary>
    /// Session-info tag: <c>HMAC(SessionInfoKey, TLV(0,[HMAC]) ‖ TLV(2,VIN) ‖ TLV(6,challenge) ‖ 0xFF ‖ encodedInfo)</c>.
    /// The challenge item is omitted when <paramref name="challenge"/> is empty.
    /// </summary>
    public byte[] SessionInfoTag(byte[] vin, ReadOnlySpan<byte> challenge, ReadOnlySpan<byte> encodedInfo)
    {
        var meta = new Metadata()
            .AddByte(Tag.SignatureType, (byte)SignatureType.Hmac)
            .Add(Tag.Personalization, vin);
        if (!challenge.IsEmpty)
        {
            meta.Add(Tag.Challenge, challenge);
        }

        return meta.HmacChecksum(SessionInfoKey, encodedInfo);
    }

    /// <summary>AES-GCM-128 encryption with K; returns ciphertext and the detached 16-byte tag.</summary>
    public (byte[] Ciphertext, byte[] Tag) Encrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData)
    {
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[GcmTagLength];
        using var gcm = new AesGcm(SharedKey, GcmTagLength);
        gcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
        return (ciphertext, tag);
    }

    /// <summary>AES-GCM-128 decryption with K.</summary>
    /// <exception cref="CryptographicException">Authentication failed or the nonce/tag has the wrong length.</exception>
    public byte[] Decrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> associatedData)
    {
        if (nonce.Length != NonceLength)
        {
            throw new CryptographicException($"AES-GCM nonce must be {NonceLength} bytes.");
        }

        if (tag.Length != GcmTagLength)
        {
            throw new CryptographicException($"AES-GCM tag must be {GcmTagLength} bytes.");
        }

        var plaintext = new byte[ciphertext.Length];
        using var gcm = new AesGcm(SharedKey, GcmTagLength);
        gcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        return plaintext;
    }
}
