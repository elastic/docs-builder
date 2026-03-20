// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``AuthoringTests``.``block elements``.``storybook elements``

open Xunit
open authoring

type ``storybook with root relative url`` () =
    static let markdown = Setup.Markdown """
:::{storybook}
:root: /storybook/my-lib
:id: components-button--primary
:height: 300
:title: Button / Primary story
:::
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<div class="storybook-embed">
	<iframe src="/storybook/my-lib/iframe.html?id=components-button--primary&amp;viewMode=story" title="Button / Primary story" style="width:100%;height:300px;border:none;" loading="lazy"></iframe>
</div>
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

    [<Fact>]
    let ``renders llm markdown`` () =
        markdown |> convertsToNewLLM """
<storybook src="https://www.elastic.co/storybook/my-lib/iframe.html?id=components-button--primary&viewMode=story" height="300" title="Button / Primary story">
</storybook>
"""

type ``storybook with body content`` () =
    static let markdown = Setup.Markdown """
:::{storybook}
:root: /storybook/my-lib
:id: components-button--primary
Supporting details for this story.
:::
"""

    [<Fact>]
    let ``renders body below iframe`` () =
        markdown |> convertsToHtml """
<div class="storybook-embed">
	<iframe src="/storybook/my-lib/iframe.html?id=components-button--primary&amp;viewMode=story" title="Storybook story" style="width:100%;height:400px;border:none;" loading="lazy"></iframe>
	<div class="storybook-embed-body">
		<p>Supporting details for this story.</p>
	</div>
</div>
"""

type ``storybook with docset root`` () =
    static let markdown =
        (Setup.GenerateWithOptions
            { SetupOptions.Empty with
                DocsetExtraYaml = Some """
storybook:
  root: /storybook/my-lib
""" }
            [ TestFile.Index """
# Test Document
:::{storybook}
:id: components-button--primary
:::
"""
            ])

    [<Fact>]
    let ``uses docset root in html`` () =
        markdown |> convertsToHtml """
<div class="storybook-embed">
	<iframe src="/storybook/my-lib/iframe.html?id=components-button--primary&amp;viewMode=story" title="Storybook story" style="width:100%;height:400px;border:none;" loading="lazy"></iframe>
</div>
"""

type ``storybook with docset server root`` () =
    static let markdown =
        (Setup.GenerateWithOptions
            { SetupOptions.Empty with
                DocsetExtraYaml = Some """
storybook:
  root: /storybook/my-lib
  server_root: http://localhost:6006
""" }
            [ TestFile.Index """
# Test Document
:::{storybook}
:id: components-button--primary
:::
"""
            ])

    [<Fact>]
    let ``uses docset server root in llm`` () =
        markdown |> convertsToNewLLM """
<storybook src="http://localhost:6006/storybook/my-lib/iframe.html?id=components-button--primary&viewMode=story" height="400" title="Storybook story">
</storybook>
"""

type ``storybook with literal root`` () =
    static let markdown = Setup.Markdown """
:::{storybook}
:root: /
:id: components-button--primary
:::
"""

    [<Fact>]
    let ``uses server root without double slash in html`` () =
        markdown |> convertsToHtml """
<div class="storybook-embed">
	<iframe src="/iframe.html?id=components-button--primary&amp;viewMode=story" title="Storybook story" style="width:100%;height:400px;border:none;" loading="lazy"></iframe>
</div>
"""

type ``storybook invalid height`` () =
    static let markdown = Setup.Markdown """
:::{storybook}
:root: /storybook/my-lib
:id: components-button--primary
:height: tall
:::
"""

    [<Fact>]
    let ``has warning`` () = markdown |> hasWarning ":height: must be a positive integer"

type ``storybook missing root`` () =
    static let markdown = Setup.Markdown """
:::{storybook}
:::
"""

    [<Fact>]
    let ``has error`` () = markdown |> hasError "requires a :root: property or docset.yml storybook.root"

type ``storybook external url`` () =
    static let markdown = Setup.Markdown """
:::{storybook}
:root: https://evil.example/storybook/my-lib
:id: components-button--primary
:::
"""

    [<Fact>]
    let ``has error`` () = markdown |> hasError "storybook.allowed_roots"
