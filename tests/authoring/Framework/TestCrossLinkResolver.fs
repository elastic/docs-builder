// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace authoring

open System
open System.Collections.Generic
open System.Runtime.InteropServices
open System.Threading.Tasks
open Elastic.Markdown.CrossLinks
open Elastic.Markdown.IO.State

type TestCrossLinkResolver () =

    let references = Dictionary<string, LinkReference>()
    member this.LinkReferences = references

    interface ICrossLinkResolver with
        member this.FetchLinks() =
            // language=json
            let json = """{
  "origin": {
    "branch": "main",
    "remote": " https://github.com/elastic/docs-conten",
    "ref": "76aac68d066e2af935c38bca8ce04d3ee67a8dd9"
  },
  "url_path_prefix": "/elastic/docs-content/tree/main",
  "cross_links": [],
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
    }
  }
}
"""
            let reference = CrossLinkResolver.Deserialize json
            this.LinkReferences.Add("docs-content", reference)
            this.LinkReferences.Add("kibana", reference)
            Task.CompletedTask

        member this.TryResolve(errorEmitter, crossLinkUri, [<Out>]resolvedUri : byref<Uri|null>) =
            CrossLinkResolver.TryResolve(errorEmitter, this.LinkReferences, crossLinkUri, &resolvedUri);


