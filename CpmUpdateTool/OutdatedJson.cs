using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace CpmUpdateTool;

public record OutdatedJson(
    int version,
    string parameters,
    string[] sources,
    ProjectInfo[] projects
);

public record ProjectInfo(string path, FrameworkInfo[] frameworks);

public record FrameworkInfo(string framework, PackageInfo[] topLevelPackages);

public record PackageInfo(
    string id,
    string requestedVersion,
    string resolvedVersion,
    string latestVersion
);

public sealed record PackageUpgrade(
    string Id,
    string CurrentVersion,
    string LatestVersion
) : IComparable<PackageUpgrade>
{
    public int CompareTo(PackageUpgrade? other)
    {
        return this.Id.CompareTo(other?.Id);
    }

    public bool Equals(PackageUpgrade? other)
    {
        if (other == null)
            return false;
        return this.EqualityContract == other.EqualityContract
            && this.Id == other.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.EqualityContract, this.Id);
    }
}

public static partial class OutdatedJsonExtensions
{
    [GeneratedRegex(@"^\d+\.\d+\.\d+")]
    public static partial Regex SemVer();

    public static ImmutableSortedSet<PackageUpgrade> Flatten(
        this OutdatedJson json
    )
    {
        var query =
            from project in json?.projects ?? []
            from fw in project.frameworks ?? []
            from pkg in fw.topLevelPackages ?? []
            where
                pkg != null
                && !string.IsNullOrEmpty(pkg.id)
                && pkg.latestVersion != pkg.requestedVersion
                && SemVer().IsMatch(pkg.latestVersion ?? string.Empty)
            select new PackageUpgrade(
                pkg.id,
                pkg.requestedVersion,
                pkg.latestVersion!
            );

        return [.. query];
    }
}
