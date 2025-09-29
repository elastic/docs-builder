// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
module ``block elements``.``admonition elements``

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
			        <div class="admonition-title">
				        <span>Note</span>
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
	<div class="admonition-title">
		<span class="applies applies-admonition">
			<span class="applicable-info" data-tippy-content="">
				<span class="applicable-name">Stack</span>
				<span class="applicable-meta applicable-meta-ga">
				</span>
			</span>
		</span>
		<span class="admonition-title__separator"></span>
		<span>Note</span>
	</div>
	<div class="admonition-content">
		<p>This is a note with applies_to information.</p>
	</div>
</div>
<div class="admonition warning">
	<div class="admonition-title">
		<span class="applies applies-admonition">
			<span class="applicable-info" data-tippy-content="">
				<span class="applicable-name">Serverless</span>
				<span class="applicable-meta applicable-meta-ga">
				</span>
			</span>
		</span>
		<span class="admonition-title__separator"></span>
		<span>Warning</span>
	</div>
	<div class="admonition-content">
		<p>This is a warning with applies_to information.</p>
	</div>
</div>
<div class="admonition tip">
	<div class="admonition-title">
		<span class="applies applies-admonition">
			<span class="applicable-info" data-tippy-content="">
				<span class="applicable-name">Serverless Elasticsearch</span>
				<span class="applicable-separator"></span>
				<span class="applicable-meta applicable-meta-preview">
					<span class="applicable-lifecycle applicable-lifecycle-preview">Preview</span>
				</span>
			</span>
		</span>
		<span class="admonition-title__separator"></span>
		<span>Tip</span>
	</div>
	<div class="admonition-content">
		<p>This is a tip with applies_to information.</p>
	</div>
</div>
<div class="admonition important">
	<div class="admonition-title">
		<span>Important</span>
	</div>
	<div class="admonition-content">
		<p>This is an important notice with applies_to information.</p>
	</div>
</div>
<div class="admonition admonition plain">
	<div class="admonition-title">
		<span>Custom Admonition</span>
	</div>
	<div class="admonition-content">
		<p>This is a custom admonition with applies_to information.</p>
	</div>
</div>"""

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
	            <div class="admonition-title">
		            <span>Note</span>
	            </div>
	            <div class="admonition-content">
 		            <ul>
 			            <li>List Item 1
 				            <div class="admonition note">
 					            <div class="admonition-title">
 						            <span>Note</span>
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
                <div class="admonition-title">
 	                <span>Note</span>
                </div>
                <div class="admonition-content">
 	                <ul>
 		                <li>List Item 1
 			                <div class="admonition note">
 				                <div class="admonition-title">
 					                <span>Note</span>
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
 	            <div class="admonition-title">
 		            <span>Note</span>
 	            </div>
 	            <div class="admonition-content">
 		            <ul>
 			            <li>List Item 1
 				            <div class="admonition note">
 					            <div class="admonition-title">
 						            <span>Note</span>
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
