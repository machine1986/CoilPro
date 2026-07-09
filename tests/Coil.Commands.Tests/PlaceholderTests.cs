namespace Coil.Commands.Tests;

/// <summary>
/// Placeholder so the project is not empty. The real Vehicle Command Protocol tests
/// (session handshake, signing, fixtures) arrive with milestone M3.
/// </summary>
public class PlaceholderTests
{
    [Fact]
    public void CoilCommands_Package_IsReferenced()
    {
        // Touches the Coil.Commands assembly to keep the project reference meaningful.
        Assert.False(string.IsNullOrEmpty(Coil.Commands.CoilCommandsInfo.Status));
    }
}
