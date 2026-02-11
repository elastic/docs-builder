# changelog bundle-amend

Amend a bundle with additional changelog entries.
Amend bundles follow a specific naming convention: `{parent-bundle-name}.amend-{N}.yaml` where `{N}` is a sequence number.

To create a bundle, use [](/cli/release/changelog-bundle.md).
For details and examples, go to [](/contribute/changelog.md).

## Usage

```sh
docs-builder changelog bundle-amend [arguments...] [options...] [-h|--help]
```

## Arguments

`<string>`
:   Required: Path to the original bundle file to amend.

## Options

`--add <string[]?>`
:   Required: Path(s) to changelog YAML file(s) to add as comma-separated values. Supports tilde (~) expansion and relative paths.

`--no-resolve`:
:   Optional: Explicitly turn off resolve (overrides inference from original bundle).

`--resolve`
:   Optional: Copy the contents of each changelog file into the entries array. Defaults to false.

## Resolve behaviour

By default, the `bundle-amend` command **infers** whether to resolve entries from the original bundle.
If the original bundle contains resolved entries (with inline `title`, `type`, and so on), the amend file will also be resolved.
If the original bundle contains only file references, the amend file will also contain only file references.

This inference ensures that amend files are portable—they contain everything needed to be understood alongside the original bundle, even when copied to another repository.

You can override this behaviour:

- `--resolve`: Force entries to be resolved (inline content), regardless of the original bundle.
- `--no-resolve`: Force entries to contain only file references, regardless of the original bundle.

## Output

Amend bundles contain only the additional entries, they are not a full repetition of the original bundle.
For example:

```yaml
# 9.3.0.amend-1.yaml
entries:
- file:
    name: late-addition.yaml
    checksum: abc123def456
```

When bundles are loaded (either via the `changelog render` command or the `{changelog}` directive), amend files are **automatically merged** with their parent bundles.
The entries from all matching amend files are combined with the parent bundle's entries, and the result is rendered as a single release.

:::{note}
Amend bundles do not need to include `products` or `hide-features` fields—they inherit these from their parent bundle. If an amend bundle is found without a matching parent bundle, it remains standalone.
:::

## Examples

### Add a single changelog to a bundle

```sh
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --add ./docs/changelog/138723.yaml
```

The new bundle automatically matches the resolve style of the original bundle.

### Add multiple changelogs to a bundle

Specify multiple files as comma-separated values:

```sh
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --add "./docs/changelog/138723.yaml,./docs/changelog/1770424335.yaml"
```

### Using different path styles

The command supports tilde expansion, relative paths, and absolute paths:

```sh
# With tilde expansion
docs-builder changelog bundle-amend \
  ~/docs/changelog/bundles/9.3.0.yaml \
  --add "~/docs/changelog/138723.yaml,~/docs/changelog/1770424335.yaml"

# With relative paths
docs-builder changelog bundle-amend \
  ./bundles/9.3.0.yaml \
  --add "./138723.yaml,./1770424335.yaml"

# With absolute paths
docs-builder changelog bundle-amend \
  /path/to/bundles/9.3.0.yaml \
  --add "/path/to/138723.yaml,/path/to/1770424335.yaml"
```

### Resolving changelog contents

Use `--resolve` to copy the full contents of each changelog file into the new bundle even if the original bundle is unresolved:

```sh
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --add "./docs/changelog/138723.yaml,./docs/changelog/1770424335.yaml" \
  --resolve
```

Likewise, you can force file-only references even if the original bundle is resolved:

```sh
docs-builder changelog bundle-amend 9.3.0.yaml \
  --add ./docs/changelog/late-addition.yaml \
  --no-resolve
```
