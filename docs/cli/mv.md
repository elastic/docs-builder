# mv

Move a file from one location to another and update all links in the documentation

## Usage

```
mv [arguments...] [options...] [-h|--help] [--version]
```

## Arguments

`[0] <string>`
:   The source file or folder path to move from

`[1] <string>`
:   The target file or folder path to move to

## Options

`--dry-run` `<bool?>`
:   Dry run the move operation (Default:   null)

`-p|--path <string?>`
:   Defaults to the`{pwd}` folder (Default:   null)