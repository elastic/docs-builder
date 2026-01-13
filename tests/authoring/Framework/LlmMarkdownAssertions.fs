// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System
open System.Diagnostics
open System.IO
open System.Text
open Elastic.Documentation.AppliesTo
open Elastic.Markdown.Exporters
open Elastic.Markdown.Myst.Components
open Elastic.Markdown.Myst.Renderers.LlmMarkdown
open JetBrains.Annotations
open Xunit.Sdk

[<AutoOpen>]
module LlmMarkdownAssertions =

    let toLlmMarkdown (actual: MarkdownResult) =
        use writer = new StringWriter()  
        LlmMarkdownExporter.ConvertToLlmMarkdown(actual.Document, actual.Context.Generator.Context).Trim()
    
    /// Creates the full LLM output with metadata (title, applies_to, etc.)
    let toLlmMarkdownWithMetadata (actual: MarkdownResult) =
        let sourceFile = actual.File
        let buildContext = actual.Context.Generator.Context
        let llmBody = LlmMarkdownExporter.ConvertToLlmMarkdown(actual.Document, buildContext).Trim()
        
        let metadata = StringBuilder()
        metadata.AppendLine("---") |> ignore
        metadata.AppendLine($"title: {sourceFile.Title}") |> ignore
        
        // Add applies_to if present
        let appliesTo = sourceFile.YamlFrontMatter
        match appliesTo with
        | null -> ()
        | appliesTo ->
            match appliesTo.AppliesTo with
            | null -> ()
            | appliesTo ->
                if appliesTo <> ApplicableTo.All && appliesTo <> ApplicableTo.Default then
                    let viewModel = ApplicableToViewModel(
                        AppliesTo = appliesTo,
                        Inline = true,
                        ShowTooltip = true,
                        VersionsConfig = buildContext.VersionsConfiguration
                    )
                    let items = viewModel.GetApplicabilityItems()
                    if items.Count > 0 then
                        metadata.AppendLine("applies_to:") |> ignore
                        for item in items do
                            let displayName = item.ApplicabilityDefinition.DisplayName.Replace("&nbsp;", " ")
                            let popoverData = item.RenderData.PopoverData
                            let availabilityText =
                                match popoverData with
                                | NonNull popoverData when popoverData.AvailabilityItems.Length > 0 ->
                                    String.Join(", ", popoverData.AvailabilityItems |> Array.map _.Text)
                                | _ -> "Available"

                            metadata.AppendLine($"  - {displayName}: {availabilityText}") |> ignore
        
        metadata.AppendLine("---") |> ignore
        metadata.AppendLine() |> ignore
        metadata.AppendLine($"# {sourceFile.Title}") |> ignore
        metadata.Append(llmBody) |> ignore
        metadata.ToString().Trim()

    [<DebuggerStepThrough>]
    let convertsToNewLLM ([<LanguageInjection("markdown")>]expected: string) (actual: Lazy<GeneratorResults>) =
        let results = actual.Value
        let defaultFile = results.MarkdownResults |> Seq.find (fun r -> r.File.RelativePath = "index.md")
        let actualLLM = toLlmMarkdown defaultFile
        let expectedWithTitle = $"{expected}".Trim()
        let difference = diff expectedWithTitle actualLLM
        match difference with
        | s when String.IsNullOrEmpty s -> ()
        | d ->
            let msg = $"""LLM text was not equal
-- DIFF --
{d}

-- EXPECTED --
{expectedWithTitle}

-- ACTUAL --
{actualLLM}
"""
            raise (XunitException(msg))
    
    [<DebuggerStepThrough>]
    let convertsToLlmWithMetadata ([<LanguageInjection("markdown")>]expected: string) (actual: Lazy<GeneratorResults>) =
        let results = actual.Value
        let defaultFile = results.MarkdownResults |> Seq.find (fun r -> r.File.RelativePath = "index.md")
        let actualLLM = toLlmMarkdownWithMetadata defaultFile
        let expectedTrimmed = expected.Trim()
        let difference = diff expectedTrimmed actualLLM
        match difference with
        | s when String.IsNullOrEmpty s -> ()
        | d ->
            let msg = $"""LLM metadata output was not equal
-- DIFF --
{d}

-- EXPECTED --
{expectedTrimmed}

-- ACTUAL --
{actualLLM}
"""
            raise (XunitException(msg))
