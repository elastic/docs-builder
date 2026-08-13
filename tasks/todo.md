# #718 api: schema — task checklist

## Task 1: Rebase and split stack

- [x] Rebase `issue-718-api-schema` onto `origin/main`
- [x] Commit full WIP to `issue-719-version-index-client`
- [x] Remove `VersionIndexClient` and client tests from #718 branch

## Task 2: Schema model and converter

- [x] `ApiProductEntry` with `spec`, `product`, `repository`, `children`
- [x] Reject legacy YAML shapes with migration guidance
- [x] Line/column attribution for diagnostics

## Task 3: Resolve-time validation

- [x] Validate `product:` against `products.yml`
- [x] Validate `repository:` as `org/repo`
- [x] Resolve `children:` under `api/<key>/`
- [x] Optional `LocalSpecFile` when spec path missing on disk

## Task 4: Navigation and generator

- [x] Prepend `children:` in `ApiNavigationBuilder`
- [x] Exclude child paths in `DocumentationGenerator`
- [x] Watch children + local spec in `ReloadableGeneratorState`
- [x] `OpenApiGenerator` uses local spec only; warn when absent

## Task 5: Migration and verification

- [x] Migrate `docs/_docset.yml` (3 keys)
- [x] Move `kibana-api-overview.md` to `docs/api/kibana/`
- [x] Update `docs/data/openapi/api-explorer.md`
- [x] `dotnet test tests/Elastic.Documentation.Configuration.Tests/`
- [x] `dotnet test tests/Elastic.ApiExplorer.Tests/` (nav/reader)
- [x] `dotnet build`

## Checkpoint: #718 complete

- [x] All acceptance criteria met for config schema
- [ ] PR opened for #718

## Task 6: #719 stack (follow-up)

- [ ] Rewrite `VersionIndexClient` for live index contract
- [ ] Open stacked PR targeting #718 branch
