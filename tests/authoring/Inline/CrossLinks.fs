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

type ``error when using wrong scheme`` () =

    static let markdown = Setup.Markdown """
[APM Server binary](docs-x:/solutions/observability/apps/apm-server-binary.md)
"""

    [<Fact>]
    let ``error on bad scheme`` () =
        markdown
        |> hasError "'docs-x' is not declared as valid cross link repository in docset.yml under cross_links"

    [<Fact>]
    let ``has no warning`` () = markdown |> hasNoWarnings

type ``error when bad anchor is used`` () =

    static let markdown = Setup.Markdown """
[APM Server binary](docs-content:/solutions/observability/apps/apm-server-binary.md#apm-deb-x)
"""

    [<Fact>]
    let ``error when linking to unknown anchor`` () =
        markdown
        |> hasError "'solutions/observability/apps/apm-server-binary.md' has no anchor named: '#apm-deb-x"

    [<Fact>]
    let ``has no warning`` () = markdown |> hasNoWarnings

type ``link to valid anchor`` () =

    static let markdown = Setup.Markdown """
[APM Server binary](docs-content:/solutions/observability/apps/apm-server-binary.md#apm-deb)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
            <p><a
                href="https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/solutions/observability/apps/apm-server-binary#apm-deb">
                APM Server binary
                </a>
            </p>
        """

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``has no warning`` () = markdown |> hasNoWarnings

type ``link to repository that does not resolve yet`` () =

    static let markdown = Setup.Markdown """
[Elasticsearch Documentation](elasticsearch:/index.md)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
            <p><a
                href="https://docs-v3-preview.elastic.dev/elastic/elasticsearch/tree/main/">
                Elasticsearch Documentation
                </a>
            </p>
        """

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``has no warning`` () = markdown |> hasNoWarnings

type ``Using double forward slashes`` () =

    static let markdown = Setup.Markdown """
[APM Server binary](docs-content://solutions/observability/apps/apm-server-binary.md#apm-deb)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
            <p><a
                href="https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/solutions/observability/apps/apm-server-binary#apm-deb">
                APM Server binary
                </a>
            </p>
        """

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``has no warning`` () = markdown |> hasNoWarnings

type ``link to repository that does not resolve yet using double slashes`` () =

    static let markdown = Setup.Markdown """
[Elasticsearch Documentation](elasticsearch://index.md)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
            <p><a
                href="https://docs-v3-preview.elastic.dev/elastic/elasticsearch/tree/main/">
                Elasticsearch Documentation
                </a>
            </p>
        """

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``has no warning`` () = markdown |> hasNoWarnings

type ``link to redirected page`` () =

    static let markdown = Setup.Markdown """
[Was first is now second](docs-content://testing/redirects/first-page-old.md)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
            <p><a
                href="https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/testing/redirects/second-page">
                Was first is now second
                </a>
            </p>
        """

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``has no warning`` () = markdown |> hasNoWarnings

type ``link to redirected page with renamed anchor`` () =

    static let markdown = Setup.Markdown """
[Was first is now second](docs-content://testing/redirects/first-page-old.md#old-anchor)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
            <p><a
                href="https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/testing/redirects/second-page#active-anchor">
                Was first is now second
                </a>
            </p>
        """

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``has no warning`` () = markdown |> hasNoWarnings
