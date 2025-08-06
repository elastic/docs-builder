// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System
open System.Collections.Generic
open System.Collections.Frozen
open System.IO.Abstractions.TestingHelpers
open Elastic.Documentation.Diagnostics
open Elastic.Documentation.Links
open Elastic.Markdown.Links.CrossLinks
open Elastic.Documentation
open Swensen.Unquote
open Elastic.Documentation.Configuration.Builder
open authoring

module CrossLinkResolverAssertions =

    let private parseRedirectsYaml (redirectsYamlContent: string) (collector: IDiagnosticsCollector) =
        let mockFileSystem = MockFileSystem()
        let fullYaml = sprintf "redirects:\n%s" (redirectsYamlContent.Replace("\r\n", "\n").Replace("\n", "\n  "))
        let mockRedirectsFilePath = "mock_redirects.yml"
        mockFileSystem.AddFile(mockRedirectsFilePath, MockFileData(fullYaml))
        let mockRedirectsFile = mockFileSystem.FileInfo.New(mockRedirectsFilePath)

        let docContext =
            { new IDocumentationSetContext with
                member _.Collector = collector
                member _.DocumentationSourceDirectory = mockFileSystem.DirectoryInfo.New("/docs")
                member _.Git = GitCheckoutInformation.Unavailable
                member _.ReadFileSystem = mockFileSystem
                member _.WriteFileSystem = mockFileSystem
                member _.ConfigurationPath = mockFileSystem.FileInfo.New("mock_docset.yml")
                member _.OutputDirectory = mockFileSystem.DirectoryInfo.New(".artifacts")
            }
        let redirectFileParser = RedirectFile(mockRedirectsFile, docContext)
        redirectFileParser.Redirects

    let private createFetchedCrossLinks (redirectsYamlSnippet: string) (linksData: IDictionary<string, LinkMetadata>) repoName =
        let collector = TestDiagnosticsCollector() :> IDiagnosticsCollector
        let redirectRules = parseRedirectsYaml redirectsYamlSnippet collector

        if collector.Errors > 0 then
            failwithf $"Failed to parse redirects YAML: %A{collector}"

        let repositoryLinks =
            RepositoryLinks(
              Origin = GitCheckoutInformation.Unavailable,
              UrlPathPrefix = null,
              Links = Dictionary(linksData),
              CrossLinks = Array.empty<string>,
              Redirects = redirectRules
            )

        let declaredRepos = HashSet<string>()
        declaredRepos.Add(repoName) |> ignore

        FetchedCrossLinks(
            DeclaredRepositories = declaredRepos,
            LinkReferences = FrozenDictionary.ToFrozenDictionary(dict [repoName, repositoryLinks]),
            FromConfiguration = true,
            LinkIndexEntries = FrozenDictionary<string, LinkRegistryEntry>.Empty
        )

    // language=yaml
    let private redirectsYaml = """
  # test scenario 1
  'testing/redirects/multi-topic-page-1-old.md':
    to: 'testing/redirects/multi-topic-page-1-new-anchorless.md'
    anchors: { "!": null }
    many:
      - to: 'testing/redirects/multi-topic-page-1-new-topic-a-subpage.md'
        anchors: {'topic-a-intro': null, 'topic-a-details': 'details-anchor'}
      - to: 'testing/redirects/multi-topic-page-1-new-topic-b-subpage.md'
        anchors: {'topic-b-main': 'main-anchor'}
      - to: 'testing/redirects/multi-topic-page-1-old.md'
        anchors: {'topic-c-main': 'topic-c-main'}
  # test scenario 2
  'testing/redirects/multi-topic-page-2-old.md':
    to: 'testing/redirects/multi-topic-page-2-old.md'
    anchors: {} # This means pass through any anchor for the default 'to'
    many:
      - to: 'testing/redirects/multi-topic-page-2-new-topic-a-subpage.md'
        anchors: {'topic-a-intro': 'introduction', 'topic-a-details': null}
      - to: 'testing/redirects/multi-topic-page-2-new-topic-b-subpage.md'
        anchors: {'topic-b-main': 'summary', 'topic-b-config': null}
"""

    let private mockLinksData =
        dict [
            ("testing/redirects/multi-topic-page-1-new-anchorless.md", LinkMetadata(Anchors = null, Hidden = false))
            ("testing/redirects/multi-topic-page-1-new-topic-a-subpage.md", LinkMetadata(Anchors = [| "details-anchor"; "introduction" |], Hidden = false))
            ("testing/redirects/multi-topic-page-1-new-topic-b-subpage.md", LinkMetadata(Anchors = [| "main-anchor" |], Hidden = false))
            ("testing/redirects/multi-topic-page-1-old.md", LinkMetadata(Anchors = [| "topic-c-main"; "unmatched-anchor" |], Hidden = false))
            ("testing/redirects/multi-topic-page-2-old.md", LinkMetadata(Anchors = [| "unmatched-anchor"; "topic-c-main"; "topic-a-intro"; "topic-a-details"; "topic-b-main"; "topic-b-config" |], Hidden = false))
            ("testing/redirects/multi-topic-page-2-new-topic-a-subpage.md", LinkMetadata(Anchors = [| "introduction"; "summary" |], Hidden = false))
            ("testing/redirects/multi-topic-page-2-new-topic-b-subpage.md", LinkMetadata(Anchors = [| "summary" |], Hidden = false))
        ] :> IDictionary<_,_>


    let private repoName = "docs-content"
    let private fetchedLinks = createFetchedCrossLinks redirectsYaml mockLinksData repoName
    let private uriResolver = IsolatedBuildEnvironmentUriResolver() :> IUriEnvironmentResolver
    let private baseExpectedUrl = $"https://docs-v3-preview.elastic.dev/elastic/{repoName}/tree/main"

    let resolvesTo (inputUrl: string) (expectedPathWithOptionalAnchor: string) =
        let mutable errors = List.empty
        let errorEmitter (msg: string) = errors <- msg :: errors
        let inputUri = Uri(inputUrl)
        let mutable resolvedUri : Uri = Unchecked.defaultof<Uri>

        let success = CrossLinkResolver.TryResolve(Action<_>(errorEmitter), fetchedLinks, uriResolver, inputUri, &resolvedUri)

        if not errors.IsEmpty then
            failwithf $"Resolution for '%s{inputUrl}' failed with errors: %A{errors}"

        test <@ success @>
        match box resolvedUri with
        | null -> failwithf $"Resolved URI was null for input '%s{inputUrl}' even though TryResolve returned true."
        | _ ->
            let expectedFullUrl = baseExpectedUrl + expectedPathWithOptionalAnchor
            test <@ resolvedUri.ToString() = expectedFullUrl @>
