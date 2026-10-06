# Constraints conventions: what a loader hides, and how the model shows it

A loader may trim, default, coerce, drop or collapse what a file wrote. The build keeps using the result; the model additionally records what was hidden, so a rule can say "the file wrote X". The loader's behaviour never changes. All of this is AOT-safe: the loader already walks the nodes, no reflection.

| The loader... | The model keeps | Rule reads |
| --- | --- | --- |
| drops keys it does not read | `IReadOnlyList<string> UnknownKeys` on the record built from that mapping, in the order written. Filled with `UnknownYamlKeys.Of(written, known...)` (Configuration/Converters). `[IgnoreEquality]`. | `Each` over the keys, one violation per key, Warning |
| coerces text into a bool or an enum (invalid text falls back to a value) | `<Member>Raw` (`string?`), the text as written; null when the model was not loaded from a file. `[IgnoreEquality]`. | the raw text parses, e.g. `Enum.GetNames<T>().Contains(raw, OrdinalIgnoreCase)` |
| defaults, trims or normalises a value | `Nullean.Entities.Authored<T>` when the member may change type; otherwise the same `<Member>Raw`. | `Raw` against the form the build expects |
| chooses between files | `SourceFileName` (`string?`) on the model | the name |
| collapses duplicate keys of a dictionary | a `DuplicateKeys` list, filled where the dictionary is built (none needed yet) | `Unique` |
| throws on bad input | nothing: the load fails with the loader's message, there is no model. The row stays with the loader. | none |

Rules for the pieces:

- A raw/unknown field never takes part in equality (`[IgnoreEquality]`) and has a default, so code that builds the model by hand needs no change and a hand-built model breaks no rule (null raw is "not authored").
- A rule about something the loader already reports (a diagnostic, an exception) is not added: one report only. Log the row as a gap instead.
- A default that is the dominant idiom of the shipped files (`apm-server:` with no settings, `versioning` omitted because it equals the product id) is not a violation; the field is not added.
- Severity of a new rule on a silent coercion is Warning, unless the catalogue says otherwise.

## Loader recipe: build, check once, hand the result on

A loader is the one place that checks the entity it builds; nothing downstream checks it again.

1. Build the entity. When a rule relates two things, the loader builds the pair record itself, e.g. `new DocumentationSetInCatalog(docset, products)`.
2. `var report = Set.All.Report(entity);` runs every rule once and reports all of them, not the first.
3. Hand the report on: `Result.From(report, file)` (carries every diagnostic; `HasErrors` is true when a rule of Error severity was broken) or, where the loader owns a collector, `collector.Write(report, file)`, which returns whether the entity is usable. Both go through `ConstraintReport<T>.ToDiagnostics`, the only violation-to-diagnostic mapping, so every diagnostic carries the rule's id as `Diagnostic.RuleId`. Match on that, never on message text.
4. Drop items only through the report, by the typed rule handle: `report.Keep(items, Set.SomeRule)` or `report.Rejected(Set.SomeRule)`, never by comparing a `ConstraintId` string. Only an Error rejects an item; a Warning never drops one.
5. Never check in a callee. A caller of a loader uses `result.EmitTo(collector)` and `result.HasErrors`; it does not run the set again. The `Check(...)` one-liners on the sets are gone: use `Set.All.Report(...)`.
6. Startup-critical configuration stops the process in one place, the composition root: `Loader.Load(...).OrThrow()` throws a `ConfigurationException` whose message lists every error and which carries every diagnostic.

A rule is not wired into a loader when something already reports the same problem with a different message or severity, or when the rule is stage dependent (its description says so): wiring it would report twice or report at the wrong stage. Log the row as a gap instead.
