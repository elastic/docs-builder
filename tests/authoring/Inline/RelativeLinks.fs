// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``inline elements``.``relative links``

open Xunit
open authoring

type ``two pages with anchors end up in artifact`` () =

    static let generator = Setup.Generate [
        Index """
# A Document that lives at the root

*Welcome* to this documentation

## This anchor is autogenerated

### Several pages can be created [#and-anchored]

Through various means $$$including-this-inline-syntax$$$
"""
        Markdown "deeply/nested/file.md" """
# file.md

[link to root](../../index.md#and-anchored)

[link to parent](../parent.md)

[link to parent](../parent.md#some-header)

[link to sibling](./file2.md)
        """
        Markdown "deeply/nested/file2.md" """
# file2.md
"""
        Markdown "deeply/parent.md" """
# parent.md

## some header
[link to root](../index.md)
        """
    ]

    [<Fact>]
    let ``validate index.md HTML`` () =
        generator |> converts "deeply/nested/file.md" |> toHtml """
             <p><a href="/#and-anchored">link to root</a></p>
             <p><a href="/deeply/parent">link to parent</a></p>
             <p><a href="/deeply/parent#some-header">link to parent</a></p>
             <p><a href="/deeply/nested/file2">link to sibling</a></p>
         """

    [<Fact>]
    let ``has no errors`` () = generator |> hasNoErrors


