// Port of teslamotors/vehicle-command internal/authentication/signer.go (EncryptDetach),
// verifier.go (verifyDetached) and peer.go (marshalNestedMessage) (Apache-2.0).

namespace Coil.Commands.Internal.Crypto;

/// <summary>
/// Splits a command into the plaintext envelope (e.g. <c>Action.vehicleAction.hvacAutoAction</c>) and the
/// innermost field contents, as needed for <c>AES_GCM_DETACHED</c>: only the innermost contents are encrypted,
/// the envelope becomes the authenticated command prefix.
/// </summary>
internal static class NestedMessage
{
    private const int WireTypeBytes = 2;
    private const uint MaxFieldNumber = (1u << 29) - 1;

    /// <summary>
    /// Walks <paramref name="depth"/> levels of nested length-delimited fields. Each level must consist of
    /// exactly one length-delimited field. Returns false if <paramref name="payload"/> does not have that shape.
    /// </summary>
    /// <param name="payload">The serialized command.</param>
    /// <param name="depth">2 for Infotainment (<c>Action → service → command</c>), 1 for VCSEC.</param>
    /// <param name="prefix">The envelope with an empty innermost field, e.g. <c>12 02 52 00</c>.</param>
    /// <param name="innermost">The contents of the innermost field.</param>
    public static bool TrySplit(ReadOnlySpan<byte> payload, int depth, out byte[] prefix, out byte[] innermost)
    {
        prefix = [];
        innermost = [];
        var fieldNumbers = new uint[depth];
        for (var i = 0; i < depth; i++)
        {
            if (!TryReadBytesField(payload, out var fieldNumber, out var contents, out var consumed) || consumed != payload.Length)
            {
                return false;
            }

            fieldNumbers[i] = fieldNumber;
            payload = contents;
        }

        prefix = Marshal(fieldNumbers, []);
        innermost = payload.ToArray();
        return true;
    }

    /// <summary>
    /// Parses a command prefix (a chain of single nested length-delimited fields ending in an empty one) into its
    /// field numbers. Returns false if the prefix is empty or malformed.
    /// </summary>
    public static bool TryParsePrefix(ReadOnlySpan<byte> prefix, out uint[] fieldNumbers)
    {
        var numbers = new List<uint>();
        fieldNumbers = [];
        if (prefix.IsEmpty)
        {
            return false;
        }

        while (!prefix.IsEmpty)
        {
            if (!TryReadBytesField(prefix, out var fieldNumber, out var contents, out var consumed) || consumed != prefix.Length)
            {
                return false;
            }

            numbers.Add(fieldNumber);
            prefix = contents;
        }

        fieldNumbers = [.. numbers];
        return true;
    }

    /// <summary>
    /// Wraps <paramref name="payload"/> in nested length-delimited fields, outermost first:
    /// <c>[1, 2, 3000]</c> + "hello" → <c>1: { 2: { 3000: "hello" } }</c>.
    /// </summary>
    public static byte[] Marshal(IReadOnlyList<uint> fieldNumbers, ReadOnlySpan<byte> payload)
    {
        var current = payload.ToArray();
        for (var i = fieldNumbers.Count - 1; i >= 0; i--)
        {
            var buffer = new List<byte>(current.Length + 15);
            WriteVarint(buffer, ((ulong)fieldNumbers[i] << 3) | WireTypeBytes);
            WriteVarint(buffer, (ulong)current.Length);
            buffer.AddRange(current);
            current = [.. buffer];
        }

        return current;
    }

    private static bool TryReadBytesField(ReadOnlySpan<byte> data, out uint fieldNumber, out ReadOnlySpan<byte> contents, out int consumed)
    {
        fieldNumber = 0;
        contents = default;
        consumed = 0;

        if (!TryReadVarint(data, out var key, out var keyLength))
        {
            return false;
        }

        var number = key >> 3;
        if ((key & 7) != WireTypeBytes || number == 0 || number > MaxFieldNumber)
        {
            return false;
        }

        if (!TryReadVarint(data[keyLength..], out var length, out var lengthLength))
        {
            return false;
        }

        var available = (ulong)(data.Length - keyLength - lengthLength);
        if (length > available)
        {
            return false;
        }

        fieldNumber = (uint)number;
        contents = data.Slice(keyLength + lengthLength, (int)length);
        consumed = keyLength + lengthLength + (int)length;
        return true;
    }

    private static bool TryReadVarint(ReadOnlySpan<byte> data, out ulong value, out int length)
    {
        value = 0;
        length = 0;
        for (var i = 0; i < data.Length && i < 10; i++)
        {
            var b = data[i];
            if (i == 9 && b > 1)
            {
                return false; // overflows 64 bits
            }

            value |= (ulong)(b & 0x7F) << (7 * i);
            if ((b & 0x80) == 0)
            {
                length = i + 1;
                return true;
            }
        }

        return false;
    }

    private static void WriteVarint(List<byte> buffer, ulong value)
    {
        while (value >= 0x80)
        {
            buffer.Add((byte)(value | 0x80));
            value >>= 7;
        }

        buffer.Add((byte)value);
    }
}
