// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
        let message = errorDiagnostics.FirstOrDefault().Message
        test <@ message.Contains(expected) @>
