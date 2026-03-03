# Improved Rules Configuration Format

## Context

The `block` section in `changelog.yml` is being redesigned and renamed to `rules:`. Goals:
1. Explicit matching semantics (`any` vs `all`)
2. Per-field include/exclude modes for types and areas
3. Product overrides nested under the section they affect
4. Clear, scannable log messages prefixed with `[+include]` / `[-exclude]`
5. No backward compat — error if old `block:` key is seen

## YAML Format

```yaml
rules:
  # Global match default for multi-valued fields (labels, areas).
  #   any (default) = match if ANY item matches the list
  #   all           = match only if ALL items match the list
  # Inherited by create, publish, and all product overrides.
  # match: any

  # Create — controls which PRs generate changelog entries.
  #   exclude: block PRs with these labels (comma-separated)
  #   include: only create changelogs for PRs with these labels
  # Cannot specify both.
  #
  # create:
  #   exclude: ">non-issue, >test"
  #   # match: any
  #   products:
  #     'elasticsearch, kibana':
  #       exclude: ">test"
  #     'cloud-serverless':
  #       exclude: "ILM"

  # Publish — controls which entries appear in rendered output.
  #   exclude_types / include_types
  #   exclude_areas / include_areas
  # Cannot mix exclude_ and include_ for the same field.
  #
  # match_areas inherits from rules.match if not specified.
  #
  # publish:
  #   # match_areas: any
  #   exclude_types:
  #     - deprecation
  #     - known-issue
  #   exclude_areas:
  #     - "Internal"
  #   products:
  #     'elasticsearch, kibana':
  #       exclude_types:
  #         - docs
  #     'cloud-serverless':
  #       # match_areas: any
  #       include_areas:
  #         - "Search"
  #         - "Monitoring"
```

### Match inheritance

```
rules.match (global default, "any" if omitted)
  ├─ create.match → create.products.{id}.match
  └─ publish.match_areas → publish.products.{id}.match_areas
```

### Area matching examples

| Config | Entry areas: `["Search", "Internal"]` | Result |
|--------|--------------------------------------|--------|
| `exclude_areas: [Internal]`, match `any` | "Internal" matches | **Blocked** |
| `exclude_areas: [Internal]`, match `all` | Not all match | **Allowed** |
| `include_areas: [Search]`, match `any` | "Search" matches | **Allowed** |
| `include_areas: [Search]`, match `all` | "Internal" not in list | **Blocked** |

## Error Messages

### Validation (config parsing)

| Condition | Message |
|-----------|---------|
| Old `block:` key found | `'block' is no longer supported. Rename to 'rules'. See changelog.example.yml.` |
| Both `exclude_types` + `include_types` | `rules.publish: cannot have both 'exclude_types' and 'include_types'. Use one or the other.` |
| Both `exclude_areas` + `include_areas` | Same pattern |
| Both `create.exclude` + `create.include` | `rules.create: cannot have both 'exclude' and 'include'. Use one or the other.` |
| Invalid match value | `rules.match: '{value}' is not valid. Use 'any' or 'all'.` |
| Empty list | `rules.publish.exclude_types: list is empty. Add types or remove the field.` |
| Unknown product | `rules.publish.products: '{id}' not in available products. Available: {list}` |

### Runtime (create/publish time)

Prefixed with `[-exclude]` or `[+include]` for scanning:

**Create:**
- `[-exclude] PR #{n}: skipped, label '{label}' matches rules.create.exclude (match: {mode})`
- `[+include] PR #{n}: created, label '{label}' matches rules.create.include (match: {mode})`
- `[+include] PR #{n}: skipped, no labels match rules.create.include [{labels}] (match: {mode})`
- Product: `[-exclude] PR #{n} ({product}): skipped, label '{label}' matches rules.create.products.{product}.exclude`

**Publish:**
- `[-exclude] PR #{n}: hidden, type '{type}' in rules.publish.exclude_types`
- `[+include] PR #{n}: hidden, type '{type}' not in rules.publish.include_types`
- `[-exclude] PR #{n}: hidden, area '{area}' in rules.publish.exclude_areas (match_areas: {mode})`
- `[-exclude] PR #{n}: hidden, all areas [{areas}] in rules.publish.exclude_areas (match_areas: all)`
- `[+include] PR #{n}: hidden, areas [{areas}] not in rules.publish.include_areas (match_areas: {mode})`
- Product: same patterns with `rules.publish.products.{product}.` prefix

## Files to Modify

### 1. Domain model — enums and PublishBlocker
**`src/Elastic.Documentation/ReleaseNotes/PublishBlocker.cs`**

- Add `MatchMode` enum (`Any`, `All`)
- Add `FieldMode` enum (`Exclude`, `Include`)
- Add to `PublishBlocker`: `MatchAreas` (MatchMode), `TypesMode` (FieldMode), `AreasMode` (FieldMode)

### 2. Domain model — rename and restructure BlockConfiguration
**`src/Elastic.Documentation.Configuration/Changelog/BlockConfiguration.cs`**

Rename to `RulesConfiguration` (or new file). Structure:
- `RulesConfiguration`: `Match` (MatchMode), `Create` (CreateRules?), `Publish` (PublishRules?)
- `CreateRules`: `Labels` (list), `Mode` (FieldMode), `Match` (MatchMode?), `ByProduct` (dict)
- `PublishRules`: `PublishBlocker` fields + `ByProduct` (dict of product-specific `PublishBlocker`s)
- Delete old `ProductBlockers` record

### 3. Core blocking logic
**`src/Elastic.Documentation/ReleaseNotes/PublishBlockerExtensions.cs`**

- `MatchesType()`: type vs list
- `MatchesArea()`: any/all matching
- `ShouldBlock()`: per-field mode (`Exclude` + match → blocked; `Include` + no match → blocked)

### 4. YAML DTO (CLI path)
**`src/services/Elastic.Changelog/Serialization/ChangelogConfigurationYaml.cs`**

- Rename `BlockConfigurationYaml` → `RulesConfigurationYaml`
- New `CreateRulesYaml`: `Exclude`/`Include` (string), `Match` (string?), `Products` (dict)
- Update `PublishBlockerYaml`: `MatchAreas`, `ExcludeTypes`/`IncludeTypes`, `ExcludeAreas`/`IncludeAreas`, `Products` (dict)
- Remove old fields (`Types`, `Areas`, `Create` string, root `Product`)
- Update parent `ChangelogConfigurationYaml`: rename `Block` → `Rules`

### 5. YAML DTO (minimal/inline path)
**`src/Elastic.Documentation.Configuration/ReleaseNotes/ReleaseNotesSerialization.cs`**

Mirror changes for minimal DTOs. Rename `BlockConfigMinimalDto` → `RulesConfigMinimalDto`, etc.

### 6. Configuration parsing + validation
**`src/services/Elastic.Changelog/Configuration/ChangelogConfigurationLoader.cs`**

- Detect old `block:` key → emit error
- Parse `rules:` with new structure
- Validate mutual exclusivity, match values, empty lists
- Resolve match inheritance chain

### 7. Create blocking logic
Find where create labels are checked and update for include/exclude + match + runtime messages.

### 8. Rendering utilities
**`src/services/Elastic.Changelog/Rendering/ChangelogRenderUtilities.cs`**

- Update for new `publish.products` structure
- Add `[-exclude]` / `[+include]` prefixed runtime log messages

### 9. Example config
**`config/changelog.example.yml`** — replace `block:` section with `rules:`.

### 10. All references to BlockConfiguration
Find and update all code referencing `BlockConfiguration`, `Block`, `ProductBlockers` to use new names.

### 11. Tests

**Unit tests** (`PublishBlockerExtensionsTests.cs`):
- All mode/match combinations (exclude×any, exclude×all, include×any, include×all)
- Mixed modes (exclude_types + include_areas)
- Match inheritance (global → section → product)

**Integration tests** (`BlockConfigurationTests.cs`):
- New format end-to-end
- Validation error messages (mutual exclusivity, invalid match, old `block:` key)
- Product overrides under publish.products and create.products
- Create include/exclude + match
- Runtime message prefixes `[-exclude]` / `[+include]`

## Verification

1. New unit tests for all mode/match combinations
2. Integration tests with new config format
3. Validation error tests — verify all error messages
4. Old `block:` key → error test
5. YAML parsing on both CLI and minimal paths
6. Runtime messages at create and publish time with correct prefixes
7. Match inheritance chain works correctly
