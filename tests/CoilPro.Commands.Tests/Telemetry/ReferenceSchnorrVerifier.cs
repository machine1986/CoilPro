using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

namespace CoilPro.Commands.Tests.Telemetry;

/// <summary>
/// Independent port of Go <c>schnorr.Verify</c> for tests. Deliberately shares no code with the production
/// signer: own affine P-256 arithmetic over <see cref="BigInteger"/>, own challenge hash.
/// </summary>
internal static class ReferenceSchnorrVerifier
{
    public enum Result { Valid, InvalidSignature, InvalidPublicKey }

    private static readonly BigInteger P = Hex("FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFF");
    private static readonly BigInteger A = P - 3;
    private static readonly BigInteger B = Hex("5AC635D8AA3A93E7B3EBBD55769886BC651D06B0CC53B0F63BCE3C3E27D2604B");
    private static readonly BigInteger N = Hex("FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551");
    private static readonly BigInteger Gx = Hex("6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296");
    private static readonly BigInteger Gy = Hex("4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5");

    /// <summary>An affine point; <c>null</c> stands for the point at infinity.</summary>
    private readonly record struct Point(BigInteger X, BigInteger Y);

    public static Result Verify(byte[] publicKey, byte[] message, byte[] signature)
    {
        // elliptic.Unmarshal: 65 bytes, 0x04 prefix, coordinates < p, on the curve.
        if (publicKey.Length != 65 || publicKey[0] != 0x04)
        {
            return Result.InvalidPublicKey;
        }

        var pub = new Point(Int(publicKey.AsSpan(1, 32)), Int(publicKey.AsSpan(33, 32)));
        if (pub.X >= P || pub.Y >= P || !OnCurve(pub))
        {
            return Result.InvalidPublicKey;
        }

        if (signature.Length != 96)
        {
            return Result.InvalidSignature;
        }

        var vX = Int(signature.AsSpan(0, 32));
        var vY = Int(signature.AsSpan(32, 32));
        var r = Int(signature.AsSpan(64, 32));

        var publicNonce = new byte[65];
        publicNonce[0] = 0x04;
        signature.AsSpan(0, 64).CopyTo(publicNonce.AsSpan(1));
        var c = Int(Challenge(publicNonce, publicKey, message));

        // V' = c·A + r·G; valid iff V' == V. (Go's ScalarMult/ScalarBaseMult reduce nothing; neither do we.)
        var sum = Add(Multiply(pub, c), Multiply(new Point(Gx, Gy), r));
        return sum is { } s && s.X == vX && s.Y == vY ? Result.Valid : Result.InvalidSignature;
    }

    /// <summary>scalar·G as a 65-byte uncompressed point (used to derive public keys in tests).</summary>
    public static byte[] PublicKeyFor(byte[] scalar)
    {
        var p = Multiply(new Point(Gx, Gy), Int(scalar)) ?? throw new ArgumentException("scalar is zero mod n");
        var bytes = new byte[65];
        bytes[0] = 0x04;
        Write(p.X, bytes.AsSpan(1, 32));
        Write(p.Y, bytes.AsSpan(33, 32));
        return bytes;
    }

    private static byte[] Challenge(byte[] publicNonce, byte[] senderPublic, byte[] message)
    {
        var g = new byte[65];
        g[0] = 0x04;
        Write(Gx, g.AsSpan(1, 32));
        Write(Gy, g.AsSpan(33, 32));

        using var stream = new MemoryStream();
        foreach (var part in new[] { g, publicNonce, senderPublic, message })
        {
            var length = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(length, (uint)part.Length);
            stream.Write(length);
            stream.Write(part);
        }

        return SHA256.HashData(stream.ToArray());
    }

    private static bool OnCurve(Point p) => Mod(p.Y * p.Y - (p.X * p.X * p.X + A * p.X + B)) == 0;

    private static Point? Add(Point? p, Point? q)
    {
        if (p is not { } a)
        {
            return q;
        }

        if (q is not { } b)
        {
            return p;
        }

        BigInteger lambda;
        if (a.X == b.X)
        {
            if (Mod(a.Y + b.Y) == 0)
            {
                return null;
            }

            lambda = Mod(3 * a.X * a.X + A) * Inverse(2 * a.Y);
        }
        else
        {
            lambda = Mod(b.Y - a.Y) * Inverse(b.X - a.X);
        }

        lambda = Mod(lambda);
        var x = Mod(lambda * lambda - a.X - b.X);
        var y = Mod(lambda * (a.X - x) - a.Y);
        return new Point(x, y);
    }

    private static Point? Multiply(Point p, BigInteger k)
    {
        Point? result = null;
        Point? addend = p;
        while (k > 0)
        {
            if (!k.IsEven)
            {
                result = Add(result, addend);
            }

            addend = Add(addend, addend);
            k >>= 1;
        }

        return result;
    }

    private static BigInteger Inverse(BigInteger value) => BigInteger.ModPow(Mod(value), P - 2, P);

    private static BigInteger Mod(BigInteger value)
    {
        var m = value % P;
        return m.Sign < 0 ? m + P : m;
    }

    private static BigInteger Int(ReadOnlySpan<byte> bytes) => new(bytes, isUnsigned: true, isBigEndian: true);

    private static BigInteger Hex(string hex) => Int(Convert.FromHexString(hex));

    private static void Write(BigInteger value, Span<byte> destination)
    {
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        destination.Clear();
        bytes.CopyTo(destination[(destination.Length - bytes.Length)..]);
    }
}
