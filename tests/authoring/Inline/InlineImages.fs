// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``inline elements``.``image``

open Xunit
open authoring

type ``static path to image`` () =
    static let markdown = Setup.Markdown """
![Elasticsearch](/_static/img/observability.png)
"""

    [<Fact>]
    let ``validate HTML: generates link and alt attr`` () =
        markdown |> convertsToHtml """
            <p><img src="/_static/img/observability.png" alt="Elasticsearch" /></p>
        """

type ``relative path to image`` () =
    static let markdown = Setup.Markdown """
![Elasticsearch](_static/img/observability.png)
"""

    [<Fact>]
    let ``validate HTML: preserves relative path`` () =
        markdown |> convertsToHtml """
            <p><img src="_static/img/observability.png" alt="Elasticsearch" /></p>
        """

type ``supplying a tittle`` () =
    static let markdown = Setup.Markdown """
![Elasticsearch](_static/img/observability.png "Hello world")
"""

    [<Fact>]
    let ``validate HTML: includes title`` () =
        markdown |> convertsToHtml """
            <p><img src="_static/img/observability.png" alt="Elasticsearch" title="Hello world" /></p>
        """

type ``supplying a tittle with width and height`` () =
    static let markdown = Setup.Markdown """
![o](obs.png "Title =250x400")
"""

    [<Fact>]
    let ``validate HTML: does not include width and height in title`` () =
        markdown |> convertsToHtml """
            <p><img src="obs.png" width="250px" height="400px" alt="o" title="Title"/></p>
        """

type ``supplying a tittle with width and height in percentage`` () =
    static let markdown = Setup.Markdown """
![o](obs.png "Title =50%x40%")
"""

    [<Fact>]
    let ``validate HTML: does not include width and height in title`` () =
        markdown |> convertsToHtml """
            <p><img src="obs.png" width="50%" height="40%" alt="o" title="Title"/></p>
        """
type ``supplying a tittle with width only`` () =
    static let markdown = Setup.Markdown """
![o](obs.png "Title =30%")
"""

    [<Fact>]
    let ``validate HTML: sets height to width if not supplied`` () =
        markdown |> convertsToHtml """
            <p><img src="obs.png" width="30%" height="30%" alt="o" title="Title"/></p>
        """
