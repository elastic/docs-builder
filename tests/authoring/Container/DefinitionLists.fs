// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``container elements``.``vertical definition lists``

open Xunit
open authoring

type ``simple multiline definition with markup`` () =

    static let markdown = Setup.Markdown """
This is my `definition`
:   And this is the definition **body**
    Which may contain multiple lines
"""

    [<Fact>]
    let ``validate HTML`` () =
        markdown |> convertsToHtml """
             <dl>
                <dt>This is my <code>definition</code> </dt>
                <dd>
                    <p> And this is the definition <strong>body</strong> <br>
                        Which may contain multiple lines</p>
                </dd>
             </dl>
            """
    [<Fact>]
    let ``has no errors`` () = markdown |> hasNoErrors

type ``with embedded directives`` () =

    static let markdown = Setup.Markdown """
This is my `definition`
:   And this is the definition **body**
    Which may contain multiple lines
    :::{note}
    My note
    :::
"""

    [<Fact>]
    let ``validate HTML 2`` () =
        markdown |> convertsToHtml """
             <dl>
                <dt>This is my <code>definition</code> </dt>
                <dd>
                    <p> And this is the definition <strong>body</strong> <br>
                        Which may contain multiple lines</p>
                        <div class="admonition note">
 			                <p class="admonition-title">Note</p>
                        <p>My note</p>
                    </div>
                </dd>
             </dl>
            """
    [<Fact>]
    let ``has no errors 2`` () = markdown |> hasNoErrors
