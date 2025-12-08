`applies_to` accepts the following version formats:

* **Greater than or equal to**: `x.x+`, `x.x`, `x.x.x+`, `x.x.x` (default behavior when no operator specified)
* **Range (inclusive)**: `x.x-y.y`, `x.x.x-y.y.y`, `x.x-y.y.y`, `x.x.x-y.y`
* **Exact version**: `=x.x`, `=x.x.x`

**Version Display:**

- Versions are always displayed as **Major.Minor** (e.g., `9.1`) in badges, regardless of the format used in source files.
- Each version represents the **latest patch** of that minor version (e.g., `9.1` means 9.1.0, 9.1.1, 9.1.6, etc.).
- The `+` symbol indicates "this version and later" (e.g., `9.1+` means 9.1.0 and all subsequent releases).
- Ranges show both versions (e.g., `9.0-9.2`) when both are released, or convert to `+` format if the end version is unreleased.

:::{note}
**Automatic Version Sorting**: When you specify multiple versions for the same product, the build system automatically sorts them in descending order (highest version first) regardless of the order you write them in the source file. For example, `stack: ga 9.1, beta 9.0, preview 8.18` will be displayed with the highest priority lifecycle and version first. Items without versions are sorted last.
:::