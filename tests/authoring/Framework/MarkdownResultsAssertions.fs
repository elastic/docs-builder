// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System.Diagnostics
open System.Text.Json
open DiffPlex.DiffBuilder
open DiffPlex.DiffBuilder.Model
open FsUnit.Xunit
open JetBrains.Annotations
open Xunit.Sdk
open System.IO

[<AutoOpen>]
module ResultsAssertions =

    let diff expected actual =
        let diffLines = InlineDiffBuilder.Diff(expected, actual).Lines

        let mutatedCount =
            diffLines
            |> Seq.filter (fun l ->
                match l.Type with
                | ChangeType.Modified -> true
                | ChangeType.Inserted -> true
                | ChangeType.Deleted -> true
                | _ -> false
            )
            |> Seq.length

        let actualLineLength = actual.Split("\n").Length
        match mutatedCount with
        | 0 -> ""
        | _ when mutatedCount >= actualLineLength -> $"Mutations {mutatedCount} on all {actualLineLength} showing actual: \n\n{actual}"
        | _ ->
            diffLines
            |> Seq.map(fun l ->
                match l.Type with
                | ChangeType.Deleted -> "- " + l.Text
                | ChangeType.Modified -> "+ " + l.Text
                | ChangeType.Inserted -> "+ " + l.Text
                | _ -> " " + l.Text
            )
            |> String.concat "\n"


    [<DebuggerStepThrough>]
    let converts file (results: Lazy<GeneratorResults>) =
        let results = results.Value

        let result =
            results.MarkdownResults
            |> Seq.tryFind (fun m -> m.File.RelativePath = (string file).Replace('/', Path.DirectorySeparatorChar))

        match result with
        | None ->
            raise (XunitException($"{file} not part of the markdown results"))
        | Some result -> result

[<AutoOpen>]
module JsonAssertions =

    [<DebuggerStepThrough>]
    let convertsToJson artifact ([<LanguageInjection("json")>]expected: string) (actual: Lazy<GeneratorResults>) =
        let actual = actual.Value
        let fs = actual.Context.ReadFileSystem

        let fi = fs.FileInfo.New(artifact)
        if not <| fi.Exists then
            raise (XunitException($"{artifact} is not part of the output"))

        let actual = fs.File.ReadAllText(fi.FullName)
        use actualJson = JsonDocument.Parse(actual);
        let actual = JsonSerializer.Serialize(actualJson, JsonSerializerOptions(WriteIndented = true))

        use expectedJson = JsonDocument.Parse(expected);
        let expected = JsonSerializer.Serialize(expectedJson, JsonSerializerOptions(WriteIndented = true))

        diff expected actual |> should be NullOrEmptyString


