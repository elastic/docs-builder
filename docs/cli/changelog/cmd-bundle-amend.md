## Description

Amend a bundle with additional or excluded changelog entries without modifying the parent bundle file.
Amend bundles follow a specific naming convention: `{parent-bundle-name}.amend-{N}.yaml` where `{N}` is a sequence number.

Specify at least one of `--add` or `--remove`.

To create a bundle, use [](/cli/changelog/bundle.md).
For details and examples, go to [](/contribute/bundle-changelogs.md).

## Resolve behaviour

By default, the `bundle-amend` command **infers** whether to resolve entries from the original bundle when you use `--add`.
If the original bundle contains resolved entries (with inline `title`, `type`, and so on), the amend file will also be resolved.
If the original bundle contains only file references, the amend file will also contain only file references.

This inference ensures that amend files are portable — they contain everything needed to be understood alongside the original bundle, even when copied to another repository.

You can override this behaviour:

- `--resolve`: Force entries to be resolved (inline content), regardless of the original bundle.
- `--no-resolve`: Force entries to contain only file references, regardless of the original bundle.

`--resolve` and `--no-resolve` apply only to `--add`. Removals always record `exclude-entries` with file name and checksum.

## Output

Amend bundles contain the parent bundle's `products` plus only the changes for that amend file, not a full repetition of the original bundle's entries.

The parent's complete `products` (including `target`, `repo`, and `owner`) are copied into every amend file so the amend is self-contained: upload destination discovery, the registry's per-product `target`, and `:version:`-filtered CDN consumption all derive from a bundle file's own products.

Additions:

```yaml
# 9.3.0.amend-1.yaml
products:
- product: elasticsearch
  target: 9.3.0
  repo: elasticsearch
  owner: elastic
entries:
- file:
    name: late-addition.yaml
    checksum: abc123def456
```

Removals:

```yaml
# 9.3.0.amend-2.yaml
products:
- product: elasticsearch
  target: 9.3.0
  repo: elasticsearch
  owner: elastic
exclude-entries:
- file:
    name: 138723.yaml
    checksum: def456abc123
```

An amend file can contain both `exclude-entries` and `entries`. Within each amend file, exclusions are applied before additions.

When bundles are loaded (either via the `changelog render` command or the `{changelog}` directive), amend files are **automatically merged** with their parent bundles in sequence (`amend-1`, `amend-2`, …).
The result is rendered as a single release.

:::{note}
Amend bundles created by older docs-builder versions may omit `products`; they are still accepted when loading and merge into their parent as before. `hide-features` is always inherited from the parent bundle. If an amend bundle is found without a matching parent bundle, it remains standalone.

`rules.bundle` filtering does not apply to `changelog bundle-amend`. The command is a direct-injection escape hatch: the files you specify with `--add` are always included regardless of any product, type, or area filter configuration.
:::

## Examples

### Add a single changelog to a bundle

```sh
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --add ./docs/changelog/138723.yaml
```

The new bundle automatically matches the resolve style of the original bundle.

### Remove a changelog from a bundle

```sh
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --remove ./docs/changelog/138723.yaml
```

The CLI computes the file checksum automatically and matches it against the effective bundle (parent plus any existing amend files).
If the bundle contains the file with a different checksum, the command fails unless you pass `--force` to remove by file name only.

### Add multiple changelogs to a bundle

Comma-separated list:

```sh
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --add "./docs/changelog/138723.yaml,./docs/changelog/1770424335.yaml"
```

Or repeat `--add`:

```sh
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --add ./docs/changelog/138723.yaml \
  --add ./docs/changelog/1770424335.yaml
```

### Remove multiple changelogs from a bundle

```sh
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --remove "./docs/changelog/old-a.yaml,./docs/changelog/old-b.yaml"
```

### Replace an entry in one amend file

```sh
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --remove ./docs/changelog/old-entry.yaml \
  --add ./docs/changelog/new-entry.yaml
```

### Force resolve or file-reference style

```sh
# Force resolved (inline) entries
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --add "./docs/changelog/138723.yaml" \
  --resolve

# Force file-only references
docs-builder changelog bundle-amend 9.3.0.yaml \
  --add ./docs/changelog/late-addition.yaml \
  --no-resolve
```

### Preview without writing an amend file

```sh
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --remove ./docs/changelog/138723.yaml \
  --dry-run
```
