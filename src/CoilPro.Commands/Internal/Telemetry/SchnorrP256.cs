using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

namespace CoilPro.Commands.Internal.Telemetry;

/// <summary>
/// Tesla's Schnorr signature over NIST P-256 / SHA-256 — a port of <c>internal/schnorr</c> from
/// <c>teslamotors/vehicle-command</c> (<c>Sign</c>, <c>DeterministicNonce</c>, <c>challenge</c>).
/// </summary>
/// <remarks>
/// <para>Scheme (RFC 8235 with an RFC 6979 nonce), for private scalar <c>a</c> and public key <c>A = aG</c>:</para>
/// <list type="number">
///   <item><description><c>v = DeterministicNonce(a, SHA-256(message))</c> (RFC 6979 §3.2, HMAC-SHA-256, qlen = hlen = 256).</description></item>
///   <item><description><c>V = vG</c>, encoded uncompressed (<c>0x04 || X || Y</c>).</description></item>
///   <item><description><c>c = SHA-256(LV(G) || LV(V) || LV(A) || LV(message))</c> where <c>LV(x)</c> = 4-byte big-endian length || x and points are 65-byte uncompressed.</description></item>
///   <item><description><c>r = (v - a·c) mod n</c>.</description></item>
///   <item><description>Signature = <c>V.X || V.Y || r</c>, 3 × 32 bytes big-endian (no 0x04 prefix).</description></item>
/// </list>
/// <para>
/// The point multiplication <c>vG</c> is done by the platform (importing <c>v</c> as an EC private key and
/// exporting its public point), so the secret-dependent curve arithmetic stays in the OS crypto library.
/// The remaining scalar arithmetic uses <see cref="BigInteger"/>, which — unlike Go's <c>saferith</c> — is
/// not constant-time.
/// </para>
/// </remarks>
internal static class SchnorrP256
{
    /// <summary>Length of a P-256 scalar / coordinate in bytes.</summary>
    public const int ScalarLength = 32;

    /// <summary>Length of a signature: <c>V.X || V.Y || r</c>.</summary>
    public const int SignatureLength = 3 * ScalarLength;

    /// <summary>Length of an uncompressed P-256 point (<c>0x04 || X || Y</c>).</summary>
    public const int PointLength = 1 + 2 * ScalarLength;

    /// <summary>Order <c>n</c> of the P-256 base point.</summary>
    public static readonly BigInteger Order = FromBytes(Convert.FromHexString(
        "FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551"));

    /// <summary>The P-256 base point <c>G</c>, uncompressed — the first challenge input in Go (<c>elliptic.Marshal(p256, Gx, Gy)</c>).</summary>
    public static ReadOnlySpan<byte> BasePoint => Convert.FromHexString(
        "04" +
        "6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296" +
        "4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5");

    /// <summary>
    /// Signs <paramref name="message"/> (Go <c>schnorr.Sign</c>). Deterministic: the same key and message
    /// always produce the same signature.
    /// </summary>
    /// <param name="privateScalar">The private scalar <c>a</c>, 32 bytes big-endian.</param>
    /// <param name="publicKey">The matching public key <c>A</c>, 65-byte uncompressed point.</param>
    /// <param name="message">The bytes to sign (for a JWS: the ASCII signing input <c>header.payload</c>).</param>
    /// <returns>The 96-byte signature.</returns>
    public static byte[] Sign(ReadOnlySpan<byte> privateScalar, ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> message)
    {
        if (privateScalar.Length != ScalarLength)
        {
            throw new ArgumentException($"Private scalar must be {ScalarLength} bytes.", nameof(privateScalar));
        }

        if (publicKey.Length != PointLength || publicKey[0] != 0x04)
        {
            throw new ArgumentException($"Public key must be a {PointLength}-byte uncompressed point.", nameof(publicKey));
        }

        var a = FromBytes(privateScalar);
        if (a.IsZero || a >= Order)
        {
            throw new ArgumentException("Private scalar is out of range.", nameof(privateScalar));
        }

        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(message, digest);

        var nonce = DeterministicNonce(privateScalar, digest);
        try
        {
            var publicNonce = MultiplyBase(nonce);
            var c = FromBytes(Challenge(publicNonce, publicKey, message));
            var v = FromBytes(nonce);

            // r = v - a·c (mod n), normalised to [0, n).
            var r = (v - a * c % Order) % Order;
            if (r.Sign < 0)
            {
                r += Order;
            }

            var signature = new byte[SignatureLength];
            publicNonce.AsSpan(1).CopyTo(signature);
            WriteScalar(r, signature.AsSpan(2 * ScalarLength));
            return signature;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(nonce);
        }
    }

    /// <summary>
    /// The Schnorr challenge hash (Go <c>challenge</c>):
    /// <c>SHA-256(LV(G) || LV(publicNonce) || LV(senderPublic) || LV(message))</c>.
    /// </summary>
    public static byte[] Challenge(ReadOnlySpan<byte> publicNonce, ReadOnlySpan<byte> senderPublic, ReadOnlySpan<byte> message)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendLengthValue(hash, BasePoint);
        AppendLengthValue(hash, publicNonce);
        AppendLengthValue(hash, senderPublic);
        AppendLengthValue(hash, message);
        return hash.GetHashAndReset();
    }

    /// <summary>
    /// RFC 6979 deterministic nonce for P-256 / SHA-256 only (Go <c>DeterministicNonce</c>). Returns the
    /// 32-byte nonce <c>v</c> with <c>1 ≤ v &lt; n</c>.
    /// </summary>
    /// <param name="privateScalar">The private scalar, 32 bytes big-endian (RFC 6979 <c>int2octets(x)</c>).</param>
    /// <param name="messageHash">SHA-256 of the message (step a, done by the caller).</param>
    public static byte[] DeterministicNonce(ReadOnlySpan<byte> privateScalar, ReadOnlySpan<byte> messageHash)
    {
        if (messageHash.Length != SHA256.HashSizeInBytes)
        {
            throw new ArgumentException("Message hash must be a SHA-256 digest.", nameof(messageHash));
        }

        // h1 = bits2octets(H(m)) = (H(m) mod n), as 32 bytes. hlen = qlen, so no truncation is needed.
        var h1 = new byte[ScalarLength];
        WriteScalar(FromBytes(messageHash) % Order, h1);

        var v = new byte[SHA256.HashSizeInBytes];
        var k = new byte[SHA256.HashSizeInBytes];
        v.AsSpan().Fill(0x01); // step (b)
        // step (c): K = 0x00 ... 0x00 (already zero)

        try
        {
            k = Hmac(k, v, [0x00], privateScalar, h1); // step (d)
            v = Hmac(k, v);                             // step (e)
            k = Hmac(k, v, [0x01], privateScalar, h1); // step (f)
            v = Hmac(k, v);                             // step (g)

            // step (h): with hlen = qlen, T = V after one HMAC round.
            while (true)
            {
                v = Hmac(k, v);
                var candidate = FromBytes(v);
                if (!candidate.IsZero && candidate < Order)
                {
                    return v;
                }

                k = Hmac(k, v, [0x00]);
                v = Hmac(k, v);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(k);
        }
    }

    /// <summary>Computes <c>scalar·G</c> as an uncompressed point, using the platform's EC implementation.</summary>
    private static byte[] MultiplyBase(byte[] scalar)
    {
        using var key = ECDsa.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, D = scalar });
        var parameters = key.ExportParameters(includePrivateParameters: false);
        var point = new byte[PointLength];
        point[0] = 0x04;
        PadLeft(parameters.Q.X!, point.AsSpan(1, ScalarLength));
        PadLeft(parameters.Q.Y!, point.AsSpan(1 + ScalarLength, ScalarLength));
        return point;
    }

    private static byte[] Hmac(byte[] key, params ReadOnlySpan<byte[]> parts)
    {
        using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key);
        foreach (var part in parts)
        {
            hmac.AppendData(part);
        }

        return hmac.GetHashAndReset();
    }

    private static byte[] Hmac(byte[] key, byte[] v, byte[] separator, ReadOnlySpan<byte> scalar, byte[] h1)
    {
        using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key);
        hmac.AppendData(v);
        hmac.AppendData(separator);
        hmac.AppendData(scalar);
        hmac.AppendData(h1);
        return hmac.GetHashAndReset();
    }

    private static void AppendLengthValue(IncrementalHash hash, ReadOnlySpan<byte> value)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)value.Length);
        hash.AppendData(length);
        hash.AppendData(value);
    }

    /// <summary>Parses big-endian unsigned bytes.</summary>
    internal static BigInteger FromBytes(ReadOnlySpan<byte> bytes) => new(bytes, isUnsigned: true, isBigEndian: true);

    /// <summary>Writes a non-negative value as a fixed-width big-endian scalar (Go <c>FillBytes</c>).</summary>
    internal static void WriteScalar(BigInteger value, Span<byte> destination)
    {
        destination.Clear();
        var length = value.GetByteCount(isUnsigned: true);
        if (length > destination.Length || !value.TryWriteBytes(destination[(destination.Length - length)..], out _, isUnsigned: true, isBigEndian: true))
        {
            throw new CryptographicException("Scalar does not fit.");
        }
    }

    private static void PadLeft(byte[] source, Span<byte> destination)
    {
        if (source.Length > destination.Length)
        {
            throw new CryptographicException("Coordinate is too long.");
        }

        destination.Clear();
        source.CopyTo(destination[(destination.Length - source.Length)..]);
    }
}
