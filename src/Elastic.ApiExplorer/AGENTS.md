# Elastic.ApiExplorer

Renders API reference documentation (landing, operation, and schema type pages) from OpenAPI
specifications, using Microsoft.OpenAPI's normalized object model as input and RazorSlices for HTML.

## Structure

Vertical slices (one folder per page feature) over MVVM-named horizontal layers:

```
OpenApiGenerator.cs      Entry point: reads specs, builds navigation, renders every page to disk.
_Layout.cshtml           Razor conventions — stay at the project root.
_ViewImports.cshtml

Model/                   The semantic layer over Microsoft.OpenAPI. Spec loading (OpenApiReader),
                         Elastic x-* extension parsing (OpenApiExtensionReader), schema
                         interpretation ($ref resolution, allOf flattening, type classification:
                         SchemaAnalyzer, TypeInfo, SchemaHelpers). No HTML, no URLs, no views.
Infrastructure/          Shared rendering plumbing every page needs: ApiRenderContext, ApiViewModel
                         (base layout view model), ApiMarkdown, ApiUrlBuilder (all URL monikers),
                         IApiModel ("model renders itself" contract), availability badges,
                         SectionHeader/ApiCodeBlockModel partial models.
Navigation/              Navigation tree assembly (ApiNavigationBuilder) and nav helpers.

Components/              Reusable View+ViewModel widgets embedded by more than one page.
  PropertyTree/          The collapsible property listing: ApiProperty view-model tree,
                         ApiPropertyTreeBuilder (maps Model → view models), TypeAnnotation.
    _Partials/           Its templates: _PropertyItem, _PropertyList, _UnionOptions, _SchemaType,
                         _ValidationConstraints, _RecursiveBadge.

Landing/                 Slice: product landing, tag landing and intro/outro markdown pages.
Operations/              Slice: operation pages (ApiOperation/ApiEndpoint, page model, view).
Types/                   Slice: schema type pages under /api/{product}/types/.

_Partials/               Cross-slice partials only: _SectionHeader, _ApiCodeBlock, _AppliesToBadge;
                         Layout/_ApiToc. View-only — partial models live in Infrastructure/.

Export/                  OpenApiDocumentExporter: search-index export, independent of page rendering.
```

## Rules

**MVVM mapping.** Each slice is a mini-MVVM stack: a model record (`ApiOperation`, `ApiSchema`,
`ApiTag`) that wraps and exposes the raw Microsoft.OpenAPI object, a page model built in
`RenderAsync` *before* the slice runs, and a view. `Model/` is the shared model semantics,
`Infrastructure/` the shared services, `Components/` shared View+ViewModel widgets.

**Layering.** `Model/` and `Infrastructure/` never reference components or slices.
`Components/*` may use both layers but no slice. Slices may use everything. Never backwards.

**Proxy over copying.** View models do not copy scalars Microsoft.OpenAPI already exposes — they
wrap the raw object (`ApiProperty.Schema`) and views read scalars off it verbatim. View models own
*structure and decisions*: anything requiring $ref resolution, classification, recursion detection,
URL building or markdown rendering is precomputed at build time.

**Views only iterate and print.** No `@{ }` decision blocks, no walking
`schema.Properties/OneOf/Items`, no JsonNode parsing, no StringBuilder HTML in `.cshtml`.
`@switch`/`@if` on precomputed view-model fields is fine.

**Partials.** `_Partials/` folders are strictly view-only. A partial used by a single slice lives in
that slice's `_Partials/`; only genuinely cross-slice partials live in the root `_Partials/`.

## Where do I put X?

| Change | Location |
|---|---|
| New page type | New slice folder: model record + nav item + page model + view |
| New `x-*` extension | `Model/OpenApiExtensionReader.cs` |
| Schema interpretation ($ref, allOf, type classification) | `Model/SchemaAnalyzer.cs` / `TypeInfo` |
| How property rows display (badges, collapse, unions) | `Components/PropertyTree/` |
| New shared widget used by 2+ pages | New folder under `Components/` |
| URL/moniker scheme | `Infrastructure/ApiUrlBuilder.cs` |
| Navigation tree shape | `Navigation/ApiNavigationBuilder.cs` |
| Search export | `Export/` |

## Testing

`tests/Elastic.ApiExplorer.Tests` exercises the view-model builders against a hand-written fixture
spec (`TestData/api-explorer-fixture.json`) covering unions, `X | X[]` simple unions, dictionaries,
recursion, enums, allOf and the Elastic `x-*` extensions; `ApiExplorerFixture` loads the spec and
builds its navigation tree once per test class.

During the MVVM refactor a byte-for-byte HTML snapshot harness guarded every change (render fixture
pages through the production path, diff against checked-in reference HTML). It was removed once the
refactor landed to avoid freezing the current markup; if you undertake another must-not-change-output
refactor, resurrect that approach first.
