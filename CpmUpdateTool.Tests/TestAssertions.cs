namespace CpmUpdateTool.Tests;

internal static class TestAssertions
{
    public static void EqualIgnoringLineEndings(
        string expected,
        string actual
    )
    {
        Assert.Equal(
            expected.ReplaceLineEndings("\n"),
            actual.ReplaceLineEndings("\n")
        );
    }
}
