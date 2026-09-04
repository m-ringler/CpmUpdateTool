# m-ringler.CpmUpdateTool

[![NuGet](https://img.shields.io/nuget/v/m-ringler.CpmUpdateTool.svg)](https://www.nuget.org/packages/m-ringler.CpmUpdateTool)


`CpmUpdateTool` is a small command‑line utility that inspects a .NET folder's `Directory.Packages.props` file and updates NuGet package versions to the latest available release.

## Installation

```bash
dotnet tool install m-ringler.CpmUpdateTool
```

## Usage

Run the tool from a directory containing `Directory.Packages.props` (typically the repo root):

```bash
dotnet update-cpm [solution-file] [options]
```

### Arguments

* `solution-file` &mdash; optionally use the specified solution file when checking for updates

### Options

* `--yes` &mdash; update all packages without asking
* `--include-prerelease` &mdash; consider prerelease package versions when checking for updates
* `--help` &mdash; display help text
* `--version` &mdash; print version

### Example

```bash
# interactive check with prompts
cd /my/solution
dotnet update-cpm --include-prerelease

# check packages for a specific solution
dotnet update-cpm MySolution.slnx

# non-interactive run, accept everything
dotnet update-cpm --yes
```

## License

This project is provided under the MIT License. See `LICENSE` for details.
