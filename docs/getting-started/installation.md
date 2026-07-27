# Installation

## Automated install (recommended)

The quickest way to get started on Linux and macOS is the one-line installer:

```bash
curl -sL https://ela.st/docs-builder-install | sh
```

On Windows, run this in PowerShell:

```ps1
iex (New-Object System.Net.WebClient).DownloadString('https://ela.st/docs-builder-install-win')
```

Both scripts download the latest release binary and add it to your system `PATH`.

## Manual install

Download the binary directly from the [Releases page](https://github.com/elastic/docs-builder/releases), extract it, and place it somewhere on your `PATH`.

## Build from source

You need [.NET 10](https://dotnet.microsoft.com/download) installed.

```bash
git clone https://github.com/elastic/docs-builder
cd docs-builder
./build.sh publishbinaries
```

The compiled binary is written to `.artifacts/publish/docs-builder/release/docs-builder`.

## Verify the installation

```bash
docs-builder --version
```
