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

## Loader pipeline: Parse + Map + Rules

Every loader is three things and nothing else, run through `LoaderPipeline` (Elastic.Documentation.Configuration/Loading):

```csharp
public static Result<SearchTuning> Load(ConfigurationFileProvider provider) =>
    LoaderPipeline.Run(
        "search.yml",
        parse: ReadDto,                       // the only code that touches the DTO (internal)
        map: ToRecord,                        // DTO -> anemic record; findings of its own go in `extra`
        rules: SearchTuningConstraints.All);  // the aggregate's set, from Elastic.Documentation
```

- `Run` catches a step that throws and returns it as an error diagnostic carrying the exception's message, runs `rules.Report` exactly once, and returns `Result<T>` (an error means no value; `errorsAreFatal: false` returns the value with its errors for a caller that reports and continues). `Check` is the same last step for a record built elsewhere; `Build` only builds, for a part whose rules relate it to another part.
- `BufferingDiagnosticsCollector` holds diagnostics in emission order, unfiltered, so a loader whose parser writes to a collector can return them in its `Result<T>` and the caller's collector sees what a direct write would have produced.

## Loader recipe: build, check once, hand the result on

A loader is the one place that checks the entity it builds; nothing downstream checks it again.

1. Build the entity. When a rule relates two things, the loader builds the pair record itself.
2. `var report = Set.All.Report(entity);` runs every rule once and reports all of them, not the first.
3. Hand the report on: `Result.From(report, file)` (carries every diagnostic; `HasErrors` is true when a rule of Error severity was broken) or, where the loader owns a collector, `collector.Write(report, file)`, which returns whether the entity is usable. Both go through `ConstraintReport<T>.ToDiagnostics`, the only violation-to-diagnostic mapping, so every diagnostic carries the rule's id as `Diagnostic.RuleId`. Match on that, never on message text.
4. Never check in a callee. A caller of a loader writes the findings with `result.Report(collector)`, which returns the same `Result<T>`, and then chooses: `.OrThrow()`, `.Value`, or a match on `HasErrors`. `EmitTo(collector)` is shorthand for `Report(collector).Value`.
5. Startup-critical configuration stops the process in one place, the composition root: `Loader.Load(...).Report(collector).OrThrow()` throws a `ConfigurationException` whose message lists every error and which carries every diagnostic.

## Domain operations: derive, then re-check

A record that is not read from a file is made by a **domain operation** (`Elastic.Documentation.Domain.DomainOperation`), never by `new` or `with` in a service.

- **Pure.** No IO, no clock (the caller passes `today`), no collector. A finding is a diagnostic in the result, in the order it was found.
- **Returns `Result<T>`** (`Elastic.Documentation.Diagnostics`), the record checked by the aggregate's set.
- **Built, then derived.** `DomainOperation.Built` is for a record made from facts that were never in a file: the result carries every finding of the set. `Derived` is for a record made from one that was already checked: the set runs again, but only the errors are in the result, because the warnings the source carried were reported when it was read. A caller of `Derived` writes `result.EmitErrorsTo(collector)`.
- **The value rides along.** `Result.From` keeps the value even when a rule of Error severity is broken, so a caller that tolerates the finding can still use it; `EmitErrorsTo` returns null instead.

## Equality

Records holding collections or file handles compare those by reference unless told otherwise. They carry Generator.Equals' `[Equatable]` and name a comparer per member; the comparers live in `Elastic.Documentation.Equality` (`FileSystemInfoLocationComparer`: a file is its full path; `StringListEntryComparer`: dictionary entries whose values are string lists).

## Testing

Property tests use [CsCheck](https://github.com/AnthonyLloyd/CsCheck) with a **fixed seed** (`Props.Check`: `Props.Seed`, 200 iterations, one thread, so a run is the same run and a failure prints a shrunk input). Each property test carries a doc comment with the law and the consequence if it is broken, because a generated input is invisible and the comment is the only place a reader learns what is guaranteed.

`DomainPurityTests` lists the bounded-context namespaces of the domain model: they hold data, not machinery (no YAML, logging, diagnostics collectors or raw `System.IO` handles). A change that adds a namespace adds it to that list.
