// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``block elements``.``image blocks``

open Xunit
open authoring

type ``static path to image`` () =
    static let markdown = Setup.Markdown """
:::{image} img/observability.png
:alt: Elasticsearch
:width: 250px
:screenshot:
:::
"""

    [<Fact>]
    let ``validate src is anchored`` () =
        markdown |> convertsToContainingHtml """
            <img loading="lazy" alt="Elasticsearch" src="/img/observability.png" style="width: 250px;" class="screenshot">
       """

type ``supports --url-path-prefix`` () =
    static let docs = Setup.GenerateWithOptions { UrlPathPrefix = Some "/docs" } [
        Static "img/observability.png"

        Index """# Testing nested inline anchors
:::{image} img/observability.png
:alt: Elasticsearch
:width: 250px
:screenshot:
:::
"""

        Markdown "folder/relative.md" """
:::{image} ../img/observability.png
:alt: Elasticsearch
:width: 250px
:screenshot:
:::
        """
    ]

    [<Fact>]
    let ``validate image src contains prefix`` () =
        docs |> convertsToContainingHtml """
            <img loading="lazy" alt="Elasticsearch" src="/docs/img/observability.png" style="width: 250px;" class="screenshot">
       """

    [<Fact>]
    let ``validate image src contains prefix when referenced relatively`` () =
        docs |> converts "folder/relative.md" |> containsHtml """
            <img loading="lazy" alt="Elasticsearch" src="/docs/img/observability.png" style="width: 250px;" class="screenshot">
       """

    [<Fact>]
    let ``has no errors`` () = docs |> hasNoErrors

type ``image ref out of scope`` () =
    static let docs = Setup.GenerateWithOptions { UrlPathPrefix = Some "/docs" } [
        Static "img/observability.png"

        Index """# Testing nested inline anchors
:::{image} ../img/observability.png
:alt: Elasticsearch
:width: 250px
:screenshot:
:::
"""
    ]

    [<Fact>]
    let ``validate image src contains prefix and is anchored to documentation scope root`` () =
        docs |> convertsToContainingHtml """
            <img loading="lazy" alt="Elasticsearch" src="/docs/img/observability.png" style="width: 250px;" class="screenshot">
       """

    [<Fact>]
    let ``emits an error image reference is outside of documentation scope`` () =
        docs |> hasError "./img/observability.png` does not exist. resolved to"
