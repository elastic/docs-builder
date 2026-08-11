// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``AuthoringTests``.``block elements``.``nested directive options``

open Xunit
open authoring

// DirectiveBlockParser.TryContinue stops an ancestor directive consuming an option line once
// it has opened a nested directive child. Without the guard the ancestor also takes every
// descendant's options, and the last one wins.
//
// No existing directive pair shares an option name, so the collision is latent for them:
// {tab-set} reads group while {tab-item} reads sync and selected, and {applies-switch} and
// {applies-item} split the same way. These tests pin that each option still reaches the block
// that declared it, which is what the guard must not break.

type ``tab set with its own group and per-item sync`` () =
    static let markdown = Setup.Markdown """
::::{tab-set}
:group: install-method

:::{tab-item} Local
:sync: local
local body
:::

:::{tab-item} Container
:sync: container
container body
:::
::::
"""

    // The group is declared on the tab-set before any child, so it still reaches the tab-set.
    [<Fact>]
    let ``the tab set keeps its own group`` () =
        markdown |> convertsToContainingRawHtml "data-sync-group=\"install-method\""

    // Each sync reaches the item that declared it, rather than all landing on the last one.
    [<Fact>]
    let ``the first item keeps its own sync`` () =
        markdown |> convertsToContainingRawHtml "data-sync-id=\"local\""

    [<Fact>]
    let ``the second item keeps its own sync`` () =
        markdown |> convertsToContainingRawHtml "data-sync-id=\"container\""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``applies switch with its own group and per-item sync`` () =
    static let markdown = Setup.Markdown """
::::{applies-switch}
:group: deployment

:::{applies-item} serverless: ga
:sync: serverless
serverless body
:::

:::{applies-item} stack: ga 9.0+
:sync: self-managed
self-managed body
:::
::::
"""

    [<Fact>]
    let ``the switch keeps its own group`` () =
        markdown |> convertsToContainingRawHtml "data-sync-group=\"deployment\""

    [<Fact>]
    let ``the first item keeps its own sync`` () =
        markdown |> convertsToContainingRawHtml "data-sync-id=\"serverless\""

    [<Fact>]
    let ``the second item keeps its own sync`` () =
        markdown |> convertsToContainingRawHtml "data-sync-id=\"self-managed\""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``stepper with per-step anchors`` () =
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
    let ``each step keeps its own anchor`` () =
        markdown |> convertsToContainingRawHtml "install-step"

    [<Fact>]
    let ``the second step keeps its own anchor`` () =
        markdown |> convertsToContainingRawHtml "configure-step"

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``dropdown wrapping an admonition with its own name`` () =
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
    let ``the dropdown keeps its own open state`` () =
        markdown |> convertsToContainingRawHtml "Outer summary"

    [<Fact>]
    let ``the nested admonition keeps its own name`` () =
        markdown |> convertsToContainingRawHtml "inner-note"

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors
