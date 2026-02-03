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
:   Required: Path to the original bundle file.

## Options

`--add <string[]?>`
:   Required: Path(s) to changelog YAML file(s) to add. Can be specified multiple times.

`--resolve`
:   Optional: Copy the contents of each changelog file into the entries array. Defaults to false.
