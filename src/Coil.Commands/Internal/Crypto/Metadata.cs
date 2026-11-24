// Port of teslamotors/vehicle-command internal/authentication/metadata.go (Apache-2.0).

using System.Buffers.Binary;
using System.Security.Cryptography;
using Signatures;

namespace Coil.Commands.Internal.Crypto;

/// <summary>
/// Injective serialization of authenticated metadata: <c>tag ‖ len ‖ value</c> per item, tags in ascending
/// order, integers as u32 big-endian, terminated by <c>0xFF</c> (<see cref="Tag.End"/>). The serialized form
/// (plus an optional trailing message) is what gets hashed or HMACed.
/// </summary>
internal sealed class Metadata
{
    private readonly MemoryStream _buffer = new();
    private int _last;

    /// <summary>Adds a <c>(tag, value)</c> item. Tags must not decrease; values are at most 255 bytes.</summary>
    /// <exception cref="InvalidOperationException">Tag out of order (a programming error).</exception>
    /// <exception cref="ArgumentException">Value longer than 255 bytes.</exception>
    public Metadata Add(Tag tag, ReadOnlySpan<byte> value)
    {
        var tagValue = (int)tag;
        if (tagValue < _last)
        {
            throw new InvalidOperationException("Metadata items need to be added in increasing tag order.");
        }

        if (value.Length > 255)
        {
            throw new ArgumentException("Metadata fields can't be more than 255 bytes long.", nameof(value));
        }

        _last = tagValue;
        _buffer.WriteByte((byte)tagValue);
        _buffer.WriteByte((byte)value.Length);
        _buffer.Write(value);
        return this;
    }

    /// <summary>Adds a single-byte item.</summary>
    public Metadata AddByte(Tag tag, byte value) => Add(tag, [value]);

    /// <summary>Adds a u32 item, encoded big-endian (even where the wire format is little-endian fixed32).</summary>
    public Metadata AddUInt32(Tag tag, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        return Add(tag, buffer);
    }

    /// <summary>The serialized metadata including the <c>0xFF</c> terminator, followed by <paramref name="message"/>.</summary>
    public byte[] Serialize(ReadOnlySpan<byte> message = default)
    {
        var length = (int)_buffer.Length;
        var result = new byte[length + 1 + message.Length];
        _buffer.GetBuffer().AsSpan(0, length).CopyTo(result);
        result[length] = (byte)Tag.End;
        message.CopyTo(result.AsSpan(length + 1));
        return result;
    }

    /// <summary><c>SHA256(metadata ‖ 0xFF ‖ message)</c> — the AES-GCM associated data.</summary>
    public byte[] Sha256Checksum(ReadOnlySpan<byte> message = default) => SHA256.HashData(Serialize(message));

    /// <summary><c>HMAC-SHA256(key, metadata ‖ 0xFF ‖ message)</c> — session-info and command tags.</summary>
    public byte[] HmacChecksum(byte[] key, ReadOnlySpan<byte> message = default) => HMACSHA256.HashData(key, Serialize(message));
}
