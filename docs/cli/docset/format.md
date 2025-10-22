# format

Format documentation files by fixing common issues like irregular space

## Usage

```
docs-builder format --check [options...]
docs-builder format --write [options...]
```

## Options

`--check`
:   Check if files need formatting without modifying them. Exits with code 1 if formatting is needed, 0 if all files are properly formatted. (required, mutually exclusive with --write)

`--write`
:   Write formatting changes to files. (required, mutually exclusive with --check)

`-p|--path` `<string>`
:   Path to the documentation folder, defaults to pwd. (optional)

## Description

The `format` command automatically detects and fixes formatting issues in your documentation files. The command only processes Markdown files (`.md`) that are included in your `_docset.yml` table of contents, ensuring that only intentional documentation files are modified.

You must specify exactly one of `--check` or `--write`:
- `--check` validates formatting without modifying files, useful for CI/CD pipelines
- `--write` applies formatting changes to files

Currently, it handles irregular space characters that may impair Markdown rendering.

### Irregular Space Detection

The format command detects and replaces 24 types of irregular space characters with regular spaces, including:

- No-Break Space (U+00A0)
- En Space (U+2002)
- Em Space (U+2003)
- Zero Width Space (U+200B)
- Line Separator (U+2028)
- Paragraph Separator (U+2029)
- And 18 other irregular space variants

These characters can cause unexpected rendering issues in Markdown and are often introduced accidentally through copy-paste operations from other applications.

## Examples

### Check if formatting is needed (CI/CD)

```bash
docs-builder format --check
```

Exit codes:
- `0`: All files are properly formatted
- `1`: Some files need formatting

### Apply formatting changes

```bash
docs-builder format --write
```

### Check specific documentation folder

```bash
docs-builder format --check --path /path/to/docs
```

### Format specific documentation folder

```bash
docs-builder format --write --path /path/to/docs
```

## Output

### Check mode output

When using `--check`, the command reports which files need formatting:

```
Checking documentation in: /path/to/docs

Formatting needed:
  Files needing formatting: 2
  irregular space fixes needed: 3

Run 'docs-builder format --write' to apply changes
```

### Write mode output

When using `--write`, the command reports the changes made:

```
Formatting documentation in: /path/to/docs
Formatted index.md (2 change(s))

Formatting complete:
  Files processed: 155
  Files modified: 1
  irregular space fixes: 2
```

## Future Enhancements

The format command is designed to be extended with additional formatting capabilities in the future, such as:

- Line ending normalization
- Trailing whitespace removal
- Consistent heading spacing
- And other formatting fixes
