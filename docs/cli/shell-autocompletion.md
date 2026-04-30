# Shell autocompletion

`docs-builder` ships with built-in tab completion for subcommands, namespaces, and flags. No extra packages are needed — the completions are generated at build time and are trimming- and AOT-safe.

Run the following once to install completions for your shell, then open a new terminal session.

## Bash

Add to `~/.bashrc`:

```bash
eval "$(docs-builder __completion bash)"
```

## Zsh

Add to `~/.zshrc`:

```zsh
source <(docs-builder __completion zsh)
```

## Fish

```fish
mkdir -p ~/.config/fish/completions
docs-builder __completion fish > ~/.config/fish/completions/docs-builder.fish
```

Requires Fish 3.4 or later.

## Inspect the generated script

You can print the raw completion script for any shell without installing it:

```bash
docs-builder __completion bash
docs-builder __completion zsh
docs-builder __completion fish
```
