// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``AuthoringTests``.``block elements``.``nested directive options``

open Xunit
open authoring

// DirectiveBlockParser.TryContinue stops an ancestor directive from consuming an option line
// once that ancestor has opened a nested directive child. Without the guard the ancestor
// swallows every descendant's options and the last one wins, corrupting its own.

type ``tab set wrapping tab items`` () =
    static let markdown = Setup.Markdown """
::::{tab-set}
:::{tab-item} First
first body
:::
:::{tab-item} Second
second body
:::
::::
"""

    [<Fact>]
    let ``keeps each tab title`` () =
        markdown |> convertsToContainingHtml """First"""

    [<Fact>]
    let ``keeps the second tab title`` () =
        markdown |> convertsToContainingHtml """Second"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``stepper wrapping steps with anchors`` () =
    static let markdown = Setup.Markdown """
::::{stepper}
:::{step} Install
:anchor: install-step
Install the thing.
:::
:::{step} Configure
:anchor: configure-step
Configure the thing.
:::
::::
"""

    [<Fact>]
    let ``keeps the first step anchor`` () =
        markdown |> convertsToContainingHtml """install-step"""

    [<Fact>]
    let ``keeps the second step anchor`` () =
        markdown |> convertsToContainingHtml """configure-step"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``admonition nested in a dropdown`` () =
    static let markdown = Setup.Markdown """
::::{dropdown} Outer summary
:open:
:::{note}
:name: inner-note
Inner content.
:::
::::
"""

    [<Fact>]
    let ``keeps the outer summary`` () =
        markdown |> convertsToContainingHtml """Outer summary"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors
