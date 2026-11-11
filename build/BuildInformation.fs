// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module BuildInformation

open System
open System.IO
open System.Runtime.InteropServices
open System.Threading
open System.Xml.Linq
open System.Xml.XPath
open Proc.Fs

type Paths =
    static member Root =
        let mutable dir = DirectoryInfo(".")
        let mutable rooted = false
        while not(rooted) && dir.GetFiles("*.slnx").Length = 0 do
            match dir.Parent with
            | NonNull parent -> dir <- parent
            | _ -> rooted <- true
        Environment.CurrentDirectory <- dir.FullName
        dir
        
    static member RelativePathToRoot path = Path.GetRelativePath(Paths.Root.FullName, path) 
        
    static member ArtifactFolder = DirectoryInfo(Path.Combine(Paths.Root.FullName, ".artifacts"))
    static member ArtifactPath t = DirectoryInfo(Path.Combine(Paths.ArtifactFolder.FullName, t))
    
    static member private SrcFolder = DirectoryInfo(Path.Combine(Paths.Root.FullName, "src"))
    static member SrcPath (t: string list) = DirectoryInfo(Path.Combine([Paths.SrcFolder.FullName] @ t |> List.toArray))


type BuildConfiguration = 
    static member ValidateAssemblyName = false
    static member GenerateApiChanges = false
    
type BuildVersion =
    { /// The version without build metadata, such as 1.75.1-canary.0.19
      Semantic: string
      /// The version with the commit, such as 1.75.1-canary.0.19+0123abc
      Informational: string }

type Software =
    static member Organization = "elastic"
    static member Repository = "docs-builder"
    static member GithubMoniker = $"%s{Software.Organization}/%s{Software.Repository}"
    static member SignKey = "069ca2728db333c1"
    
    static let restore =
        Lazy<unit>((fun _ -> cmd "dotnet" [ "tool"; "restore" ] |> Cmd.run), LazyThreadSafetyMode.ExecutionAndPublication)

    static let versionInfo =
        Lazy<BuildVersion>(fun _ ->
            let sha = cmd "git" [ "rev-parse"; "HEAD" ] |> Cmd.text |> Cmd.get
            // MinVer logs on standard error, which Cmd.text leaves on the console.
            let semantic = cmd "dotnet" [ "minver"; "-p"; "canary.0"; "-m"; "0.1" ] |> Cmd.text |> Cmd.get
            { Semantic = semantic; Informational = $"%s{semantic}+%s{sha}" }
        , LazyThreadSafetyMode.ExecutionAndPublication)

    static member Version = restore.Value; versionInfo.Value

type OS =
    | OSX | Windows | Linux
with
    static member Current = 
        match 1 with
        | _ when RuntimeInformation.IsOSPlatform OSPlatform.OSX -> OSX
        | _ when RuntimeInformation.IsOSPlatform OSPlatform.Windows -> Windows
        | _ -> Linux

    static member Name =
        match OS.Current with
        | Linux -> "linux"
        | OSX -> "mac"
        | Windows -> "win"

    static member Arch =
        match RuntimeInformation.ProcessArchitecture with
        | Architecture.X86 -> "x86"
        | Architecture.X64 -> "x64"
        | Architecture.Arm -> "arm"
        | Architecture.Arm64 -> "arm64"
        | _ -> "unknown"



