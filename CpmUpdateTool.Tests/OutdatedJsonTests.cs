namespace CpmUpdateTool.Tests;

public class OutdatedJsonTests
{
    [Fact]
    public void Flatten_ShouldReturnNoPackages_WhenJsonNullOrEmpty()
    {
        OutdatedJson? empty = null;
        var results = OutdatedJsonExtensions.Flatten(empty!);
        Assert.Empty(results);
    }

    [Fact]
    public void Flatten_ShouldDeduplicateAndSkipSameVersion()
    {
        var json = new OutdatedJson(
            version: 1,
            parameters: "",
            sources: [],
            projects:
            [
                new ProjectInfo(
                    "p1",
                    [
                        new FrameworkInfo(
                            "net10.0",
                            [
                                new PackageInfo("A", "1.0.0", "1.0.0", "2.0.0"),
                                new PackageInfo("B", "1.0.0", "1.0.0", "1.0.0"),
                            ]
                        ),
                    ]
                ),
                new ProjectInfo(
                    "p2",
                    [
                        new FrameworkInfo(
                            "net10.0",
                            [new PackageInfo("A", "1.0.0", "1.0.0", "2.0.0")]
                        ),
                    ]
                ),
            ]
        );

        var upgrades = json.Flatten().ToList();
        Assert.Single(upgrades);
        var pkg = upgrades[0];
        Assert.Equal("A", pkg.Id);
        Assert.Equal("1.0.0", pkg.CurrentVersion);
        Assert.Equal("2.0.0", pkg.LatestVersion);
    }

    [Fact]
    public void Flatten_ShouldSkipNonSemverLatestVersion()
    {
        var json = new OutdatedJson(
            version: 1,
            parameters: "",
            sources: [],
            projects:
            [
                new ProjectInfo(
                    "p1",
                    [
                        new FrameworkInfo(
                            "net10.0",
                            [
                                // valid semver start, include patch
                                new PackageInfo(
                                    "Good",
                                    "1.0.0",
                                    "1.0.0",
                                    "2.1.3"
                                ),
                                // missing patch, should be skipped
                                new PackageInfo(
                                    "Bad1",
                                    "1.0.0",
                                    "1.0.0",
                                    "2.0"
                                ),
                                // not numeric, should be skipped
                                new PackageInfo(
                                    "Bad2",
                                    "1.0.0",
                                    "1.0.0",
                                    "alpha"
                                ),
                                // semver with prerelease is ok
                                new PackageInfo(
                                    "Good2",
                                    "1.0.0",
                                    "1.0.0",
                                    "3.0.0-beta"
                                ),
                            ]
                        ),
                    ]
                ),
            ]
        );

        var upgrades = json.Flatten().ToList();
        // only Good and Good2 should appear
        Assert.Equal(2, upgrades.Count);
        Assert.Contains(
            upgrades,
            u => u.Id == "Good" && u.LatestVersion == "2.1.3"
        );
        Assert.Contains(
            upgrades,
            u => u.Id == "Good2" && u.LatestVersion == "3.0.0-beta"
        );
    }
}
