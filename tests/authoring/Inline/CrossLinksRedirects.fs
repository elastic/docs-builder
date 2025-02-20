// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``inline elements``.``cross links redirects``

open Xunit
open authoring

let urlPrefix = "https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main"

type ``link to redirected page`` () =

    static let markdown = Setup.Markdown """
[Was first is now second](docs-content://testing/redirects/first-page-old.md)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml $"""
            <p><a href="{urlPrefix}/testing/redirects/second-page">
                Was first is now second
            </a></p>
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
        markdown |> convertsToHtml $"""
            <p><a href="{urlPrefix}/testing/redirects/second-page#active-anchor">
                Was first is now second
            </a></p>
        """

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``has no warning`` () = markdown |> hasNoWarnings

/// Goal: A writer moves a file to a new location.
/// Required functionality: A 1:1 redirect from the old file location to the new file location.
/// All anchors in the old file are mapped to identical anchors in the new file.
type ``Scenario 1: Moving a file`` () =
    static let markdown = Setup.Markdown """
[Scenario 1](docs-content://testing/redirects/first-page-old.md#old-anchor)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml $"""
            <p><a href="{urlPrefix}/testing/redirects/second-page#active-anchor">
                Scenario 1</a></p>
        """

type ``Scenario 1 B: Moving a file`` () =
    static let markdown = Setup.Markdown """
[Scenario 1](docs-content://testing/redirects/4th-page.md#yy)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml $"""
            <p><a href="{urlPrefix}/testing/redirects/5th-page#yy">
                Scenario 1</a></p>
        """


/// Goal: A writer breaks up an existing page into multiple pages.
/// Required functionality: A 1:many redirect, where the original file maps to multiple new pages.
/// In this case, the anchors in the old file may end up in multiple new files.
/// The ability to specify anchor-by-anchor redirects is required.
type ``Scenario 2: Splitting a page into multiple smaller pages`` () =
    static let markdown = Setup.Markdown """
[Scenario 2](docs-content://testing/redirects/second-page-old.md#aa)
[Scenario 2](docs-content://testing/redirects/second-page-old.md#bb)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml $"""
            <p><a href="{urlPrefix}/testing/redirects/second-page#zz">
                Scenario 2</a><br/>
            <a href="{urlPrefix}/testing/redirects/third-page#yy">
                Scenario 2</a></p>
        """


/// Goal: A writer removes a section of a page that was previously linked to via a cross-repo anchor link.
/// Required functionality: A 1:null mapping for anchors. Any inbound links with that anchor should update
/// to point to the base page instead.
type ``Scenario 3: Deleting a section on a page (removing anchors)`` () =
    static let markdown = Setup.Markdown """
[Scenario 3](docs-content://testing/redirects/third-page.md#removed-anchor)
"""
    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml $"""
            <p><a href="{urlPrefix}/testing/redirects/third-page">
                Scenario 3</a></p>
        """

/// Goal: A writer removes a section of a page that was previously linked to via a cross-repo anchor link.
/// Required functionality: A 1:null mapping for anchors. Any inbound links with that anchor should update
/// to point to the base page instead.
type ``Scenario 3 B: Linking to a removed anchor on a redirected page`` () =
    static let markdown = Setup.Markdown """
[Scenario 3 B](docs-content://testing/redirects/second-page-old.md#removed-anchor)
"""
    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml $"""
            <p><a href="{urlPrefix}/testing/redirects/second-page">
                Scenario 3 B</a></p>
        """



/// Goal: A writer removes a page completely.
/// Required functionality: A catchall redirect that strips any anchor links to the old page and points
/// to a designated fallback page instead.
type ``Scenario 4: Deleting an entire page`` () =
    static let markdown = Setup.Markdown """
[Scenario 4](docs-content://testing/redirects/7th-page.md#yy)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml $"""
            <p><a href="{urlPrefix}/testing/redirects/5th-page">
                Scenario 4</a></p>
        """

type ``Scenario 4 B: Deleting an entire page (short syntax for no anchors)`` () =
    static let markdown = Setup.Markdown """
[Scenario 4](docs-content://testing/redirects/9th-page.md)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml $"""
            <p><a href="{urlPrefix}/testing/redirects/5th-page">
                Scenario 4</a></p>
        """

/// Goal: A writer removes a page completely.
/// Redirect to empty (index) because no alternative page is available
type ``Scenario 5: Deleting an entire page`` () =
    static let markdown = Setup.Markdown """
[Scenario 5](docs-content://testing/redirects/6th-page.md#yy)
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml $"""
            <p><a href="{urlPrefix}/">Scenario 5</a></p>
        """

