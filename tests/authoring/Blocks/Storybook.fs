// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``AuthoringTests``.``block elements``.``storybook elements``

open Xunit
open authoring

type ``storybook missing reference`` () =
    static let markdown = Setup.Markdown """
:::{storybook}
:::
"""

    [<Fact>]
    let ``has error`` () = markdown |> hasError "requires :id: or :project:"

type ``storybook missing registry`` () =
    static let markdown = Setup.Markdown """
:::{storybook}
:id: kibana:shared_ux:components-button--regular
:::
"""

    [<Fact>]
    let ``has error`` () = markdown |> hasError "requires docset.yml storybook.registry"
