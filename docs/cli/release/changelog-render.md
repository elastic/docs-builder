# changelog render

Generate markdown files from changelog bundle files.

To create the bundle files, use [](/cli/release/changelog-bundle.md).

For details and examples, go to [](/contribute/changelog.md).

## Usage

```sh
docs-builder changelog render [options...] [-h|--help]
```

## Options

  --input <List<BundleInput>>    Required: Bundle input(s) in format "bundle-file-path, changelog-file-path, repo". Can be specified multiple times. Only bundle-file-path is required. [Required]
  --output <string?>             Optional: Output directory for rendered markdown files. Defaults to current directory [Default: null]
  --title <string?>              Optional: Title to use for section headers in output markdown files. Defaults to version from first bundle [Default: null]
  --subsections                  Optional: Group entries by area/component in subsections. Defaults to false
  --hide-private-links           Optional: Hide private links by commenting them out in markdown output. Defaults to false

`--input <List<BundleInput>>`
:   One or more bundle input files.
:   Each item can be specified as "bundle-file-path, changelog-file-path, repo" to accommodate files coming from multiple locations.
:   Only `bundle-file-path` is required.

`--output <string?>`
:   Optional: The output directory for rendered markdown files.
:   Defaults to current directory.

`--title <string?>`
:   Optional: The title to use for section headers, directories, and anchors in output markdown files.
:   Defaults to the version in the first bundle.
:   If the string contains spaces, they are replaced with dashes when used in directory names and anchors.

`--subsections`
:   Optional: Group entries by area in subsections.
:   Defaults to false.

`--hide-private-links`
:   Optional: Hide private links by commenting them out in markdown output.
:   This option is useful when rendering changelog bundles in private repositories.
:   Defaults to false.
