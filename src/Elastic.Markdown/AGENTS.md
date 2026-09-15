# Elastic.Markdown

The core Markdown parser: a Markdig extension that adds Myst-style directives and roles, plus the
diagnostics, linting, and export machinery that turns a directory of `.md` files into rendered
pages. Everything else in this repo (the CLI, the frontend, essc) consumes this project's output.

## Structure

```
Myst/
  Directives/            Block-level directives ({admonition}, {tabs}, {dropdown}, ...). One
                          folder per directive, typically Block.cs + View.cshtml + ViewModel.cs.
  Roles/                 Inline analog of directives ({applies_to}, {kbd}, {math}, icons). Same
                          idea, much smaller surface (Role.cs, RoleParser.cs, one folder per role).
  FrontMatter/            YAML front matter parsing.
  AppliesTo/              The applies-to versioning/compatibility model shared by directives+roles.
  CodeBlocks/             Fenced code block handling (substitutions, callouts, console/copy UI).
  InlineParsers/          Custom inline Markdig parsers (substitutions, cross-doc links, etc).
  Linters/                Structural lint rules over the parsed document tree.
  Components/             Shared Myst-level building blocks used by multiple directives/roles.
  Renderers/              HTML rendering glue for the extension.
Diagnostics/              Build-time diagnostics (broken links, missing includes, etc) — separate
                          from Myst/Linters, which is about document-structure rules.
Exporters/                Turn a built doc set into something else: plain text, LLM-friendly
                          markdown, or Elasticsearch documents (Exporters/Elasticsearch/ —
                          feeds the shared Elastic.Documentation.Search.Contract schema).
Extensions/               Cross-cutting Markdown extensions (CLI reference, detection rules).
IO/                       File system abstractions.
Page/                     Assembled-page model consumed by the CLI/site.
Layout/                   Shared Razor layout partials.
```

## Directives: the pattern, and where it bends

Most directives are a 3-file folder under `Myst/Directives/<Name>/`: `<Name>Block.cs` (the Markdig
block type), `<Name>View.cshtml` (Razor rendering), `<Name>ViewModel.cs` (data the view reads). Some
have more files (e.g. `Tabs/`, `Stepper/`, `Image/` render multiple sub-elements), and at least one
**composes rather than duplicates**: `DropdownBlock` is a one-line subclass of `AdmonitionBlock`
(`Admonition/AdmonitionBlock.cs`) rather than its own implementation — check whether a new directive
is really a variant of an existing one before writing a fresh block type.

**Dispatch is a plain `if` chain, not a lookup table.** `DirectiveBlockParser.CreateFencedBlock`
(`Myst/Directives/DirectiveBlockParser.cs`) matches the directive name string via a sequence of
`if (info.IndexOf("{name}") > 0) return new XBlock(...)` checks. The `FrozenDictionary` in that same
file (`UnsupportedBlocks`) is only the legacy/removed-directive tracking list, not the active
registration mechanism — don't assume adding a directive means adding to a dictionary.

## Adding a new directive: the full cross-project checklist

A new directive ripples outside this project. In order:

1. `Myst/Directives/<Name>/` — `<Name>Block.cs` + `<Name>View.cshtml` + `<Name>ViewModel.cs`.
2. Wire it into `DirectiveBlockParser.CreateFencedBlock`'s `if` chain.
3. `src/Elastic.Documentation.Site/Assets/markdown/<name>.css` — directive CSS lives in the frontend
   project, named after the directive (see `admonition.css`, `dropdown.css`, `button.css` there).
4. Optional interactive behavior → TS in `src/Elastic.Documentation.Site/Assets/`.
5. Document the syntax in `docs/syntax/directives.md`.

Missing step 3 or 5 is the most common way a directive works but looks broken or undocumented.

## Testing

`tests/Elastic.Markdown.Tests/` — base classes in `Inline/InlneBaseTests.cs` (`InlineTest`,
`LeafTest<T>`, `BlockTest<T>`, `InlineTest<T>`) parse a raw markdown string against a
`MockFileSystem` fixture built by `TestHelpers.CreateConfigurationContext(...)` and expose `Html`/
`Document`/`Collector` for assertions. Subclass the matching base, pass markdown via a primary
constructor, assert with AwesomeAssertions (`Html.Should().Contain(...)`). Run with
`dotnet test tests/Elastic.Markdown.Tests/`.
