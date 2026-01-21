`applies_to` accepts the following version formats:

### Version specifiers

You can use version specifiers to precisely control how versions are interpreted:

| Specifier | Syntax | Description | Example |
|-----------|--------|-------------|---------|
| Greater than or equal (default) | `x.x` `x.x+` `x.x.x` `x.x.x+` | Feature available from this version onwards | `ga 9.2+` or `ga 9.2` |
| Range (inclusive) | `x.x-y.y` `x.x.x-y.y.y` | Feature available only in this version range | `beta 9.0-9.1` |
| Exact version | `=x.x` `=x.x.x` | Feature available only in this specific version | `preview =9.0` |

Regardless of the version format used in the source file, the version number is always rendered in the `Major.Minor` format in badges.

:::{note}
The `+` suffix is optional for greater-than-or-equal syntax. Both `ga 9.2` and `ga 9.2+` have the same meaning.
:::

### Examples

```yaml
# Greater than or equal (feature available from 9.2 onwards)
stack: ga 9.2
stack: ga 9.2+

# Range (feature was in beta from 9.0 to 9.1, then became GA)
stack: ga 9.2+, beta 9.0-9.1

# Exact version (feature was in preview only in 9.0)
stack: ga 9.1+, preview =9.0
```

### Implicit version inference for multiple lifecycles {#implicit-version-inference}

When you specify multiple lifecycles with simple versions (without explicit specifiers), the system automatically infers the version ranges:

**Input:**
```yaml
stack: preview 9.0, alpha 9.1, beta 9.2, ga 9.4
```

**Interpreted as:**
```yaml
stack: preview =9.0, alpha =9.1, beta 9.2-9.3, ga 9.4+
```

The inference rules are:
1. **Consecutive versions**: If a lifecycle is immediately followed by another in the next minor version, it's treated as an **exact version** (`=x.x`).
2. **Non-consecutive versions**: If there's a gap between one lifecycle's version and the next lifecycle's version, it becomes a **range** from the start version to one version before the next lifecycle.
3. **Last lifecycle**: The highest versioned lifecycle is always treated as **greater-than-or-equal** (`x.x+`).

This makes it easy to document features that evolve through multiple lifecycle stages. For example, a feature that goes through preview → beta → GA can be written simply as:

```yaml
stack: preview 9.0, beta 9.1, ga 9.3
```

Which is automatically interpreted as:
```yaml
stack: preview =9.0, beta 9.1-9.2, ga 9.3+
```

:::{note}
**Automatic Version Sorting**: When you specify multiple versions for the same product, the build system automatically sorts them in descending order (highest version first) regardless of the order you write them in the source file. For example, `stack: ga 9.1, beta 9.0, preview 8.18` will be displayed with the highest priority lifecycle and version first. Items without versions are sorted last.
:::