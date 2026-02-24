namespace CpmUpdateTool.Tests;

public class DefaultCmdRunnerTests
{
    private const string path = "./Directory.Packages.props";
    private const string cwd = "./";

    private static CancellationToken Token =>
        TestContext.Current.CancellationToken;

    [Fact]
    public async Task UpdatePackageAsync_ShouldModifyVersionInPropsFile()
    {
        const string original =
            "<Project>\n"
            + "  <ItemGroup>\n"
            + "    <PackageVersion Include=\"Foo\" Version=\"1.0.0\" />\n"
            + "    <PackageVersion Include=\"Bar\" Version=\"2.0.0\" />\n"
            + "  </ItemGroup>\n"
            + "</Project>\n";
        var fs = new FakeFileSystem();
        fs.AddFile(path, original);

        var console = new FakeConsole();
        var runner = new DefaultCmdRunner(console, fs, cwd);

        var exit = await runner.UpdatePackageAsync("Foo", "1.1.1", Token);
        Assert.Equal(0, exit);

        var updated = fs[path];
        Assert.Equal(original.Replace("1.0.0", "1.1.1"), updated);
    }

    [Fact]
    public async Task UpdatePackageAsync_ShouldReturnNonZero_WhenPackageNotFound()
    {
        const string original =
            "<Project>\n"
            + "  <ItemGroup>\n"
            + "    <PackageVersion Include=\"Foo\" Version=\"1.0.0\" />\n"
            + "  </ItemGroup>\n"
            + "</Project>\n";

        var fs = new FakeFileSystem();
        fs.AddFile(path, original);

        var runner = new DefaultCmdRunner(new FakeConsole(), fs, cwd);
        var exit = await runner.UpdatePackageAsync("Bar", "9.9.9", Token);
        Assert.NotEqual(0, exit);

        // file should remain untouched
        var updated = fs[path];
        Assert.Equal(original, updated);
    }

    [Fact]
    public async Task UpdatePackageAsync_ShouldReturnNonZero_WhenFileMissing()
    {
        var fs = new FakeFileSystem();
        var runner = new DefaultCmdRunner(new FakeConsole(), fs, cwd);
        var exit = await runner.UpdatePackageAsync("Foo", "1.1.1", Token);
        Assert.NotEqual(0, exit);
    }

    [Fact]
    public async Task UpdatePackageAsync_ShouldPreserveWhitespace()
    {
        var fs = new FakeFileSystem();
        fs.AddFile(path, TestXml);
        var runner = new DefaultCmdRunner(new FakeConsole(), fs, cwd);
        var exit = await runner.UpdatePackageAsync(
            "Microsoft.NET.Test.Sdk",
            "18.3.0",
            Token
        );

        Assert.Equal(0, exit);
        var updated = fs[path];
        Assert.Equal(TestXml.Replace("18.0.1", "18.3.0"), updated);
    }

    private const string TestXml = """
<Project>
  <ItemGroup>
    <PackageVersion Include="AwesomeAssertions" Version="9.4.0" />
    <PackageVersion Include="AwesomeAssertions.Analyzers" Version="9.0.8" />
    <PackageVersion Include="coverlet.collector" Version="8.0.0" />
    <PackageVersion Include="coverlet.msbuild" Version="8.0.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="Verify" Version="29.3.1" />
    <PackageVersion Include="Verify.XunitV3" Version="31.13.2" />

    <!-- This is a comment that should be preserved -->
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />

    <PackageVersion Include="xunit.v3" Version="3.2.2"></PackageVersion>
  </ItemGroup>
</Project>
""";
}
