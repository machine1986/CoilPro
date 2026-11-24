using System.Security.Cryptography;

namespace CoilPro.Commands.Tests.Protocol.TestSupport;

/// <summary>
/// Published test keys and values from teslamotors/vehicle-command <c>pkg/protocol/protocol.md</c> (Apache-2.0).
/// Never enroll these keys on a real vehicle — the private keys are public.
/// </summary>
internal static class TestKeys
{
    public const string Vin = "5YJ30123456789ABC";

    /// <summary>Client private scalar <c>c</c>.</summary>
    public const string ClientScalar = "2538CDC29A97C19C1E99A637D6CF4F8C970C118B56EDE1E6323E6D162C4B30DB";

    public const string ClientPublic =
        "04b2b6bc68c2da0665ce656815594996c62394edd8bea905fe781a754fe6a845a714330902f225e9269d466e05b349981fda9d85cc23c6fb444aa73b629105dc6e";

    /// <summary>Vehicle private scalar <c>v</c>.</summary>
    public const string VehicleScalar = "344EE5B466A7CF1EEB12B6F50331DB2E5EC5834EF5F4BEFCFD8CBE55C2528D70";

    public const string VehiclePublic =
        "04c7a1f47138486aa4729971494878d33b1a24e39571f748a6e16c5955b3d877d3a6aaa0e955166474af5d32c410f439a2234137ad1bb085fd4e8813c958f11d97";

    /// <summary><c>client.key</c> from protocol.md (SEC 1 PEM).</summary>
    public const string ClientPem = """
        -----BEGIN EC PRIVATE KEY-----
        MHcCAQEEICU4zcKal8GcHpmmN9bPT4yXDBGLVu3h5jI+bRYsSzDboAoGCCqGSM49
        AwEHoUQDQgAEsra8aMLaBmXOZWgVWUmWxiOU7di+qQX+eBp1T+aoRacUMwkC8iXp
        Jp1GbgWzSZgf2p2FzCPG+0RKpztikQXcbg==
        -----END EC PRIVATE KEY-----
        """;

    public const string Sx = "6b9a2844b300e6a85e4474f70b31b23de2d8f7d3e594784cdfe8318031442f8b";
    public const string K = "1b2fce19967b79db696f909cff89ea9a";
    public const string SessionInfoKey = "fceb679ee7bca756fcd441bf238bf2f338629b41d9eb9c67be1b32c9672ce300";
    public const string CommandKey = "6d3a14d0b6d762e4f076739e2cf6edd291d2e56e3f25bc6a2af5cb26dc753b14";

    /// <summary>Handshake example: request routing address and uuid (= challenge).</summary>
    public const string RoutingAddress = "2c907bd76c640d360b3027dc7404efde";
    public const string Challenge = "1588d5a30eabc6f8fc9a951b11f6fd11";

    /// <summary>Handshake example reply: counter 6, vehicle public key, epoch, clock_time 2650.</summary>
    public const string EncodedSessionInfo =
        "0806124104c7a1f47138486aa4729971494878d33b1a24e39571f748a6e16c5955b3d877d3a6aaa0e955166474af5d32c410f439a2234137ad1bb085fd4e8813c958f11d971a104c463f9cc0d3d26906e982ed224adde6255a0a0000";

    public const string SessionInfoTag = "996c1fe38331be138f8039c194b14db2198846ed7d8251e6749284d7b32ea002";
    public const string Epoch = "4c463f9cc0d3d26906e982ed224adde6";
    public const uint SessionCounter = 6;
    public const uint ClockTime = 2650;

    public static TeslaKeyPair ClientKeyPair() => TeslaKeyPair.FromPrivateKeyPem(ClientPem);

    public static ECDiffieHellman EcdhFromScalar(string scalarHex)
    {
        var key = ECDiffieHellman.Create();
        key.ImportParameters(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, D = Convert.FromHexString(scalarHex) });
        return key;
    }

    public static byte[] H(string hex) => Convert.FromHexString(hex.Replace(" ", string.Empty, StringComparison.Ordinal));

    public static string X(ReadOnlySpan<byte> bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
}
