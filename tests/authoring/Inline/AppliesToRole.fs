// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``AuthoringTests``.``inline elements``.``applies_to role``

open Elastic.Documentation.AppliesTo
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
        <applies-to-popover badge-key="Stack" badge-lifecycle-text="Planned" lifecycle-class="preview" lifecycle-name="Preview" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}" show-popover="true" is-inline="true">
        </applies-to-popover>
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

type ``render 'Preview' for GA in future version`` () =
    static let markdown = Setup.Markdown """

This is an inline {applies_to}`stack: preview 8.0, ga 8.1` element.
"""

    [<Fact>]
    let ``parses to AppliesDirective`` () =
        let directives = markdown |> converts "index.md" |> parses<AppliesToRole>
        test <@ directives.Length = 1 @>
        directives |> appliesToDirective (ApplicableTo(
            Stack=AppliesCollection.op_Explicit "ga 8.1, preview 8.0"
        ))

    [<Fact>]
    let ``validate HTML: generates single combined badge`` () =
        markdown |> convertsToHtml """
<p>This is an inline
	<span class="applies applies-inline">
        <applies-to-popover badge-key="Stack" badge-version="8.0" lifecycle-class="preview" lifecycle-name="Preview" show-lifecycle-name="true" show-version="true" has-multiple-lifecycles="true" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Preview in 8.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in technical preview and is not ready for production usage. Technical preview features may change or be removed at any time. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}" show-popover="true" is-inline="true">
        </applies-to-popover>
	</span>
	element.</p>
"""
