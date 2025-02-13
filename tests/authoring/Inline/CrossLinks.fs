// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``inline elements``.``cross links``

open Xunit
open authoring

type ``cross-link makes it into html`` () =

    static let markdown = Setup.Markdown """
[APM Server binary](docs-content:/solutions/observability/apps/apm-server-binary.md)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
            <p><a
                href="https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/solutions/observability/apps/apm-server-binary">
                APM Server binary
                </a>
            </p>
        """
        
    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``has no warning`` () = markdown |> hasNoWarnings
