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
	<span class="applies applies-inline">
		<span class="applicable-info" data-tippy-content="We plan to add this in a future Elastic&nbsp;Stack update. Plans may change without notice.

This functionality may be changed or removed in a future release. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features.">
			<span class="applicable-name">Stack</span>
			<span class="applicable-separator"></span>
			<span class="applicable-meta applicable-meta-preview">
				Planned
			</span>
		</span>
	</span>
	element.</p>
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


type ``parses applies to without version in table`` () =
    static let markdown = Setup.Markdown """
| col1 | col2                         |
|------|------------------------------|
| test | {applies_to}`ece: removed`   |
"""

    [<Fact>]
    let ``parses to AppliesDirective`` () =
        let directives = markdown |> converts "index.md" |> parses<AppliesToRole>
        test <@ directives.Length = 1 @>
        directives |>  appliesToDirective (ApplicableTo(
            Deployment=DeploymentApplicability(
                Ece=AppliesCollection.op_Explicit "removed"
            )
        ))

type ``parses applies to with text afterwards`` () =
    static let markdown = Setup.Markdown """
{applies_to}`ece: removed` hello world
"""

    [<Fact>]
    let ``parses to AppliesDirective`` () =
        let directives = markdown |> converts "index.md" |> parses<AppliesToRole>
        test <@ directives.Length = 1 @>
        directives |>  appliesToDirective (ApplicableTo(
            Deployment=DeploymentApplicability(
                Ece=AppliesCollection.op_Explicit "removed"
            )
        ))

type ``parses multiple applies_to in one line`` () =
    static let markdown = Setup.Markdown """
{applies_to}`ece: removed` {applies_to}`ece: removed`
"""

    [<Fact>]
    let ``parses to AppliesDirective`` () =
        let directives = markdown |> converts "index.md" |> parses<AppliesToRole>
        test <@ directives.Length = 2 @>
        directives |>  appliesToDirective (ApplicableTo(
            Deployment=DeploymentApplicability(
                Ece=AppliesCollection.op_Explicit "removed"
            )
        ))
