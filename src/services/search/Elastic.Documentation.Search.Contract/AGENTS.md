# Elastic.Documentation.Search.Contract

The shared document/mapping/analysis schema consumed by **two independent indexers**: the docs
build pipeline (`Elastic.Markdown/Exporters/Elasticsearch/ElasticsearchMarkdownExporter.cs`) and
`essc` (`src/tooling/essc/`). Nothing here should assume which one is calling it — that's the whole
point of the project existing separately instead of living inside either.

## Structure

```
Common/       Shared plumbing: ISearchDocument, the polymorphism wiring, mapping/analysis config
              shared across document types, content-tier and synonym types.
Docs/         DocumentationDocument — docs-builder's own indexed pages.
Site/         SiteDocument — elastic.co website content.
Labs/         LabsDocument — Labs content source.
Guide/        GuideDocument — Guide content source.
WebsiteSearch/ WebsiteSearchDocument — the unified cross-source search index.
Autocomplete/ Request/response types for autocomplete, not a document type.
Search/       ISearchService and query/aggregation request/response types.
```

One folder per document type/concern; each document type owns its own `*MappingConfig.cs` and
composes `Common/SharedMappingConfig.cs`'s shared field mappings.

Folder layout is concern-based but the project **deliberately exposes only two namespaces** — root
and `.Mapping` — not one per folder; the `.csproj` suppresses `IDE0130` (namespace-doesn't-match-
folder) for exactly this reason. Don't "fix" that warning by adding folder-matching namespaces.

## Rules

**AOT-safe polymorphism, not reflection.** All document types are declared as
`[JsonDerivedType(typeof(X), "discriminator")]` directly on `ISearchDocument` (`Common/ISearchDocument.cs`)
with string discriminators (`"site"`, `"labs"`, `"guide"`, `"website"`, `"docs"`), and
`Common/SearchDocumentPolymorphism.cs` builds the equivalent config into `JsonSerializerOptions` for
callers that need it dynamically. Adding a document type means adding both a `[JsonDerivedType]`
attribute *and* registering it in whichever `JsonSerializerContext` serializes it — this project is
`IsAotCompatible`, and reflection-based discovery isn't an option.

**Analyzer/normalizer names must match across two files, by string, not by reference.**
`Common/SharedMappingConfig.cs` declares name *constants* (`KeywordNormalizer = "keyword_normalizer"`,
`StartsWithAnalyzer`, `HierarchyAnalyzer`, `SynonymsAnalyzer`, ...); `Common/SharedAnalysisFactory.cs`
builds the actual Elasticsearch analyzer/normalizer definitions using **raw string literals**, not
the constants. If you rename one side, you silently break the mapping↔analysis link with no compiler
error — grep for the literal string on both sides before renaming anything here.

**Schema/discriminator stability is a rule, not a preference.** Both consumers reindex from these
types; an incompatible change to a document type or a discriminator value breaks one indexer without
necessarily breaking the other's build. Bump the contract version (see `essc`/docs-builder normalization
history) rather than changing a shipped shape in place.

## Where do I put X?

| Change | Location |
|---|---|
| New document type for a new content source | New folder + `*Document.cs` + `*MappingConfig.cs`, register discriminator in `Common/ISearchDocument.cs` |
| Field shared by every document type | `Common/SharedMappingConfig.cs` (mapping) — keep `SharedAnalysisFactory.cs` in sync by string |
| New analyzer/normalizer | `Common/SharedAnalysisFactory.cs`, name constant in `SharedMappingConfig.cs` |
| Search/autocomplete request or response shape | `Search/` or `Autocomplete/` |
| Content-tier / synonym logic shared across sources | `Common/ContentTiers.cs` / `Common/IndexTimeSynonyms.cs` |

## Related docs

`docs/development/essc.md` covers the operator/config side (credentials, sources, container image);
this file is the code-facing contract rules that doc doesn't repeat.
