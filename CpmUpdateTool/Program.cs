namespace CpmUpdateTool;

using System.CommandLine;
using System.Text.Json;

internal class Program(
    ICmdRunner runner,
    IConsole console,
    IFileSystem fileSystem
)
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly Option<bool> yesOption = new("--yes")
    {
        Description = "Update all packages without prompting",
    };

    private readonly Option<bool> includePrereleaseOption = new(
        "--include-prerelease"
    )
    {
        Description = "Include prerelease versions when checking for updates",
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
        root.SetAction(RunAsync);

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
        if (!fileSystem.Exists(propsFile))
        {
            console.ErrorWriteLine(
                $"error: {propsFile} not found in current directory"
            );
            return 1;
        }

        string jsonText;
        try
        {
            console.WriteLine("Checking for outdated packages...");
            jsonText = await runner.ListOutdatedAsync(
                includePrerelease,
                CancellationToken.None
            );
        }
        catch (Exception ex)
        {
            console.ErrorWriteLine($"failed to list outdated packages: {ex}");
            return 1;
        }

        OutdatedJson? data;
        try
        {
            data = JsonSerializer.Deserialize<OutdatedJson>(jsonText, options);
        }
        catch (Exception ex)
        {
            console.ErrorWriteLine($"error parsing JSON output: {ex}");
            return 1;
        }

        var upgrades = data?.Flatten() ?? [];
        if (upgrades == null || upgrades.IsEmpty)
        {
            console.WriteLine("All packages are up to date.");
            return 0;
        }

        console.WriteLine("Packages with available updates:");
        foreach (var u in upgrades)
        {
            console.WriteLine(
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
                console.WriteLine($"updating {u.Id} to {u.LatestVersion}...");
                var exit = await runner.UpdatePackageAsync(
                    u.Id,
                    u.LatestVersion,
                    CancellationToken.None
                );
                if (exit != 0)
                {
                    console.ErrorWriteLine(
                        $"dotnet package update returned code {exit}"
                    );
                }
            }
        }

        return 0;
    }

    private bool AskUser(string prompt)
    {
        console.Write(prompt);
        var resp = console.ReadLine();
        if (string.IsNullOrEmpty(resp))
            return false;
        char c = resp.Trim().ToLowerInvariant()[0];
        return c == 'y';
    }
}
