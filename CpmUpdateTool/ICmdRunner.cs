namespace CpmUpdateTool;

public interface ICmdRunner
{
    Task<string> ListOutdatedAsync(
        string? solutionFile,
        bool includePrerelease,
        CancellationToken token = default
    );

    Task<int> UpdatePackageAsync(
        string packageId,
        string version,
        CancellationToken token = default
    );
}
