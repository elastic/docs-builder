# format

Format documentation files by fixing common issues like irregular whitespace

## Usage

```
docs-builder format [options...] [-h|--help] [--version]
```

## Options

`-p|--path` `<string>`
:   Path to the documentation folder, defaults to pwd. (optional)

`--dry-run` `<bool?>`
:   Preview changes without modifying files (optional)

## Description

The `format` command automatically detects and fixes formatting issues in your documentation files. Currently, it handles irregular whitespace characters that may impair Markdown rendering.

### Irregular Whitespace Detection

The format command detects and replaces 24 types of irregular whitespace characters with regular spaces, including:

- No-Break Space (U+00A0)
- En Space (U+2002)
- Em Space (U+2003)
- Zero Width Space (U+200B)
- Line Separator (U+2028)
- Paragraph Separator (U+2029)
- And 18 other irregular whitespace variants

These characters can cause unexpected rendering issues in Markdown and are often introduced accidentally through copy-paste operations from other applications.

## Examples

### Format current directory

```bash
docs-builder format
```

### Preview changes without modifying files

```bash
docs-builder format --dry-run
```

### Format specific documentation folder

```bash
docs-builder format --path /path/to/docs
```

## Output

The command provides detailed feedback about the formatting process:

```
Formatting documentation in: /path/to/docs
Fixed 2 irregular whitespace(s) in: guide/setup.md
Fixed 1 irregular whitespace(s) in: api/endpoints.md

Formatting complete:
  Files processed: 155
  Files modified: 2
  Total replacements: 3
```

When using `--dry-run`, files are not modified and the command reminds you to run without the flag to apply changes.

## Future Enhancements

The format command is designed to be extended with additional formatting capabilities in the future, such as:

- Line ending normalization
- Trailing whitespace removal
- Consistent heading spacing
- And other formatting fixes
