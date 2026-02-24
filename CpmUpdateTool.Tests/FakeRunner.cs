namespace CpmUpdateTool.Tests;

internal class FakeRunner : ICmdRunner
{
    public bool ListCalled { get; private set; }
    public bool UpdateCalled { get; private set; }
    public string LastUpdateId { get; private set; } = string.Empty;
    public string LastUpdateVersion { get; private set; } = string.Empty;
    public bool ThrowOnList { get; set; }
    public string JsonToReturn { get; set; } = string.Empty;

    public Task<string> ListOutdatedAsync(
        bool includePrerelease,
        CancellationToken token = default
    )
    {
        ListCalled = true;
        if (ThrowOnList)
            throw new InvalidOperationException("fail");
        return Task.FromResult(JsonToReturn);
    }

    public Task<int> UpdatePackageAsync(
        string packageId,
        string version,
        CancellationToken token = default
    )
    {
        UpdateCalled = true;
        LastUpdateId = packageId;
        LastUpdateVersion = version;
        return Task.FromResult(0);
    }
}
