## Description

Create a changelog YAML for content that is not tied to a pull request.
Typical uses are known issues and security advisories.
For details and examples, go to [](/data/release-notes/create.md).

Files are named `note-{slug}.yml`. Each product lists `products[].versions` — the release versions the change applies to.

## Options

: `--products`
  Products and versions in the format `"product versions [lifecycle], ..."` where `versions` is a `|`-separated list of release versions (for example, `"elasticsearch 9.3.0|9.4.0 ga"`).
  Unlike `changelog add`, the middle slot is required and is a version list, not omitted.
  The valid product identifiers are listed in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).

: `--title`
  A short, user-facing headline (max 80 characters). Required.

: `--type`
  The type of change. For valid values, see [ChangelogEntryType.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs). Required.

: `--description`
  Additional information (max 600 characters). Optional.

: `--issues`
  URLs of related issues. Optional citation field; listing issues does not attach the file to a release.

## Product and version format

The `--products` option uses the same positional slots as `changelog add`, but the middle slot is a `|`-separated version list and is required:

- `"elasticsearch 9.3.0 ga"` — one version
- `"elasticsearch 9.3.0|9.4.0|9.5.0 ga"` — multiple versions
- `"cloud-serverless 2025-08-05"` — date-based release, one version

A changelog that spans products can declare each product separately:

```sh
docs-builder changelog note \
  --title "Known issue with aggregations" \
  --type known-issue \
  --products "elasticsearch 9.3.0|9.4.0 ga" \
  --products "kibana 9.3.0|9.4.0 ga"
```

## Output

The command writes a `note-{slug}.yml` file to the configured output directory:

```yaml
title: Known issue with aggregations
type: known-issue
products:
  - product: elasticsearch
    versions: [9.3.0, 9.4.0]
    lifecycle: ga
```

## After creation

Upload is the same as for other changelog YAML files.
An index at `changelog/{org}/{repo}/notes-{version}.json` lists every changelog "note" file that applies to each version.

If the release bundle for that product and version or date has already shipped when you upload, the scrubber generates an amend file so the changelog reaches published docs without a manual rerun.

If there is no existing or planned bundle for that product and version or date, you can create a bundle from a path list that contains all the relevant changelogs. Refer to [Bundle by file paths](/cli/changelog/bundle.md#changelog-bundle-files).

## Configuration checks

The same configuration-file checks that apply to `changelog add` apply here:
valid `products`, `lifecycles`, and `type` values are validated against `docs/changelog.yml` when it exists.

A version in `--products` for `changelog add` is an error; use `changelog note` instead.
