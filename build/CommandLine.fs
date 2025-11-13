// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module CommandLine

open Argu
open Microsoft.FSharp.Reflection
open System
open Bullseye

type TestSuite = All | Unit | Integration
    with
    member this.SuitName =
        match FSharpValue.GetUnionFields(this, typeof<TestSuite>) with
        | case, _ -> case.Name.ToLowerInvariant()

type FormatArgs =
    | [<MainCommand>] Include of string list
    with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Include _ -> "Specify files to include in format, passed to dotnet format --include"

type LintArgs =
    | [<MainCommand>] Include of string list
    with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Include _ -> "Specify files to include in lint check, passed to dotnet format --include"

type Build =
    | [<CliPrefix(CliPrefix.None);SubCommand>] Clean
    | [<CliPrefix(CliPrefix.None);SubCommand>] Version
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] Compile
    | [<CliPrefix(CliPrefix.None);SubCommand>] Build

    | [<CliPrefix(CliPrefix.None);SubCommand>] Test
    | [<CliPrefix(CliPrefix.None);SubCommand>] Unit_Test
    | [<CliPrefix(CliPrefix.None);SubCommand>] Integrate

    | [<CliPrefix(CliPrefix.None);SubCommand>] Format of ParseResults<FormatArgs>
    | [<CliPrefix(CliPrefix.None);SubCommand>] Watch

    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] Lint of ParseResults<LintArgs>
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] PristineCheck
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] ValidateLicenses

    | [<CliPrefix(CliPrefix.None);SubCommand>] Publish
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] PublishBinaries
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] RunLocalContainer
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] PublishContainers
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] PublishZip

    | [<CliPrefix(CliPrefix.None);SubCommand>] Release
    
    | [<Inherit;AltCommandLine("-s")>] Single_Target
    | [<Inherit>] Token of string 
    | [<Inherit;AltCommandLine("-c")>] Skip_Dirty_Check
    | [<Inherit;EqualsAssignment>] Test_Suite of TestSuite
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            // commands
            | Clean -> "clean known output locations"
            | Version -> "print version information"
            | Build -> "Run build"

            | Unit_Test -> "alias to providing: test --test-suite=unit"
            | Integrate -> "alias to providing: test --test-suite=integration"
            | Test -> "runs a clean build and then runs all the tests unless --test-suite is provided"

            | Release -> "runs build, tests, and create and validates the packages shy of publishing them"
            | Publish -> "Publishes artifacts"
            | Format _ -> "runs dotnet format"

            | Watch -> "runs dotnet watch to continuous build code/templates and web assets on the fly"

            // steps
            | Lint _ -> "runs dotnet format --verify-no-changes"
            | PristineCheck
            | PublishBinaries
            | PublishContainers
            | RunLocalContainer
            | PublishZip
            | ValidateLicenses
            | Compile

            // flags
            | Single_Target -> "Runs the provided sub command without running their dependencies"
            | Token _ -> "Token to be used to authenticate with github"
            | Skip_Dirty_Check -> "Skip the clean checkout check that guards the release/publish targets"
            | Test_Suite _ -> "Specify the test suite to run, defaults to all"

    member this.StepName =
        match FSharpValue.GetUnionFields(this, typeof<Build>) with
        | case, _ -> case.Name.ToLowerInvariant()
        
    static member Targets =
        let cases = FSharpType.GetUnionCases(typeof<Build>)
        seq {
             for c in cases do
                 match c.GetFields().Length with
                 | 0 ->
                     match FSharpValue.MakeUnion(c, [| |]) with
                     | NonNull u -> u :?> Build
                     | _ -> failwithf $"%s{c.Name} can not be cast to Build enum"
                 | _ when c.Name = "Format" ->
                     // Format has sub-arguments, create empty instance
                     let emptyFormatArgs = ArgumentParser.Create<FormatArgs>().Parse([||])
                     Format emptyFormatArgs
                 | _ when c.Name = "Lint" ->
                     // Lint has sub-arguments, create empty instance
                     let emptyLintArgs = ArgumentParser.Create<LintArgs>().Parse([||])
                     Lint emptyLintArgs
                 | _ -> ()
        }
        
    static member Ignore (_: Build) _ = ()
        
    static member Step action (target: Build) parsed =
        Targets.Target(target.StepName, Action(fun _ -> action(parsed)))

    static member Cmd (dependsOn: Build list) (composedOf: Build list) action (target: Build) (parsed: ParseResults<Build>) =
        let singleTarget = parsed.TryGetResult Single_Target |> Option.isSome
        let dependsOn = if singleTarget then [] else dependsOn
            
        let steps = dependsOn @ composedOf |> List.map _.StepName
        Targets.Target(target.StepName, steps, Action(fun _ -> action parsed))
