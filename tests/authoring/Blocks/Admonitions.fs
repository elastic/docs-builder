// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``AuthoringTests``.``block elements``.``admonition elements``

open Xunit
open authoring

type ``admonition in list`` () =
    static let markdown = Setup.Markdown """
- List Item 1
  :::::{note}
  Hello, World!
  :::::
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
        <ul>
	        <li>List Item 1
		        <div class="admonition note">
			        <div class="admonition-header">
				        <span class="admonition-title">Note</span>
			        </div>
			        <div class="admonition-content">
				        <p>Hello, World!</p>
			        </div>
		        </div>
	        </li>
        </ul>
        """
    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``admonition with applies_to`` () =
    static let markdown = Setup.Markdown """
:::{note}
:applies_to: stack: ga
This is a note with applies_to information.
:::
:::{warning}
:applies_to: serverless: ga
This is a warning with applies_to information.
:::
:::{tip}
:applies_to: elasticsearch: preview
This is a tip with applies_to information.
:::
:::{important}
:applies_to: stack: ga, serverless: ga
This is an important notice with applies_to information.
:::
:::{admonition} Custom Admonition
:applies_to: stack: ga, serverless: ga, elasticsearch: preview
This is a custom admonition with applies_to information.
:::
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<div class="admonition note">
	<div class="admonition-header">
		<span class="admonition-title">Note</span>
		<span class="applies applies-admonition">
<applies-to-popover badge-key="Stack" badge-version="8.0+" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available in 8.0\u002B&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}" show-popover="true" is-inline="true"></applies-to-popover>
		</span>
		<span class="admonition-title__separator"></span>
	</div>
	<div class="admonition-content">
		<p>This is a note with applies_to information.</p>
	</div>
</div>
<div class="admonition warning">
	<div class="admonition-header">
		<span class="admonition-title">Warning</span>
		<span class="applies applies-admonition">
			<applies-to-popover badge-key="Serverless" badge-version="8.0+" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available in 8.0\u002B&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Serverless interfaces and procedures might differ from classic Elastic Stack deployments.&quot;,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="true"></applies-to-popover>
		</span>
		<span class="admonition-title__separator"></span>
	</div>
	<div class="admonition-content">
		<p>This is a warning with applies_to information.</p>
	</div>
</div>
<div class="admonition tip">
	<div class="admonition-header">
		<span class="admonition-title">Tip</span>
		<span class="applies applies-admonition">
			<applies-to-popover badge-key="Serverless Elasticsearch" badge-version="8.0+" lifecycle-class="preview" lifecycle-name="Preview" show-lifecycle-name="true" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Preview in 8.0\u002B&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in technical preview and is not ready for production usage. Technical preview features may change or be removed at any time. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="true"></applies-to-popover>
		</span>
		<span class="admonition-title__separator"></span>
	</div>
	<div class="admonition-content">
		<p>This is a tip with applies_to information.</p>
	</div>
</div>
<div class="admonition important">
	<div class="admonition-header">
		<span class="admonition-title">Important</span>
	</div>
	<div class="admonition-content">
		<p>This is an important notice with applies_to information.</p>
	</div>
</div>
<div class="admonition admonition plain">
	<div class="admonition-header">
		<span class="admonition-title">Custom Admonition</span>
	</div>
	<div class="admonition-content">
		<p>This is a custom admonition with applies_to information.</p>
	</div>
</div>
"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``nested admonition in list`` () =
    static let markdown = Setup.Markdown """
:::{note}

- List Item 1
  :::::{note}
  Hello, World!
  :::::

## What

:::
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<div class="admonition note">
	<div class="admonition-header">
		<span class="admonition-title">Note</span>
	</div>
	<div class="admonition-content">
		<ul>
			<li>List Item 1
				<div class="admonition note">
					<div class="admonition-header">
						<span class="admonition-title">Note</span>
					</div>
					<div class="admonition-content">
						<p>Hello, World!</p>
					</div>
				</div>
			</li>
		</ul>
	</div>
</div>
<div class="heading-wrapper" id="what">
	<h2>
		<a class="headerlink" href="#what">What</a>
	</h2>
</div>"""

    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors


type ``nested admonition in list 2`` () =
    static let markdown = Setup.Markdown """
# heading

:::{note}

- List Item 1
  :::{note}
  Hello, World!
  :::

## What

:::
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<div class="heading-wrapper" id="heading">
	<h1>
		<a class="headerlink" href="#heading">heading</a>
	</h1>
</div>
<div class="admonition note">
	<div class="admonition-header">
		<span class="admonition-title">Note</span>
	</div>
	<div class="admonition-content">
		<ul>
			<li>List Item 1
				<div class="admonition note">
					<div class="admonition-header">
						<span class="admonition-title">Note</span>
					</div>
					<div class="admonition-content">
						<p>Hello, World!</p>
					</div>
				</div>
			</li>
		</ul>
	</div>
</div>
<div class="heading-wrapper" id="what">
	<h2>
		<a class="headerlink" href="#what">What</a>
	</h2>
</div>
"""
    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``nested admonition in list 3`` () =
    static let markdown = Setup.Markdown """
# heading

:::::{note}

- List Item 1
  ::::{note}
  Hello, World!
  ::::

## What

:::::
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
<div class="heading-wrapper" id="heading">
	<h1>
		<a class="headerlink" href="#heading">heading</a>
	</h1>
</div>
<div class="admonition note">
	<div class="admonition-header">
		<span class="admonition-title">Note</span>
	</div>
	<div class="admonition-content">
		<ul>
			<li>List Item 1
				<div class="admonition note">
					<div class="admonition-header">
						<span class="admonition-title">Note</span>
					</div>
					<div class="admonition-content">
						<p>Hello, World!</p>
					</div>
				</div>
			</li>
		</ul>
		<div class="heading-wrapper" id="what">
			<h2>
				<a class="headerlink" href="#what">What</a>
			</h2>
		</div>
	</div>
</div>
"""
    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors
