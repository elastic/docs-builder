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

To build from source, see the [docs-builder repository](https://github.com/elastic/docs-builder).

## Verify the installation

```bash
docs-builder --version
```

## Shell autocompletion

{{dbuild}} ships with built-in tab completion for subcommands, namespaces, and flags. Run the following once to install completions for your shell, then open a new terminal session.

### Bash

Add to `~/.bashrc`:

```bash
eval "$(docs-builder __completion bash)"
```

### Zsh

Add to `~/.zshrc`:

```zsh
source <(docs-builder __completion zsh)
```

### Fish

```shell
mkdir -p ~/.config/fish/completions
docs-builder __completion fish > ~/.config/fish/completions/docs-builder.fish
```

Requires Fish 3.4 or later.
