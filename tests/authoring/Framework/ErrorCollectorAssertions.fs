namespace authoring

open System.Diagnostics
open System.Linq
open Elastic.Markdown.Diagnostics
open FsUnitTyped
open Swensen.Unquote

[<AutoOpen>]
module DiagnosticsCollectorAssertions =

    [<DebuggerStepThrough>]
    let hasNoErrors (actual: GenerateResult) =
        test <@ actual.Context.Collector.Errors = 0 @>

    [<DebuggerStepThrough>]
    let hasError (expected: string) (actual: GenerateResult) =
        actual.Context.Collector.Errors |> shouldBeGreaterThan 0
        let errorDiagnostics = actual.Context.Collector.Diagnostics
                                   .Where(fun d -> d.Severity = Severity.Error)
                                   .ToArray()
                                   |> List.ofArray
        test <@ errorDiagnostics.FirstOrDefault().Message.Contains(expected) @>
