// Port of teslamotors/vehicle-command internal/authentication/peer.go (Apache-2.0).

using CoilPro.Commands.Internal.Crypto;
using Signatures;
using UniversalMessage;

namespace CoilPro.Commands.Internal.Protocol;

/// <summary>Metadata layouts shared by the signer (<see cref="VehicleProtocol"/>) and vehicle-side test doubles.</summary>
internal static class MessageAuthentication
{
    /// <summary>
    /// Request metadata: signature type, domain, VIN, epoch, expires_at, counter, and flags (only if non-zero).
    /// <c>AES_GCM_DETACHED</c> callers append <see cref="Tag.CommandPrefix"/> afterwards.
    /// </summary>
    public static Metadata CommandMetadata(
        SignatureType signatureType,
        Domain domain,
        byte[] vin,
        ReadOnlySpan<byte> epoch,
        uint expiresAt,
        uint counter,
        uint flags)
    {
        var meta = new Metadata()
            .AddByte(Tag.SignatureType, (byte)signatureType)
            .AddByte(Tag.Domain, (byte)domain)
            .Add(Tag.Personalization, vin)
            .Add(Tag.Epoch, epoch)
            .AddUInt32(Tag.ExpiresAt, expiresAt)
            .AddUInt32(Tag.Counter, counter);

        // For backwards compatibility, flags are only authenticated if at least one is set.
        if (flags > 0)
        {
            meta.AddUInt32(Tag.Flags, flags);
        }

        return meta;
    }

    /// <summary>
    /// Associated data of an encrypted response:
    /// <c>SHA256(TLV(0,[AES_GCM_RESPONSE]) ‖ TLV(1,from-domain) ‖ TLV(2,VIN) ‖ TLV(5,counter) ‖ TLV(7,flags) ‖ TLV(8,request hash) ‖ TLV(9,fault) ‖ 0xFF)</c>.
    /// </summary>
    public static byte[] ResponseAssociatedData(
        Domain fromDomain,
        byte[] vin,
        uint counter,
        uint flags,
        ReadOnlySpan<byte> requestHash,
        MessageFault_E fault) =>
        new Metadata()
            .AddByte(Tag.SignatureType, (byte)SignatureType.AesGcmResponse)
            .AddByte(Tag.Domain, (byte)fromDomain)
            .Add(Tag.Personalization, vin)
            .AddUInt32(Tag.Counter, counter)
            .AddUInt32(Tag.Flags, flags)
            .Add(Tag.RequestHash, requestHash)
            .AddUInt32(Tag.Fault, (uint)fault)
            .Sha256Checksum();

    /// <summary>
    /// The request hash a response is bound to: <c>[signature type] ‖ tag</c>; for VCSEC <c>HMAC_PERSONALIZED</c>
    /// the tag is truncated to 16 bytes (17 bytes total). Empty if the message carries no command signature.
    /// </summary>
    public static byte[] RequestHash(RoutableMessage message)
    {
        var signature = message.SignatureData;
        if (signature is null)
        {
            return [];
        }

        switch (signature.SigTypeCase)
        {
            case SignatureData.SigTypeOneofCase.AESGCMPersonalizedData:
                return Prepend(SignatureType.AesGcmPersonalized, signature.AESGCMPersonalizedData.Tag.Span);
            case SignatureData.SigTypeOneofCase.HMACPersonalizedData:
                var tag = signature.HMACPersonalizedData.Tag.Span;
                if (message.ToDestination?.SubDestinationCase == Destination.SubDestinationOneofCase.Domain &&
                    message.ToDestination.Domain == Domain.VehicleSecurity &&
                    tag.Length > 16)
                {
                    tag = tag[..16];
                }

                return Prepend(SignatureType.HmacPersonalized, tag);
            case SignatureData.SigTypeOneofCase.AESGCMDetachedData:
                return Prepend(SignatureType.AesGcmDetached, signature.AESGCMDetachedData.Tag.Span);
            default:
                return [];
        }
    }

    private static byte[] Prepend(SignatureType type, ReadOnlySpan<byte> tag)
    {
        var result = new byte[1 + tag.Length];
        result[0] = (byte)type;
        tag.CopyTo(result.AsSpan(1));
        return result;
    }
}
