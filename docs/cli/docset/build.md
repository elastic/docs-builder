# build

Converts a source Markdown folder or file to an output folder

## Usage

```
docs-builder [command] [options...] [-h|--help] [--version]
```

## Global Options

- `--log-level` `level`

## Options

`-p|--path <string>`
:   Defaults to the`{pwd}/docs` folder (optional)

`-o|--output <string>`
:   Defaults to `.artifacts/html` (optional)

`--path-prefix` `<string>`
:   Specifies the path prefix for urls (optional)

`--force` `<bool?>`
:   Force a full rebuild of the destination folder (optional)

`--strict` `<bool?>`
:   Treat warnings as errors and fail the build on warnings (optional)

`--allow-indexing` `<bool?>`
:   Allow indexing and following of HTML files (optional)

`--metadata-only` `<bool?>`
:   Only emit documentation metadata to output, ignored if 'exporters' is also set (optional)

`--exporters` `<IReadOnlySet<Exporter>?>`
:   Set available exporters:   html, es, config, links, state, llm, redirect, metadata, none. Defaults to (html, config, links, state, redirect) or 'default'. (optional)

`--canonical-base-url` `<string>`
:   The base URL for the canonical url tag (optional)

## Commands

`assemble`
:   Do a full assembler clone and assembler build in one swoop

`assembler bloom-filter create`
:   Generate the bloom filter binary file

`assembler bloom-filter lookup`
:   Lookup whether path exists in the bloomfilter

`assembler build`
:   Builds all repositories

`assembler clone`
:   Clones all repositories

`assembler config init`
:   Clone the configuration folder

`assembler content-source match`
:

`assembler content-source validate`
:

`assembler deploy apply`
:   Applies a sync plan

`assembler deploy plan`
:   Creates a sync plan

`assembler deploy update-redirects`
:   Refreshes the redirects mapping in Cloudfront's KeyValueStore

`assembler index`
:   Index documentation to Elasticsearch, calls `docs-builder assembler build --exporters elasticsearch`. Exposes more options

`assembler navigation validate`
:   Validates navigation.yml does not contain colliding path prefixes and all urls are unique

`assembler navigation validate-link-reference`
:   Validate all published links in links.json do not collide with navigation path_prefixes and all urls are unique.

`assembler serve`
:   Serve the output of an assembler build

`diff validate`
:   Validates redirect updates in the current branch using the redirect file against changes reported by git.

`inbound-links validate`
:   Validate all published cross_links in all published links.json files.

`inbound-links validate-all`
:   Validate all published cross_links in all published links.json files.

`inbound-links validate-link-reference`
:   Validate a locally published links.json file against all published links.json files in the registry

`index`
:   Index a single documentation set to Elasticsearch, calls `docs-builder --exporters elasticsearch`. Exposes more options

`mv`
:   Move a file from one location to another and update all links in the documentation

`serve`
:   Continuously serve a documentation folder at http://localhost:3000. File systems changes will be reflected without having to restart the server.