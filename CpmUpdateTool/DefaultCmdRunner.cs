using System.Diagnostics;

namespace CpmUpdateTool;

public class DefaultCmdRunner(
    IConsole console,
    IFileSystem fileSystem,
    string workingDirectory
) : ICmdRunner
{
    public async Task<string> ListOutdatedAsync(bool includePrerelease)
    {
        var args =
            "list package --outdated --format json"
            + (includePrerelease ? " --include-prerelease" : "");
        return await RunDotNetAsync(args);
    }

    public async Task<int> UpdatePackageAsync(string packageId, string version)
    {
        // update the central package version file in the current working directory
        // this method is intentionally simple and synchronous; keeping async
        // so callers can await it without changing their code.

        var propsPath = fileSystem.CombinePaths(
            workingDirectory,
            "Directory.Packages.props"
        );
        if (!fileSystem.Exists(propsPath))
        {
            // nothing to do if the file doesn't exist
            return 1;
        }

        // read with XmlReaderSettings so whitespace is retained, then operate
        // on an XDocument which makes LINQ-based mutation easier than XmlDocument
        var readerSettings = new System.Xml.XmlReaderSettings
        {
            IgnoreWhitespace = false,
        };

        System.Xml.Linq.XDocument? xdoc;
        using (var stream = fileSystem.OpenRead(propsPath))
        {
            xdoc = System.Xml.Linq.XDocument.Load(
                stream,
                System.Xml.Linq.LoadOptions.PreserveWhitespace
            );
        }

        if (xdoc == null)
        {
            // should not happen, but guard anyway
            return 3;
        }

        bool found = false;
        foreach (var el in xdoc.Descendants("PackageVersion"))
        {
            var include = (string?)el.Attribute("Include");
            if (string.Equals(include, packageId, StringComparison.Ordinal))
            {
                el.SetAttributeValue("Version", version);
                found = true;
            }
        }

        if (!found)
        {
            // package id not present
            return 2;
        }

        using (var stream = fileSystem.OpenWrite(propsPath))
        {
            // Preserve whitespace as much as possible. It is not
            // possible however, to preserve whitespace within XML tags.
            var writerSettings = new System.Xml.XmlWriterSettings
            {
                NewLineHandling = System.Xml.NewLineHandling.Entitize,
                OmitXmlDeclaration = xdoc.Declaration == null,
            };

            using var writer = System.Xml.XmlWriter.Create(
                stream,
                writerSettings
            );
            xdoc.Save(writer);
        }

        return 0;
    }

    private async Task<string> RunDotNetAsync(string args)
    {
        console.WriteLine($"Running: dotnet {args}");
        var psi = new ProcessStartInfo("dotnet", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
        };
        var p = Process.Start(psi);
        if (p == null)
            throw new InvalidOperationException(
                "failed to start dotnet process"
            );

        var outputTask = p.StandardOutput.ReadToEndAsync();
        var errorTask = p.StandardError.ReadToEndAsync();

        using (var cts = new CancellationTokenSource())
        {
            var busyTask = console.WriteBusyAsync(cts.Token);
            try
            {
                await p.WaitForExitAsync();
            }
            finally
            {
                cts.Cancel();
                await busyTask;
            }
        }

        var output = await outputTask;
        var error = await errorTask;
        if (p.ExitCode != 0)
        {
            if (string.IsNullOrWhiteSpace(error))
                error = output; // sometimes errors are written to stdout

            throw new InvalidOperationException(
                $"dotnet process exited with {p.ExitCode}: {error}"
            );
        }
        return output;
    }
}
