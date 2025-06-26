// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``inline elements``.``applies_to role``

open Elastic.Markdown.Myst.FrontMatter
open Elastic.Markdown.Myst.Roles.AppliesTo
open Swensen.Unquote
open Xunit
open authoring
open authoring.MarkdownDocumentAssertions

type ``parses inline {applies_to} role`` () =
    static let markdown = Setup.Markdown """

This is an inline {applies_to}`stack: preview 9.1` element.
"""

    [<Fact>]
    let ``parses to AppliesDirective`` () =
        let directives = markdown |> converts "index.md" |> parses<AppliesToRole>
        test <@ directives.Length = 1 @>
        directives |> appliesToDirective (ApplicableTo(
            Stack=AppliesCollection.op_Explicit "preview 9.1.0"
        ))

    [<Fact>]
    let ``validate HTML: generates link and alt attr`` () =
        markdown |> convertsToHtml """
        <p>This is an inline
            <span class="applies-inline">
                <span class="applicable-info" data-tippy-content="We plan to add this functionality in a future update. Plans may change without notice.">
                    Planned
                    <span class="applicable-meta applicable-meta-technical-preview">
                    </span>
                </span>
            </span>
            element.
        </p>
        """


type ``parses nested ess moniker`` () =
    static let markdown = Setup.Markdown """

This is an inline {applies_to}`ess: preview 9.1` element.
"""

    [<Fact>]
    let ``parses to AppliesDirective`` () =
        let directives = markdown |> converts "index.md" |> parses<AppliesToRole>
        test <@ directives.Length = 1 @>
        directives |> appliesToDirective (ApplicableTo(
            Deployment=DeploymentApplicability(
                Ess=AppliesCollection.op_Explicit "preview 9.1.0"
            )
        ))

type ``parses {preview} shortcut`` () =
    static let markdown = Setup.Markdown """

This is an inline {preview}`9.1` element.
"""

    [<Fact>]
    let ``parses to AppliesDirective`` () =
        let directives = markdown |> converts "index.md" |> parses<AppliesToRole>
        test <@ directives.Length = 1 @>
        directives |> appliesToDirective (ApplicableTo(
            Product=AppliesCollection.op_Explicit "preview 9.1.0"
        ))
