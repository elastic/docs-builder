// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``AuthoringTests``.``product availability``.``applicable to component``

open Elastic.Documentation.AppliesTo
open Elastic.Markdown.Myst.Directives.AppliesTo
open authoring
open authoring.MarkdownDocumentAssertions
open Swensen.Unquote
open Xunit

// Test Stack applicability scenarios
type ``stack ga future applicability`` () =
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
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
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
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-lifecycle-text="Planned" lifecycle-class="preview" lifecycle-name="Preview" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack beta future version`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: beta 8.8.0
```
"""

    [<Fact>]
    let ``renders beta future version`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-lifecycle-text="Planned" lifecycle-class="beta" lifecycle-name="Beta" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack planned deprecation`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: deprecated 8.7.0
```
"""

    [<Fact>]
    let ``renders deprecation planned`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-lifecycle-text="Deprecation planned" lifecycle-class="deprecated" lifecycle-name="Deprecated" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned for deprecation&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is planned to be deprecated in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack removal planned`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: removed 8.6.0
```
"""

    [<Fact>]
    let ``renders planned for removal`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-lifecycle-text="Removal planned" lifecycle-class="removed" lifecycle-name="Removed" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned for removal&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is planned to be removed in an upcoming Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack ga base version`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: ga
```
"""

    [<Fact>]
    let ``renders ga base version`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-version="8.0+" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available since 8.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

// Test Serverless applicability scenarios
type ``serverless ga future`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
serverless: ga 9.0.0
```
"""

    [<Fact>]
    let ``renders serverless ga planned`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Serverless" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Serverless update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Serverless interfaces and procedures might differ from classic Elastic Stack deployments.&quot;,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
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
<p class="applies applies-block">
	<applies-to-popover badge-key="Serverless Elasticsearch" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Serverless Elasticsearch projects update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="Serverless Observability" badge-lifecycle-text="Planned" lifecycle-class="beta" lifecycle-name="Beta" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Serverless Observability projects update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="Serverless Security" badge-lifecycle-text="Planned" lifecycle-class="preview" lifecycle-name="Preview" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Serverless Security projects update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
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
<p class="applies applies-block">
	<applies-to-popover badge-key="ECE" badge-lifecycle-text="Planned" lifecycle-class="ga" 
	lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" 
	popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Enterprise\u003C/strong\u003E is a self-managed orchestration platform for deploying and managing the Elastic Stack at scale.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Enterprise update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}"
	show-popover="true" is-inline="false">	
</applies-to-popover>
</p>
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
<p class="applies applies-block">
	<applies-to-popover badge-key="ECK" badge-lifecycle-text="Planned" lifecycle-class="beta" lifecycle-name="Beta" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud on Kubernetes\u003C/strong\u003E extends Kubernetes orchestration capabilities to allow you to deploy and manage components of the Elastic Stack.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud on Kubernetes update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
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
<p class="applies applies-block">
	<applies-to-popover badge-key="ECH" badge-lifecycle-text="Planned" lifecycle-class="preview" lifecycle-name="Preview" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Hosted\u003C/strong\u003E is a deployment of the Elastic Stack that\u0027s hosted on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Hosted update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="false">
</applies-to-popover></p>
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
<p class="applies applies-block">
	<applies-to-popover badge-key="Self-Managed" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003ESelf-managed\u003C/strong\u003E deployments are Elastic Stack deployments managed without the assistance of an orchestrator.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Self-managed Elastic deployments update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
	</p>
"""

// Test Product applicability scenarios
type ``apm agents future versions`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
apm_agent_dotnet: ga 9.0.0
apm_agent_java: beta 9.1.0
apm_agent_python: preview 9.2.0
```
"""

    [<Fact>]
    let ``renders APM agents planned`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="APM Agent .NET" badge-lifecycle-text="Planned" lifecycle-class="ga" 
	    lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" 
	    popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic APM .NET agent\u003C/strong\u003E enables you to trace the execution of operations in your .NET applications, sending performance metrics and errors to the Elastic APM server.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Application Performance Monitoring Agent for .NET update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}"
	    show-popover="true" is-inline="false">	
    </applies-to-popover>
	<applies-to-popover badge-key="APM Agent Java" badge-lifecycle-text="Planned" lifecycle-class="beta" 
	    lifecycle-name="Beta" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" 
	    popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic APM Java agent\u003C/strong\u003E enables you to trace the execution of operations in your Java applications, sending performance metrics and errors to the Elastic APM Server.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Application Performance Monitoring Agent for Java update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}"
	    show-popover="true" is-inline="false">
    </applies-to-popover>
	<applies-to-popover badge-key="APM Agent Python" badge-lifecycle-text="Planned" lifecycle-class="preview" 
    	lifecycle-name="Preview" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" 
	    popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic APM Python agent\u003C/strong\u003E enables you to trace the execution of operations in your Python applications, sending performance metrics and errors to the Elastic APM Server.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Application Performance Monitoring Agent for Python update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}"
	    show-popover="true" is-inline="false">	
    </applies-to-popover>
</p>
"""

type ``edot agents future versions`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
edot_dotnet: ga 9.0.0
edot_java: beta 9.1.0
edot_python: preview 9.2.0
```
"""

    [<Fact>]
    let ``renders EDOT agents planned`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="EDOT .NET" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Distribution of OpenTelemetry (EDOT) .NET SDK\u003C/strong\u003E collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Distribution of OpenTelemetry .NET update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="EDOT Java" badge-lifecycle-text="Planned" lifecycle-class="beta" lifecycle-name="Beta" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Distribution of OpenTelemetry (EDOT) Java SDK\u003C/strong\u003E collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Distribution of OpenTelemetry Java update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="EDOT Python" badge-lifecycle-text="Planned" lifecycle-class="preview" lifecycle-name="Preview" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Distribution of OpenTelemetry (EDOT) Python SDK\u003C/strong\u003E collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Distribution of OpenTelemetry Python update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

// Test complex scenarios with multiple lifecycles
type ``mixed unreleased lifecycles falls back to preview`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: ga 8.8.0, preview 8.1.0
```
"""

    [<Fact>]
    let ``renders Preview when GA and Preview both exist for an unreleased entry`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" lifecycle-class="preview" lifecycle-name="Preview" show-lifecycle-name="true" show-version="false" has-multiple-lifecycles="true" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
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
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-lifecycle-text="Deprecation planned" lifecycle-class="deprecated" lifecycle-name="Deprecated" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned for deprecation&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is planned to be deprecated in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
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
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-lifecycle-text="Removal planned" lifecycle-class="removed" lifecycle-name="Removed" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned for removal&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is planned to be removed in an upcoming Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
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
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-version="8.0+" lifecycle-class="unavailable" lifecycle-name="Unavailable" show-lifecycle-name="true" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Unavailable since 8.0&quot;,&quot;lifecycleDescription&quot;:&quot;Elastic Stack doesn\u0027t include this functionality.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
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
<p class="applies applies-block">
	<applies-to-popover badge-version="8.0+" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:null,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available since 8.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""
        
type ``product preview`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
product: preview 1.3.0
```
"""

    [<Fact>]
    let ``renders product preview`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-version="1.3+" lifecycle-class="preview" lifecycle-name="Preview" show-lifecycle-name="true" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:null,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Preview since 1.3&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in technical preview and is not ready for production usage. Technical preview features may change or be removed at any time. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
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
<p class="applies applies-block">
	<applies-to-popover badge-key="Serverless Elasticsearch" badge-lifecycle-text="Planned" lifecycle-class="beta" lifecycle-name="Beta" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Serverless Elasticsearch projects update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="Serverless Observability" badge-lifecycle-text="Planned" lifecycle-class="preview" lifecycle-name="Preview" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Serverless Observability projects update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="Stack" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="ECK" badge-lifecycle-text="Planned" lifecycle-class="beta" lifecycle-name="Beta" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud on Kubernetes\u003C/strong\u003E extends Kubernetes orchestration capabilities to allow you to deploy and manage components of the Elastic Stack.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud on Kubernetes update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="ECE" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Enterprise\u003C/strong\u003E is a self-managed orchestration platform for deploying and managing the Elastic Stack at scale.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Enterprise update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="APM Agent .NET" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic APM .NET agent\u003C/strong\u003E enables you to trace the execution of operations in your .NET applications, sending performance metrics and errors to the Elastic APM server.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Application Performance Monitoring Agent for .NET update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="APM Agent Java" badge-lifecycle-text="Planned" lifecycle-class="beta" lifecycle-name="Beta" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic APM Java agent\u003C/strong\u003E enables you to trace the execution of operations in your Java applications, sending performance metrics and errors to the Elastic APM Server.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Application Performance Monitoring Agent for Java update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack and ece future versions`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: beta 9.1.0
deployment:
  ece: ga 9.1.0
```
"""

    [<Fact>]
    let ``renders stack and ece planned`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-lifecycle-text="Planned" lifecycle-class="beta" lifecycle-name="Beta" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="ECE" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Enterprise\u003C/strong\u003E is a self-managed orchestration platform for deploying and managing the Elastic Stack at scale.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Enterprise update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack empty defaults to ga`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: 
```
"""

    [<Fact>]
    let ``no version defaults to ga`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available since 8.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

// Test missing VersioningSystemId coverage
type ``all products future version coverage`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: ga 9.0.0
serverless: ga 9.0.0
deployment:
  ece: ga 9.0.0
  eck: ga 9.0.0
  ess: ga 9.0.0
  self: ga 9.0.0
product: ga 9.0.0
```
"""

    [<Fact>]
    let ``renders VersioningSystemId coverage`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Serverless" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Serverless update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Serverless interfaces and procedures might differ from classic Elastic Stack deployments.&quot;,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="Stack" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="ECH" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Hosted\u003C/strong\u003E is a deployment of the Elastic Stack that\u0027s hosted on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Hosted update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="ECK" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud on Kubernetes\u003C/strong\u003E extends Kubernetes orchestration capabilities to allow you to deploy and manage components of the Elastic Stack.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud on Kubernetes update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="ECE" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Enterprise\u003C/strong\u003E is a self-managed orchestration platform for deploying and managing the Elastic Stack at scale.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Enterprise update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-key="Self-Managed" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;\u003Cstrong\u003ESelf-managed\u003C/strong\u003E deployments are Elastic Stack deployments managed without the assistance of an orchestrator.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Self-managed Elastic deployments update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
	<applies-to-popover badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:null,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future  update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

// Test multiple lifecycles for same applicability key
// With version inference: ga 8.0, beta 8.1 → ga =8.0 (exact), beta 8.1+ (highest gets GTE)
type ``ga with beta uses version inference`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: ga 8.0.0, beta 8.1.0
```
"""

    [<Fact>]
    let ``renders multiple lifecycles with ellipsis and shows GA lifecycle`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-version="8.0" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="true" show-version="true" has-multiple-lifecycles="true" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available in 8.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack ga released version`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: ga 7.0.0
```
"""

    [<Fact>]
    let ``renders ga since released version`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-version="7.0+" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available since 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack preview released version`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: preview 7.0.0
```
"""

    [<Fact>]
    let ``renders preview since released version`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-version="7.0+" lifecycle-class="preview" lifecycle-name="Preview" show-lifecycle-name="true" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Preview since 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in technical preview and is not ready for production usage. Technical preview features may change or be removed at any time. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack beta released version`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: beta 7.0.0
```
"""

    [<Fact>]
    let ``renders beta since released version`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-version="7.0+" lifecycle-class="beta" lifecycle-name="Beta" show-lifecycle-name="true" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Beta since 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in beta and is not ready for production usage. For beta features, the design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack deprecated released version`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: deprecated 7.0.0
```
"""

    [<Fact>]
    let ``renders deprecated since released version`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-version="7.0+" lifecycle-class="deprecated" lifecycle-name="Deprecated" show-lifecycle-name="true" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Deprecated since 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is deprecated. You can still use it, but it\u0027ll be removed in a future Elastic Stack update.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack removed released version`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: removed 7.0.0
```
"""

    [<Fact>]
    let ``renders removed in released version`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-version="7.0" lifecycle-class="removed" lifecycle-name="Removed" show-lifecycle-name="true" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Removed in 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality was removed. You can no longer use it if you\u0027re running on this version or a later one.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

// Version spec syntax tests (exact and range)
type ``stack ga exact version released`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: ga =7.5
```
"""

    [<Fact>]
    let ``renders ga in exact released version`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-version="7.5" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available in 7.5&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack ga range both released`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: ga 7.0-8.0
```
"""

    [<Fact>]
    let ``renders ga from-to when both ends released`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-version="7.0-8.0" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available from 7.0 to 8.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

type ``stack ga range max unreleased`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: ga 7.0-9.0
```
"""

    [<Fact>]
    let ``renders ga since min when max unreleased`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-version="7.0+" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="true" has-multiple-lifecycles="false" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available since 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""

// Multiple released lifecycles showing both in popover
type ``preview and ga both released`` () =
    static let markdown = Setup.Markdown """
```{applies_to}
stack: preview 7.0, ga 7.5
```
"""

    [<Fact>]
    let ``renders ga badge with both lifecycles in popover`` () =
        markdown |> convertsToHtml """
<p class="applies applies-block">
	<applies-to-popover badge-key="Stack" badge-version="7.5+" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="true" show-version="true" has-multiple-lifecycles="true" popover-data="{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available since 7.5&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;},{&quot;text&quot;:&quot;Preview from 7.0 to 7.4&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in technical preview and is not ready for production usage. Technical preview features may change or be removed at any time. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the release notes for changes.&quot;}" show-popover="true" is-inline="false">
</applies-to-popover>
</p>
"""
