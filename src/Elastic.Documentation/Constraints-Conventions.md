# Constraints conventions: what a loader hides, and how the model shows it

A loader may trim, default, coerce, drop or collapse what a file wrote. The build keeps using the result; the model additionally records what was hidden, so a rule can say "the file wrote X". The loader's behaviour never changes. All of this is AOT-safe: the loader already walks the nodes, no reflection.

| The loader... | The model keeps | Rule reads |
| --- | --- | --- |
| drops keys it does not read | `IReadOnlyList<string> UnknownKeys` on the record built from that mapping, in the order written. Filled with `UnknownYamlKeys.Of(written, known...)` (Configuration/Converters). `[IgnoreEquality]`. | `Each` over the keys, one violation per key, Warning |
| coerces text into a bool or an enum (invalid text falls back to a value) | `<Member>Raw` (`string?`), the text as written; null when the model was not loaded from a file. `[IgnoreEquality]`. | the raw text parses, e.g. `Enum.GetNames<T>().Contains(raw, OrdinalIgnoreCase)` |
| defaults, trims or normalises a value | `Nullean.Entities.Authored<T>` when the member may change type; otherwise the same `<Member>Raw`. | `Raw` against the form the build expects |
| chooses between files | `SourceFileName` (`string?`) on the model | the name |
| collapses duplicate keys of a dictionary | a `DuplicateKeys` list, filled where the dictionary is built | `Unique` |
| throws on bad input | nothing: the load fails with the loader's message, there is no model. | none |

Rules for the pieces:

- A raw/unknown field never takes part in equality (`[IgnoreEquality]`) and has a default, so code that builds the model by hand needs no change and a hand-built model breaks no rule (null raw is "not authored").
- A rule about something the loader already reports (a diagnostic, an exception) is not added: one report only.
- A default that is the dominant idiom of the shipped files is not a violation; the field is not added.
- Severity of a new rule on a silent coercion is Warning, unless the catalogue says otherwise.

## Where rules live

A record's rules are declared once, in a `*Constraints` class in `Elastic.Documentation` tagged `[Constraints<T>]` (the generator collects every static `Constraint<T>` field into `All`). The record exposes them as its one static set:

```csharp
public sealed record Widget(string Name, int Size) : IHaveConstraints<Widget>
{
    public static ConstraintSet<Widget> Constraints => WidgetConstraints.All;
}
```

Everything that checks a record reads `T.Constraints`. There is no way to pass another set, and no `ConstraintSet.Merge/From/Pair` outside the domain.

**The ban.** `BannedSymbols.txt` (Microsoft.CodeAnalysis.BannedApiAnalyzers, rule RS0030, wired in `src/Directory.Build.props`) makes the rule-authoring API a compile error in every `src` project except `Elastic.Documentation`: `Constraints<T>`, `Relation`, `Tree`, the static `ConstraintSet`, and running a set by hand (`Report`, `Validate`, `Validated`, `Ensure`, `Check`). Tests are exempt. If you think a loader or service needs a rule, add it to the `*Constraints` class instead. `ConstraintSet<T>.Apply`/`ApplyDetailed` stay allowed: they are the checked form of an operation. The one sanctioned runner is `Check` (below), which lives in the domain assembly, so no project suppresses RS0030.

## Stages are types

A rule that holds only at one stage (a published entry versus one being authored) is not an option on a loader. Each stage is its own record with its own static `Constraints`, and a C# 15 `union` names the stages, so a switch must handle every one (CS8509, an error in `src`):

```csharp
public sealed record DraftWidget(string Name) : IHaveConstraints<DraftWidget> { /* Constraints => DraftWidgetConstraints.All */ }
public sealed record PublishedWidget(string Name, string Description) : IHaveConstraints<PublishedWidget> { /* ... */ }
public readonly union WidgetStage(DraftWidget, PublishedWidget);
```

A union is a struct that wraps its case: read the case through `.Value`, and write `OfCase`-style helpers rather than `OfType` over unions. A rule about two aggregates is a wrapper record with its own `Constraints`.

## Loaders: declared once

A loader is declared as a static `Loader<TDto, T>` and has one entry, `Load(text, file)`, returning a `LoadResult<T>`:

```csharp
internal static class SearchLoader
{
    public static readonly Loader<SearchDto, SearchTuning> Yaml = Loader.For(
        parse: (text, file) => ReadDto(text, file),   // the only code that touches the DTO
        map:   (dto, context) => ToRecord(dto, context));   // DTO -> record; own findings go to context.Warn / context.Hint
}
```

- `parse` and `map` are the only steps. Findings made in `map` go to the `MapContext` the loader creates per load, so they cannot be lost or reordered, and are kept (first) when a later step throws.
- The record is then checked once against `T.Constraints`. `errorsAreFatal: true` (the default, `Fatal.All`) makes an error an `Unreadable<T>`; `false` (`Fatal.None`) returns the `Loaded<T>` with its errors, for a caller that reports and continues. A load that must keep a value despite mapping errors, or that may give up in `map`, uses the `Fatal` overload: see "Tolerant loads".
- `context.Collector` is an `IDiagnosticsCollector` over the same findings, in order, so a parser written against a collector is reused unchanged; it counts only this load's severities. `context.Error(...)` records an explicit error.
- A step that throws becomes an error finding. Its text is `describe(exception)`, declared with the loader (default: the message, so a loader keeps the text it always gave). Cancellation and out-of-memory propagate.
- `value.Check(file)` checks a record built elsewhere against its `T.Constraints` and returns the `Loaded<T>` (see "Check" below).

`LoadResult<T>` is a union of `Loaded<T>` (the value and every finding, a tolerant load may still hold errors) and `Unreadable<T>` (findings only). It is not `Nullean.Entities.Result<T>`, which is the outcome of an operation. A caller writes the findings with `result.Report(collector)` (returns the same result) and then chooses `OrThrow()`, `OrThrowIfMissing()`, `ValueOrNull()` or a match on the case. `EmitTo(collector)` is `Report(collector).ValueOrNull()`. `EmitErrorsTo` writes errors only and returns null when there are some; a result's own warnings are in `Findings()` and written by `EmitTo`, not by `EmitErrorsTo`. Startup-critical configuration stops the process in one place, the composition root: `Loader.Load(...).Report(collector).OrThrow()` throws a `ConfigurationException` listing every error and carrying every finding.

A caller that owns a collector and has a `ConstraintReport` writes it with `collector.WriteViolations(report, file)`, which returns whether the value is usable (no errors).

`BufferingDiagnosticsCollector` holds diagnostics unfiltered, for a parser that writes to a collector whose findings a loader must return.

## Check: a record built elsewhere

`CheckExtensions.Check<T>(this T value, string file)` is in `Elastic.Documentation/Diagnostics`, next to `Loaded<T>`. It runs `T.Constraints.Report(value)` once and returns `Loaded<T>` with the findings as diagnostics (a rule's own file wins, otherwise `file`). Loaders call it after `map`; a service calls it on a record or stage type it built itself: `new SomeStage(record).Check(file).Findings`.

- **Why it is public.** It lives in the only assembly where running a set is allowed, and the ban covers every other project, so it must be public for a service to reach it. One generic entry replaces a public wrapper per type; the type constraint `IHaveConstraints<T>` keeps it to records that own their rules.
- **What it does not do.** It takes no rule set (a call site cannot choose, add or skip a rule). It does not decide fatality: a violated Error rule still returns `Loaded<T>`, the caller reads `HasErrors` or the findings, and a loader (`Loader.For`) is what turns errors into `Unreadable<T>`. It does not write to a collector, throw, or log. It does not map or parse.
- The ban stays in force for everything else: calling `ConstraintSet<T>.Report`/`Validate`/`Validated`/`Ensure`/`Check`, or `Constraints<T>.Always`, outside `Elastic.Documentation` fails the build (RS0030).

## Tolerant loads

The default of every loader is strict: any error makes the load `Unreadable<T>`. Some loaders on `main` are tolerant: an error made while reading does not discard the value, a rule's error does, or some loads give up explicitly. A refactor must not change that, so `Loader.For(parse, map, fatal: ...)` can name it. These modes hide errors from the outcome, so they are debt, not a feature: each use is a behaviour that is meant to become strict later.

| `Fatal` mode | Meaning | When it is legitimate | What a reader must know |
| --- | --- | --- | --- |
| `All` (default; `errorsAreFatal: true`) | Any error, made while mapping or by the constraint check, is `Unreadable<T>`. | Always; new loaders use it. | `Loaded<T>` means no error finding. |
| `CheckErrors` | Only an error from `T.Constraints` is `Unreadable<T>`. Errors made while mapping (`context.Error`, errors written to `context.Collector`) are reported and the value is kept. | The loader on `main` reported mapping errors and continued, but its rules were always fatal, and a snapshot or test locks that. | `Loaded<T>` can hold error findings. Do not branch on "loaded means clean": use `HasErrors`. |
| `None` (`errorsAreFatal: false`) | An error is never fatal; the value is returned with every finding. Only a `null` from `map` or an exception is `Unreadable<T>`. | The caller on `main` reports and continues whatever the file holds (a docset). | As above, and a rule's Error severity changes nothing about the outcome. |

Other tolerant-load facts, true in every mode:

- `map` may return `null` through the `TryMap` overload: the load is `Unreadable<T>` with the findings made so far and the check does not run. Use it only where the hand-rolled loader gave up the same way.
- Findings are ordered: findings made while mapping (`Warn`, `Hint`, `Error`, collector writes), then the check's.
- A step that throws keeps its behaviour in every mode: `Unreadable<T>` with the findings made so far, then the described error.

**Every use of `Fatal.CheckErrors` or `Fatal.None` carries a `// TOLERANT LOAD:` comment** directly above it, naming (1) the behaviour on `main` it preserves, (2) the snapshot or test that locks it, and (3) what the strict behaviour would be once the lock is lifted (the deferred behaviour change, to list in the PR). `TolerantLoadCommentTests` fails when a source file uses a non-default mode without that comment; it checks the comment is there, not that it is true. Find the debt with `grep -rn "TOLERANT LOAD:" src`.

```csharp
// illustrative; the names are made up
// TOLERANT LOAD: (1) main reports an unknown product and still uses the file;
// (2) locked by ChangelogConfigurationSnapshotTests.Unknown_product; (3) strict would be Fatal.All: the file is rejected.
public static readonly Loader<Dto, Config> Yaml = Loader.For(Parse, Map, Fatal.CheckErrors);
```

Loaders known to be tolerant today on `main` (they need these modes when moved onto `Loader.For`): `changelog.yml` (mapping errors keep the value, the check is fatal, `block:` gives up), the codex configuration, and the links file. Each is confirmed against its own snapshots when it moves. Add a loader here when it joins.

## Operations: methods on the record, checked by the set

A change to a record is a public method that returns the record (`widget.Grow()`, `widget.Rename(name)`). The checked form is the set's own `T.Constraints.Apply(current, r => r.Grow())` (`ApplyDetailed` where "nothing happened" matters). `ApplyDetailed(...).ToDiagnostics(file)` turns the outcome into findings, including the warnings `Apply` and `Validated` drop. There is no operation wrapper class.

Operations are verified, not hand-tested: `AutoTest.For(WidgetConstraints.Instance, gen)` (Nullean.Entities.CsCheck) runs every public method returning `T` (and every one taking a single argument) against every rule, one generated test per kind of rule. A method returning `Result<T>` is not discovered. An operation that can open a state a rule forbids fails with the rule, the operation and the smallest input. The generated values must satisfy every rule, and each axis fails as vacuous when the data never reaches it: generate inputs on both sides of an idempotent operation. See `WidgetAutoTests`.

## Equality

Records holding collections or file handles compare those by reference unless told otherwise. They carry Generator.Equals' `[Equatable]` and name a comparer per member; the comparers live in `Elastic.Documentation.Equality` (`FileSystemInfoLocationComparer`: a file is its full path; `StringListEntryComparer`: dictionary entries whose values are string lists).

## Testing

Property tests use [CsCheck](https://github.com/AnthonyLloyd/CsCheck) with its **default random seed**: every run explores new inputs, so a rule or generator weakness that one seed hides is found by a later run. A failure prints its seed (`Set seed: "..." or -e CsCheck_Seed=... to reproduce`), and CsCheck reads the `CsCheck_Seed` environment variable to start from a given seed (`CsCheck_Seed=<seed> mise run test`). `AutoTest` defaults to a fixed seed, so a test passes one explicitly with `.Seed(RunSeed.Value)`: random per run, or `CsCheck_Seed` when set; its failure prints a `.Seed("...")` to replay.

Never pin a seed in a test. **Every bug a property test finds gets a pinned regression case**: a plain example test with the shrunk input it printed, in the same class, so the fix cannot regress and nobody has to rediscover it by luck. Each property test carries a doc comment with the law and the consequence if it is broken, because a generated input is invisible and the comment is the only place a reader learns what is guaranteed.

`DomainPurityTests` lists the bounded-context namespaces of the domain model: they hold data, not machinery (no YAML, logging, diagnostics collectors or raw `System.IO` handles). A change that adds a namespace adds it to that list.
