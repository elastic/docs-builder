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
