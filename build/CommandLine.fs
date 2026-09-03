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

type Build =
    | [<CliPrefix(CliPrefix.None);SubCommand>] Clean
    | [<CliPrefix(CliPrefix.None);SubCommand>] Version
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] Compile
    | [<CliPrefix(CliPrefix.None);SubCommand>] Build

    | [<CliPrefix(CliPrefix.None);SubCommand>] Test
    | [<CliPrefix(CliPrefix.None);SubCommand>] Unit_Test
    | [<CliPrefix(CliPrefix.None);SubCommand>] Integrate

    | [<CliPrefix(CliPrefix.None);SubCommand>] Format
    | [<CliPrefix(CliPrefix.None);SubCommand>] Watch
    | [<CliPrefix(CliPrefix.None);SubCommand>] Watch_All

    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] Lint
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] PristineCheck
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] ValidateLicenses

    | [<CliPrefix(CliPrefix.None);SubCommand>] Publish
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] PublishBinaries
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] RunLocalContainer
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] PublishContainers
    | [<CliPrefix(CliPrefix.None);Hidden;SubCommand>] PublishZip

    | [<CliPrefix(CliPrefix.None);SubCommand>] Air_Gapped_Build
    | [<CliPrefix(CliPrefix.None);SubCommand>] Air_Gapped_Run

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

            | Air_Gapped_Build -> "Clones, builds docs with html exporter, and packages into an air-gapped Docker container"
            | Air_Gapped_Run -> "Runs the air-gapped docs container on port 8080"

            | Release -> "runs build, tests, and create and validates the packages shy of publishing them"
            | Publish -> "Publishes artifacts"
            | Format -> "runs dotnet format"

            | Watch -> "runs dotnet watch to continuous build code/templates and web assets on the fly"
            | Watch_All -> "runs dotnet watch on the Aspire AppHost to continuously rebuild and serve the fully assembled site"

            // steps
            | Lint -> "runs dotnet curb check"
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
