using System.CommandLine;
using System.Text.Json;

namespace CpmUpdateTool;

using System.Text;

internal class Program(
    ICmdRunner runner,
    IConsole console,
    IFileSystem fileSystem
)
{
    private readonly ICmdRunner runner = runner;
    private readonly IConsole console = console;
    private readonly IFileSystem fileSystem = fileSystem;

    private readonly Option<bool> yesOption = new("--yes")
    {
        Description = "update all packages without prompting",
    };

    private readonly Option<bool> includePrereleaseOption = new(
        "--include-prerelease"
    )
    {
        Description = "include prerelease versions when checking for updates",
    };

    private static Task<int> Main(string[] args)
    {
        var console = new RealConsole();
        var fs = new RealFileSystem();
        var runner = new DefaultCmdRunner(
            console,
            fs,
            Directory.GetCurrentDirectory()
        );
        var program = new Program(runner, console, fs);
        var pr = program.BuildRootCommand().Parse(args);
        return pr.InvokeAsync();
    }

    internal static Task<int> RunAsync(
        string[] args,
        ICmdRunner runner,
        IConsole console,
        IFileSystem fileSystem
    )
    {
        var program = new Program(runner, console, fileSystem);
        var root = program.BuildRootCommand();
        var pr = root.Parse(args);
        return pr.InvokeAsync();
    }

    internal Command BuildRootCommand()
    {
        // Build a simple command line schema using System.CommandLine.

        var root = new RootCommand(
            "Inspect and update NuGet packages referenced in Directory.Packages.props"
        )
        {
            this.includePrereleaseOption,
            this.yesOption,
        };

        root.TreatUnmatchedTokensAsErrors = true;

        // handler encapsulates the previous logic; options are supplied by the parser
        root.SetAction(this.RunAsync);

        return root;
    }

    private async Task<int> RunAsync(
        ParseResult parseResult,
        CancellationToken ct
    )
    {
        var yes = parseResult.GetValue(this.yesOption);
        var includePrerelease = parseResult.GetValue(
            this.includePrereleaseOption
        );

        const string propsFile = "Directory.Packages.props";
        if (!this.fileSystem.Exists(propsFile))
        {
            this.console.ErrorWriteLine(
                $"error: {propsFile} not found in current directory"
            );
            return 1;
        }

        string jsonText;
        try
        {
            this.console.WriteLine("Checking for outdated packages...");
            jsonText = await this.runner.ListOutdatedAsync(includePrerelease);
        }
        catch (Exception ex)
        {
            this.console.ErrorWriteLine(
                $"failed to list outdated packages: {ex}"
            );
            return 1;
        }

        OutdatedJson? data;
        try
        {
            data = JsonSerializer.Deserialize<OutdatedJson>(
                jsonText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        catch (Exception ex)
        {
            this.console.ErrorWriteLine(
                $"error parsing JSON output: {ex.Message}"
            );
            return 1;
        }

        var upgrades = data?.Flatten() ?? [];
        if (upgrades == null || !upgrades.Any())
        {
            this.console.WriteLine("All packages are up to date.");
            return 0;
        }

        this.console.WriteLine("Packages with available updates:");
        foreach (var u in upgrades)
        {
            this.console.WriteLine(
                $"- {u.Id}: {u.CurrentVersion} -> {u.LatestVersion}"
            );
        }

        foreach (var u in upgrades)
        {
            bool doUpdate = yes;
            if (!yes)
            {
                doUpdate = AskUser(
                    $"Update {u.Id} from {u.CurrentVersion} to {u.LatestVersion}? (y/n): "
                );
            }
            if (doUpdate)
            {
                this.console.WriteLine(
                    $"updating {u.Id} to {u.LatestVersion}..."
                );
                var exit = await this.runner.UpdatePackageAsync(
                    u.Id,
                    u.LatestVersion
                );
                if (exit != 0)
                {
                    this.console.ErrorWriteLine(
                        $"dotnet package update returned code {exit}"
                    );
                }
            }
        }

        return 0;
    }

    private bool AskUser(string prompt)
    {
        this.console.Write(prompt);
        var resp = this.console.ReadLine();
        if (string.IsNullOrEmpty(resp))
            return false;
        char c = resp.Trim().ToLowerInvariant()[0];
        return c == 'y';
    }

    private class ConsoleTextWriter : TextWriter
    {
        private readonly IConsole console;
        private readonly bool isError;

        public ConsoleTextWriter(IConsole console, bool isError)
        {
            this.console = console;
            this.isError = isError;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string? value)
        {
            if (this.isError)
                this.console.ErrorWriteLine(value ?? string.Empty);
            else
                this.console.WriteLine(value ?? string.Empty);
        }

        public override void Write(char value)
        {
            if (this.isError)
                this.console.ErrorWriteLine(value.ToString());
            else
                this.console.Write(value.ToString());
        }

        public override void Write(string? value)
        {
            if (this.isError)
                this.console.ErrorWriteLine(value ?? string.Empty);
            else
                this.console.Write(value ?? string.Empty);
        }
    }
}
