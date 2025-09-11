// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``product availability``.``applicable to component``

open Elastic.Documentation.AppliesTo
open Elastic.Markdown.Myst.Directives.AppliesTo
open authoring
open authoring.MarkdownDocumentAssertions
open Swensen.Unquote
open Xunit

// Test Stack applicability scenarios
type ``stack applicability tests`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: ga 9.0.0
```
"""

    [<Fact>]
    let ``parses to AppliesDirective`` () =
        let directives = markdown |> converts "index.md" |> parses<AppliesToDirective>
        test <@ directives.Length = 1 @>
        directives |> appliesToDirective (ApplicableTo(
            Stack=AppliesCollection.op_Explicit "ga 9.0.0"
        ))

    [<Fact>]
    let ``renders GA with version`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available on Elastic&nbsp;Stack version 9.0.0 and later unless otherwise specified.

If this functionality is unavailable or behaves differently when deployed on ECH, ECE, ECK, or a self-managed installation, it will be indicated on the page.">
		<span class="applicable-name">Stack</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-ga">
			<span class="applicable-version applicable-version-ga">9.0.0</span>
		</span>
	</span>
</div>
"""

type ``stack preview future version`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: preview 9.1.0
```
"""

    [<Fact>]
    let ``renders preview future version as planned`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="We plan to add this functionality in a future Elastic&nbsp;Stack update. Subject to change.

This functionality may be changed or removed in a future release. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features.">
		<span class="applicable-name">Stack</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-preview">
			Planned
		</span>
	</span>
</div>
"""

type ``stack beta current version`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: beta 8.8.0
```
"""

    [<Fact>]
    let ``renders beta current version`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available in beta on Elastic&nbsp;Stack version 8.8.0 and later unless otherwise specified.

Beta features are subject to change. The design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features.">
		<span class="applicable-name">Stack</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-beta">
			<span class="applicable-lifecycle applicable-lifecycle-beta">Beta</span>
			<span class="applicable-version applicable-version-beta">8.8.0</span>
		</span>
	</span>
</div>
"""

type ``stack deprecated`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: deprecated 8.7.0
```
"""

    [<Fact>]
    let ``renders deprecated`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Deprecated on Elastic&nbsp;Stack version 8.7.0 and later unless otherwise specified.">
		<span class="applicable-name">Stack</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-deprecated">
			<span class="applicable-lifecycle applicable-lifecycle-deprecated">Deprecated</span>
			<span class="applicable-version applicable-version-deprecated">8.7.0</span>
		</span>
	</span>
</div>
"""

type ``stack removed`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: removed 8.6.0
```
"""

    [<Fact>]
    let ``renders removed`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Removed on Elastic&nbsp;Stack version 8.6.0 and later unless otherwise specified.">
		<span class="applicable-name">Stack</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-removed">
			<span class="applicable-lifecycle applicable-lifecycle-removed">Removed</span>
			<span class="applicable-version applicable-version-removed">8.6.0</span>
		</span>
	</span>
</div>
"""

type ``stack all versions`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: ga
```
"""

    [<Fact>]
    let ``renders all versions`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available on Elastic&nbsp;Stack unless otherwise specified.

If this functionality is unavailable or behaves differently when deployed on ECH, ECE, ECK, or a self-managed installation, it will be indicated on the page.">
		<span class="applicable-name">Stack</span>
	</span>
</div>
"""

// Test Serverless applicability scenarios
type ``serverless all projects`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
serverless: ga 9.0.0
```
"""

    [<Fact>]
    let ``renders serverless all projects`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available on Elastic&nbsp;Cloud&nbsp;Serverless version 9.0.0 and later unless otherwise specified.">
		<span class="applicable-name">Serverless</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-ga">
			<span class="applicable-version applicable-version-ga">9.0.0</span>
		</span>
	</span>
</div>
"""

type ``serverless individual projects`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
serverless:
  elasticsearch: ga 9.0.0
  observability: beta 9.1.0
  security: preview 9.2.0
```
"""

    [<Fact>]
    let ``renders serverless individual projects`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available on Serverless&nbsp;Elasticsearch projects version 9.0.0 and later unless otherwise specified.">
		<span class="applicable-name">Serverless Elasticsearch</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-ga">
			<span class="applicable-version applicable-version-ga">9.0.0</span>
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="Available in beta on Serverless&nbsp;Observability projects version 9.1.0 and later unless otherwise specified.

Beta features are subject to change. The design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features.">
		<span class="applicable-name">Serverless Observability</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-beta">
			<span class="applicable-lifecycle applicable-lifecycle-beta">Beta</span>
			<span class="applicable-version applicable-version-beta">9.1.0</span>
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="We plan to add this functionality in a future Serverless&nbsp;Security projects update. Subject to change.

This functionality may be changed or removed in a future release. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features.">
		<span class="applicable-name">Serverless Security</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-preview">
			Planned
		</span>
	</span>
</div>
"""

// Test Deployment applicability scenarios
type ``deployment ece`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
deployment:
  ece: ga 9.0.0
```
"""

    [<Fact>]
    let ``renders ECE deployment`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available on Elastic&nbsp;Cloud&nbsp;Enterprise version 9.0.0 and later unless otherwise specified.">
		<span class="applicable-name">ECE</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-ga">
			<span class="applicable-version applicable-version-ga">9.0.0</span>
		</span>
	</span>
</div>
"""

type ``deployment eck`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
deployment:
  eck: beta 9.0.0
```
"""

    [<Fact>]
    let ``renders ECK deployment`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available in beta on Elastic&nbsp;Cloud&nbsp;on&nbsp;Kubernetes version 9.0.0 and later unless otherwise specified.

Beta features are subject to change. The design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features.">
		<span class="applicable-name">ECK</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-beta">
			<span class="applicable-lifecycle applicable-lifecycle-beta">Beta</span>
			<span class="applicable-version applicable-version-beta">9.0.0</span>
		</span>
	</span>
</div>
"""

type ``deployment ess`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
deployment:
  ess: preview 9.0.0
```
"""

    [<Fact>]
    let ``renders ECH deployment`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="We plan to add this functionality in a future Elastic&nbsp;Cloud&nbsp;Hosted update. Subject to change.

This functionality may be changed or removed in a future release. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features.">
		<span class="applicable-name">ECH</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-preview">
			Planned
		</span>
	</span>
</div>
"""

type ``deployment self managed`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
deployment:
  self: ga 9.0.0
```
"""

    [<Fact>]
    let ``renders self-managed deployment`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available on Self-managed Elastic&nbsp;deployments version 9.0.0 and later unless otherwise specified.">
		<span class="applicable-name">Self-Managed</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-ga">
			<span class="applicable-version applicable-version-ga">9.0.0</span>
		</span>
	</span>
</div>
"""

// Test Product applicability scenarios
type ``apm agents`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
apm_agent_dotnet: ga 9.0.0
apm_agent_java: beta 9.1.0
apm_agent_python: preview 9.2.0
```
"""

    [<Fact>]
    let ``renders APM agents`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available on Application&nbsp;Performance&nbsp;Monitoring Agent for .NET version 9.0.0 and later unless otherwise specified.">
		<span class="applicable-name">APM Agent .NET</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-ga">
			<span class="applicable-version applicable-version-ga">9.0.0</span>
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="Available in beta on Application&nbsp;Performance&nbsp;Monitoring Agent for Java version 9.1.0 and later unless otherwise specified.

Beta features are subject to change. The design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features.">
		<span class="applicable-name">APM Agent Java</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-beta">
			<span class="applicable-lifecycle applicable-lifecycle-beta">Beta</span>
			<span class="applicable-version applicable-version-beta">9.1.0</span>
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="We plan to add this functionality in a future Application&nbsp;Performance&nbsp;Monitoring Agent for Python update. Subject to change.

This functionality may be changed or removed in a future release. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features.">
		<span class="applicable-name">APM Agent Python</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-preview">
			Planned
		</span>
	</span>
</div>
"""

type ``edot agents`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
edot_dotnet: ga 9.0.0
edot_java: beta 9.1.0
edot_python: preview 9.2.0
```
"""

    [<Fact>]
    let ``renders EDOT agents`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available on Elastic&nbsp;Distribution of OpenTelemetry&nbsp;.NET version 9.0.0 and later unless otherwise specified.">
		<span class="applicable-name">EDOT .NET</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-ga">
			<span class="applicable-version applicable-version-ga">9.0.0</span>
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="Available in beta on Elastic&nbsp;Distribution of OpenTelemetry&nbsp;Java version 9.1.0 and later unless otherwise specified.

Beta features are subject to change. The design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features.">
		<span class="applicable-name">EDOT Java</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-beta">
			<span class="applicable-lifecycle applicable-lifecycle-beta">Beta</span>
			<span class="applicable-version applicable-version-beta">9.1.0</span>
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="We plan to add this functionality in a future Elastic&nbsp;Distribution of OpenTelemetry&nbsp;Python update. Subject to change.

This functionality may be changed or removed in a future release. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features.">
		<span class="applicable-name">EDOT Python</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-preview">
			Planned
		</span>
	</span>
</div>
"""

// Test complex scenarios with multiple lifecycles
type ``mixed lifecycles with ga planned`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: ga 8.8.0, preview 9.0.0
```
"""

    [<Fact>]
    let ``renders GA planned when preview exists alongside GA`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available on Elastic&nbsp;Stack version 8.8.0 and later unless otherwise specified.

If this functionality is unavailable or behaves differently when deployed on ECH, ECE, ECK, or a self-managed installation, it will be indicated on the page.">
		<span class="applicable-name">Stack</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-ga">
			<span class="applicable-version applicable-version-ga">8.8.0</span>
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="We plan to add this functionality in a future Elastic&nbsp;Stack update. Subject to change.

This functionality may be changed or removed in a future release. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features.">
		<span class="applicable-name">Stack</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-preview">
			Planned
		</span>
	</span>
</div>
"""

type ``deprecation planned`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: deprecated 9.1.0
```
"""

    [<Fact>]
    let ``renders deprecation planned for future version`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="We plan to deprecate this functionality in a future Elastic&nbsp;Stack update. Subject to change.">
		<span class="applicable-name">Stack</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-deprecated">
			Deprecation planned
		</span>
	</span>
</div>
"""

type ``removal planned`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: removed 9.1.0
```
"""

    [<Fact>]
    let ``renders removal planned for future version`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="We plan to remove this functionality in a future Elastic&nbsp;Stack update. Subject to change.">
		<span class="applicable-name">Stack</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-removed">
			Removal planned
		</span>
	</span>
</div>
"""

// Test edge cases
type ``unavailable lifecycle`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: unavailable
```
"""

    [<Fact>]
    let ``renders unavailable`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Not available on Elastic&nbsp;Stack unless otherwise specified.">
		<span class="applicable-name">Stack</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-unavailable">
			<span class="applicable-lifecycle applicable-lifecycle-unavailable">Unavailable</span>
		</span>
	</span>
</div>
"""

type ``product all versions`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
product: ga
```
"""

    [<Fact>]
    let ``renders product all versions`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available unless otherwise specified.">
		<span class="applicable-name"></span>
	</span>
</div>
"""

// Test complex mixed scenarios
type ``complex mixed scenario`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: ga 8.8.0
serverless:
  elasticsearch: beta 9.0.0
  observability: preview 9.1.0
deployment:
  ece: ga 8.8.0
  eck: beta 9.0.0
apm_agent_dotnet: ga 9.0.0
apm_agent_java: beta 9.1.0
```
"""

    [<Fact>]
    let ``renders complex mixed scenario`` () =
        markdown |> convertsToHtml """
<div class="applies">
	<span class="applicable-info" data-tippy-content="Available on Elastic&nbsp;Stack version 8.8.0 and later unless otherwise specified.

If this functionality is unavailable or behaves differently when deployed on ECH, ECE, ECK, or a self-managed installation, it will be indicated on the page.">
		<span class="applicable-name">Stack</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-ga">
			<span class="applicable-version applicable-version-ga">8.8.0</span>
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="Available in beta on Serverless&nbsp;Elasticsearch projects version 9.0.0 and later unless otherwise specified.

Beta features are subject to change. The design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features.">
		<span class="applicable-name">Serverless Elasticsearch</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-beta">
			<span class="applicable-lifecycle applicable-lifecycle-beta">Beta</span>
			<span class="applicable-version applicable-version-beta">9.0.0</span>
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="We plan to add this functionality in a future Serverless&nbsp;Observability projects update. Subject to change.

This functionality may be changed or removed in a future release. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features.">
		<span class="applicable-name">Serverless Observability</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-preview">
			Planned
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="Available on Elastic&nbsp;Cloud&nbsp;Enterprise version 8.8.0 and later unless otherwise specified.">
		<span class="applicable-name">ECE</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-ga">
			<span class="applicable-version applicable-version-ga">8.8.0</span>
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="Available in beta on Elastic&nbsp;Cloud&nbsp;on&nbsp;Kubernetes version 9.0.0 and later unless otherwise specified.

Beta features are subject to change. The design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features.">
		<span class="applicable-name">ECK</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-beta">
			<span class="applicable-lifecycle applicable-lifecycle-beta">Beta</span>
			<span class="applicable-version applicable-version-beta">9.0.0</span>
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="Available on Application&nbsp;Performance&nbsp;Monitoring Agent for .NET version 9.0.0 and later unless otherwise specified.">
		<span class="applicable-name">APM Agent .NET</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-ga">
			<span class="applicable-version applicable-version-ga">9.0.0</span>
		</span>
	</span>
	<span class="applicable-info" data-tippy-content="We plan to add this functionality in a future Application&nbsp;Performance&nbsp;Monitoring Agent for Java update. Subject to change.

Beta features are subject to change. The design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features.">
		<span class="applicable-name">APM Agent Java</span>
		<span class="applicable-separator"></span>
		<span class="applicable-meta applicable-meta-beta">
			Planned
		</span>
	</span>
</div>
"""
