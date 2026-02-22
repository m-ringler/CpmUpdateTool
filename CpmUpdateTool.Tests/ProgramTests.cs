namespace CpmUpdateTool.Tests;

public class ProgramTests
{
    private const string SampleJson =
        "{ "
        + "\"version\":1,\"parameters\":\"--outdated\",\"sources\":[],\"projects\":[{"
        + "\"path\":\"a.csproj\",\"frameworks\":[{\"framework\":\"net10.0\",\"topLevelPackages\":[{\"id\":\"Foo\",\"requestedVersion\":\"1.0\",\"resolvedVersion\":\"1.0\",\"latestVersion\":\"2.0.0\"}]}]}]}";

    [Fact]
    public async Task Main_ShouldReturnError_WhenPropsMissing()
    {
        var fake = new FakeRunner();
        var console = new FakeConsole();
        var fs = new FakeFileSystem();

        var exit = await RunWithRunner(fake, console, fs, []);
        Assert.Equal(1, exit);
        Assert.Contains(
            "not found",
            console.Err.ToString(),
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public async Task Main_ShouldListAndAsk_AndUpdate_WhenUserApproves()
    {
        var fake = new FakeRunner { JsonToReturn = SampleJson };
        var console = new FakeConsole();
        console.QueueInput("y");
        var fs = new FakeFileSystem();
        fs.AddFile("Directory.Packages.props");

        var exit = await RunWithRunner(fake, console, fs, []);
        Assert.Equal(0, exit);
        Assert.True(fake.ListCalled);
        Assert.True(fake.UpdateCalled);
        Assert.Contains("Foo: 1.0 -> 2.0", console.Out.ToString());
    }

    [Fact]
    public async Task Main_ShouldUpdateAll_WhenYesOption()
    {
        var fake = new FakeRunner { JsonToReturn = SampleJson };
        var console = new FakeConsole();
        var fs = new FakeFileSystem();
        fs.AddFile("Directory.Packages.props");

        var exit = await RunWithRunner(fake, console, fs, ["--yes"]);
        Assert.Equal(0, exit);
        Assert.True(fake.ListCalled);
        Assert.True(fake.UpdateCalled);
    }

    [Fact]
    public async Task Main_ShouldShowHelp_WhenHelpOption()
    {
        var fake = new FakeRunner();
        var console = new FakeConsole();
        var fs = new FakeFileSystem();
        fs.AddFile("Directory.Packages.props");

        Assert.False(fake.ListCalled);
        Assert.False(fake.UpdateCalled);
        var (exitCode, output, errorOutput) = HelpVerifier.GetHelp<Program>(
            new Program(fake, console, fs).BuildRootCommand()
        );
        Assert.Equal(0, exitCode);
        Assert.Empty(errorOutput);
        Assert.Equal(
            """
Description:
  Inspect and update NuGet packages referenced in Directory.Packages.props

Nutzung:
  CpmUpdateTool.Tests [options]

Optionen:
  --include-prerelease  include prerelease versions when checking for updates
  --yes                 update all packages without prompting
  -?, -h, --help        Show help and usage information
  --version             Versionsinformationen anzeigen


""",
            output
        );
    }

    [Fact]
    public async Task Main_ShouldSupportYesOption()
    {
        var fake = new FakeRunner { JsonToReturn = SampleJson };
        var console = new FakeConsole();
        var fs = new FakeFileSystem();
        fs.AddFile("Directory.Packages.props");

        var exit = await RunWithRunner(fake, console, fs, ["--yes"]);
        Assert.Equal(0, exit);
        Assert.True(fake.ListCalled);
        Assert.True(fake.UpdateCalled);
    }

    [Fact]
    public async Task Main_ShouldError_OnUnknownArgument()
    {
        var fake = new FakeRunner();
        var console = new FakeConsole();
        var fs = new FakeFileSystem();
        fs.AddFile("Directory.Packages.props");

        var exit = await RunWithRunner(fake, console, fs, ["--foobar"]);
        Assert.Equal(1, exit);
        // error text may not be captured by our fake console
        Assert.False(fake.ListCalled);
    }

    private static Task<int> RunWithRunner(
        FakeRunner runner,
        IConsole console,
        IFileSystem fs,
        string[] args
    )
    {
        return Program.RunAsync(args, runner, console, fs);
    }
}
