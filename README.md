# CpmUpdate

`CpmUpdate` is a small command‑line utility that inspects a .NET folder's `Directory.Packages.props` file and updates NuGet package versions to the latest available release using `dotnet` CLI commands.

## Features

* Lists outdated top‑level NuGet package references in projects under the current directory.
* Optionally includes prerelease versions when checking for updates.
* Prompts interactively for each upgrade, or can run non‑interactively with `--yes`.
* Abstracts filesystem and console operations to ease testing.
* Extensible through `ICmdRunner` for custom command execution.

## Building

```bash
cd /path/to/CpmUpdate
# restore and build everything
dotnet build
```

### Formatting

We use [CSharpier](https://github.com/belav/csharpier) to keep code style consistent:

```bash
csharpier format .
```

## Installation

dotnet tool install m-ringler.CpmUpdateTool

## Usage

Run the tool from a directory containing `Directory.Packages.props` (typically the repo root):

```bash
dotnet update-cpm [options]
```

### Options

* `-y`, `--yes` &mdash; update all packages without asking.
* `--include-prerelease` &mdash; consider prerelease package versions when checking for updates.
* `--help` &mdash; display help text.

### Example

```bash
# interactive check with prompts
cd /my/solution
dotnet update-cpm --include-prerelease

# non-interactive run, accept everything
dotnet update-cpm -y
```

## License

This project is provided under the MIT License. See `LICENSE` for details.
