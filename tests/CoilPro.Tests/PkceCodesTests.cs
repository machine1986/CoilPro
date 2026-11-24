using System.Security.Cryptography;
using System.Text;

namespace CoilPro.Tests;

public class PkceCodesTests
{
    [Fact]
    public void Generate_ProducesNonEmptyVerifierAndChallenge()
    {
        var pkce = PkceCodes.Generate();

        Assert.False(string.IsNullOrEmpty(pkce.Verifier));
        Assert.False(string.IsNullOrEmpty(pkce.Challenge));
    }

    [Fact]
    public void Generate_Verifier_IsBase64UrlEncoded()
    {
        var pkce = PkceCodes.Generate();

        AssertIsBase64Url(pkce.Verifier);
    }

    [Fact]
    public void Generate_Challenge_IsBase64UrlEncoded()
    {
        var pkce = PkceCodes.Generate();

        AssertIsBase64Url(pkce.Challenge);
    }

    [Fact]
    public void Generate_VerifierAndChallenge_AreDifferent()
    {
        var pkce = PkceCodes.Generate();

        Assert.NotEqual(pkce.Verifier, pkce.Challenge);
    }

    [Fact]
    public void Generate_TwoCalls_ProduceDifferentVerifiersAndChallenges()
    {
        var first = PkceCodes.Generate();
        var second = PkceCodes.Generate();

        Assert.NotEqual(first.Verifier, second.Verifier);
        Assert.NotEqual(first.Challenge, second.Challenge);
    }

    [Fact]
    public void Generate_Challenge_EqualsBase64UrlOfSha256OfVerifier()
    {
        var pkce = PkceCodes.Generate();

        var expectedChallengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(pkce.Verifier));
        var expectedChallenge = Convert.ToBase64String(expectedChallengeBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        Assert.Equal(expectedChallenge, pkce.Challenge);
    }

    private static void AssertIsBase64Url(string value)
    {
        Assert.NotEmpty(value);
        Assert.DoesNotContain('+', value);
        Assert.DoesNotContain('/', value);
        Assert.DoesNotContain('=', value);
    }
}
