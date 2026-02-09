# changelog bundle-amend

Amend a bundle with additional changelog entries, creating an immutable `.amend-N.yaml` file.

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

`--resolve`
:   Optional: Copy the contents of each changelog file into the entries array. Defaults to false.

## Examples

### Add a single changelog to a bundle

```sh
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --add ./docs/changelog/138723.yaml
```

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

Use `--resolve` to copy the full contents of each changelog file into the bundle entries array:

```sh
docs-builder changelog bundle-amend \
  ./docs/changelog/bundles/9.3.0.yaml \
  --add "./docs/changelog/138723.yaml,./docs/changelog/1770424335.yaml" \
  --resolve
```
