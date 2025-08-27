// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System
open System.Collections.Generic
open System.Collections.Frozen
open System.Runtime.InteropServices
open System.Linq
open Elastic.Documentation.Configuration.Builder
open Elastic.Documentation.Links
open Elastic.Markdown.Links.CrossLinks

type TestCrossLinkResolver (config: ConfigurationFile) =

    let references = Dictionary<string, RepositoryLinks>()
    let declared = HashSet<string>()
    let uriResolver = IsolatedBuildEnvironmentUriResolver()
    let mutable crossLinks = FetchedCrossLinks.Empty
    
    do
        let redirects = RepositoryLinks.SerializeRedirects config.Redirects
        // language=json
        let json = $$"""{
  "origin": {
    "branch": "main",
    "remote": " https://github.com/elastic/docs-content",
    "ref": "76aac68d066e2af935c38bca8ce04d3ee67a8dd9"
  },
  "url_path_prefix": "/elastic/docs-content/tree/main",
  "cross_links": [],
  "redirects" : {{redirects}},
  "links": {
    "index.md": {},
    "get-started/index.md": {
      "anchors": [
        "elasticsearch-intro-elastic-stack",
        "elasticsearch-intro-use-cases"
      ]
    },
    "solutions/observability/apps/apm-server-binary.md": {
      "anchors": [ "apm-deb" ]
    },
    "testing/redirects/first-page.md": {
      "anchors": [ "current-anchor", "another-anchor" ]
    },
    "testing/redirects/second-page.md": {
      "anchors": [ "active-anchor", "zz" ]
    },
    "testing/redirects/third-page.md": { "anchors": [ "bb" ] },
    "testing/redirects/5th-page.md": { "anchors": [ "yy" ] }
  }
}
"""
        let reference = CrossLinkFetcher.Deserialize json
        references.Add("docs-content", reference)
        references.Add("kibana", reference)
        declared.Add("docs-content") |> ignore;
        declared.Add("kibana") |> ignore;

        let indexEntries =
            references.ToDictionary(_.Key, fun (e : KeyValuePair<string, RepositoryLinks>) -> LinkRegistryEntry(
                Repository = e.Key,
                Path = $"elastic/docs-builder-tests/{e.Key}/links.json",
                Branch = "main",
                ETag = Guid.NewGuid().ToString(),
                GitReference = Guid.NewGuid().ToString()
             ))

        let resolvedCrossLinks =
            FetchedCrossLinks(
                DeclaredRepositories=declared,
                LinkReferences=references.ToFrozenDictionary(),
                LinkIndexEntries=indexEntries.ToFrozenDictionary()
            )
        crossLinks <- resolvedCrossLinks

        

    member this.LinkReferences = references
    member this.DeclaredRepositories = declared
    
    interface ICrossLinkResolver with

        member this.UriResolver = uriResolver

        member this.TryResolve(errorEmitter, crossLinkUri, [<Out>]resolvedUri : byref<Uri|null>) =
            CrossLinkResolver.TryResolve(errorEmitter, crossLinks, uriResolver, crossLinkUri, &resolvedUri)


