# mv

Move a file or folder from one location to another and update all links in the documentation

## Usage

```
docs-builder mv [arguments...] [options...] [-h|--help] [--version]
```

## Arguments

`[0] <string>`
:   The source file or folder path to move from (required)

`[1] <string>`
:   The target file or folder path to move to (required)

## Options

`--dry-run` `<bool?>`
:   Dry run the move operation (optional)

`-p|--path <string>`
:   Defaults to the`{pwd}` folder (optional)