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
				        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="size-6">
					        <path stroke-linecap="round" stroke-linejoin="round" d="m11.25 11.25.041-.02a.75.75 0 0 1 1.063.852l-.708 2.836a.75.75 0 0 0 1.063.853l.041-.021M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9-3.75h.008v.008H12V8.25Z"></path>
				        </svg>
				        <span>Note</span>
			        </div>
			        <div class="admonition-content">
				        Hello, World!
			        </div>
		        </div>
	        </li>
        </ul>
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
	            <div class="admonition-title">
		            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="size-6">
			            <path stroke-linecap="round" stroke-linejoin="round" d="m11.25 11.25.041-.02a.75.75 0 0 1 1.063.852l-.708 2.836a.75.75 0 0 0 1.063.853l.041-.021M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9-3.75h.008v.008H12V8.25Z"></path>
		            </svg>
		            <span>Note</span>
	            </div>
	            <div class="admonition-content">
 		            <ul>
 			            <li>List Item 1
 				            <div class="admonition note">
 					            <div class="admonition-title">
 						            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="size-6">
 							            <path stroke-linecap="round" stroke-linejoin="round" d="m11.25 11.25.041-.02a.75.75 0 0 1 1.063.852l-.708 2.836a.75.75 0 0 0 1.063.853l.041-.021M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9-3.75h.008v.008H12V8.25Z"></path>
 						            </svg>
 						            <span>Note</span>
 					            </div>
 					            <div class="admonition-content">
 						            Hello, World!
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
 	                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="size-6">
 		                <path stroke-linecap="round" stroke-linejoin="round" d="m11.25 11.25.041-.02a.75.75 0 0 1 1.063.852l-.708 2.836a.75.75 0 0 0 1.063.853l.041-.021M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9-3.75h.008v.008H12V8.25Z"></path>
 	                </svg>
 	                <span>Note</span>
                </div>
                <div class="admonition-content">
 	                <ul>
 		                <li>List Item 1
 			                <div class="admonition note">
 				                <div class="admonition-title">
 					                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="size-6">
 						                <path stroke-linecap="round" stroke-linejoin="round" d="m11.25 11.25.041-.02a.75.75 0 0 1 1.063.852l-.708 2.836a.75.75 0 0 0 1.063.853l.041-.021M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9-3.75h.008v.008H12V8.25Z"></path>
 					                </svg>
 					                <span>Note</span>
 				                </div>
 				                <div class="admonition-content">
 					                Hello, World!
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
 		            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="size-6">
 			            <path stroke-linecap="round" stroke-linejoin="round" d="m11.25 11.25.041-.02a.75.75 0 0 1 1.063.852l-.708 2.836a.75.75 0 0 0 1.063.853l.041-.021M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9-3.75h.008v.008H12V8.25Z"></path>
 		            </svg>
 		            <span>Note</span>
 	            </div>
 	            <div class="admonition-content">
 		            <ul>
 			            <li>List Item 1
 				            <div class="admonition note">
 					            <div class="admonition-title">
 						            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="size-6">
 							            <path stroke-linecap="round" stroke-linejoin="round" d="m11.25 11.25.041-.02a.75.75 0 0 1 1.063.852l-.708 2.836a.75.75 0 0 0 1.063.853l.041-.021M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9-3.75h.008v.008H12V8.25Z"></path>
 						            </svg>
 						            <span>Note</span>
 					            </div>
 					            <div class="admonition-content">
 						            Hello, World!
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
