using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace CpmUpdateTool;

public record OutdatedJson(
    int version,
    string parameters,
    List<string> sources,
    List<ProjectInfo> projects
);

public record ProjectInfo(string path, List<FrameworkInfo> frameworks);

public record FrameworkInfo(
    string framework,
    List<PackageInfo> topLevelPackages
);

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
        return this.EqualityContract == other.EqualityContract && this.Id == other.Id;
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

    public static ImmutableSortedSet<PackageUpgrade> Flatten(this OutdatedJson json)
    {
        if (json?.projects == null)
            return [];

        var results = ImmutableSortedSet.CreateBuilder<PackageUpgrade>();
        foreach (var project in json.projects)
        {
            if (project.frameworks == null)
                continue;
            foreach (var fw in project.frameworks)
            {
                if (fw.topLevelPackages == null)
                    continue;
                foreach (var pkg in fw.topLevelPackages)
                {
                    if (pkg == null)
                        continue;
                    if (
                        string.IsNullOrEmpty(pkg.id)
                        || pkg.latestVersion == pkg.requestedVersion
                    )
                        continue; // no update or invalid

                    // ensure latestVersion begins with major.minor.patch (semver-like)
                    // examples: "1.2.3", "2.0.0-beta" are ok; "1.2", "beta" are skipped
                    if (!SemVer().IsMatch(pkg.latestVersion ?? string.Empty))
                        continue; // not a semantic version we can handle

                    results.Add(
                        new PackageUpgrade(
                            pkg.id,
                            pkg.requestedVersion,
                            pkg.latestVersion!
                        )
                    );
                }
            }
        }

        return results.ToImmutableSortedSet();
    }
}
