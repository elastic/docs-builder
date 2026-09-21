// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;

// Alias needed: file namespace contains 'AppliesToDirective' as a segment, which shadows the type.
using AppliesToDirectiveType = Elastic.Markdown.Myst.Directives.AppliesTo.AppliesToDirective;

namespace Elastic.Authoring.Tests.Applicability.ApplicableToComponent;

public class StackGaFutureApplicability : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga 9.0.0
		```
		""";

	[Fact(DisplayName = "parses to AppliesDirective")]
	public async Task ParsesAppliesDirective()
	{
		var parsesTask = Docs.Converts("index.md").Parses<AppliesToDirectiveType>();
		(await parsesTask).Should().HaveCount(1);
		await parsesTask.AppliesToDirective(new ApplicableTo { Stack = Applies("ga 9.0.0") });
	}

	[Fact(DisplayName = "renders GA with version")]
	public async Task RendersGaWithVersion() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackPreviewFutureVersion : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: preview 9.1.0
		```
		""";

	[Fact(DisplayName = "renders preview future version as planned")]
	public async Task RendersPreviewFutureVersionAsPlanned() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Planned"" lifecycle-class=""preview"" lifecycle-name=""Preview"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackBetaFutureVersion : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: beta 8.8.0
		```
		""";

	[Fact(DisplayName = "renders beta future version")]
	public async Task RendersBetaFutureVersion() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Planned"" lifecycle-class=""beta"" lifecycle-name=""Beta"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackExperimentalFutureVersion : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: experimental 9.1.0
		```
		""";

	[Fact(DisplayName = "parses to AppliesDirective")]
	public async Task ParsesAppliesDirective()
	{
		var parsesTask = Docs.Converts("index.md").Parses<AppliesToDirectiveType>();
		(await parsesTask).Should().HaveCount(1);
		await parsesTask.AppliesToDirective(new ApplicableTo { Stack = Applies("experimental 9.1.0") });
	}

	[Fact(DisplayName = "renders experimental future version as planned")]
	public async Task RendersExperimentalFutureVersionAsPlanned() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Planned"" lifecycle-class=""experimental"" lifecycle-name=""Experimental"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackPlannedDeprecation : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: deprecated 8.7.0
		```
		""";

	[Fact(DisplayName = "renders deprecation planned")]
	public async Task RendersDeprecationPlanned() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Deprecation planned"" lifecycle-class=""deprecated"" lifecycle-name=""Deprecated"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned for deprecation&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is planned to be deprecated in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackRemovalPlanned : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: removed 8.6.0
		```
		""";

	[Fact(DisplayName = "renders planned for removal")]
	public async Task RendersPlannedForRemoval() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Removal planned"" lifecycle-class=""removed"" lifecycle-name=""Removed"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned for removal&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is planned to be removed in an upcoming Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackGaNoVersion : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga
		```
		""";

	[Fact(DisplayName = "renders ga without version in badge or popover")]
	public async Task RendersGaWithoutVersionInBadgeOrPopover() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class ServerlessGa : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		serverless: ga
		```
		""";

	[Fact(DisplayName = "renders serverless ga")]
	public async Task RendersServerlessGa() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Serverless"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Serverless interfaces and procedures might differ from classic Elastic Stack deployments.&quot;,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false""></applies-to-popover>
</p>
"
		);
}

public class ServerlessIndividualProjects : MarkdownTest
{
	protected override string Markdown =>
		"""
		```{applies_to}
		serverless:
		  elasticsearch: ga 9.0.0
		  observability: beta 9.1.0
		  security: preview 9.2.0
		```
		""";

	[Fact(DisplayName = "renders serverless individual projects")]
	public async Task RendersServerlessIndividualProjects() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Serverless Elasticsearch"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Serverless Elasticsearch projects update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""Serverless Observability"" badge-lifecycle-text=""Planned"" lifecycle-class=""beta"" lifecycle-name=""Beta"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Serverless Observability projects update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""Serverless Security"" badge-lifecycle-text=""Planned"" lifecycle-class=""preview"" lifecycle-name=""Preview"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Serverless Security projects update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class ServerlessVectorDatabase : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		serverless:
		  vectordb: ga
		```
		""";

	[Fact(DisplayName = "renders serverless vector database")]
	public async Task RendersServerlessVectorDatabase() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Serverless Vector Database"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Serverless interfaces and procedures might differ from classic Elastic Stack deployments.&quot;,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false""></applies-to-popover>
</p>
"
		);
}

public class DeploymentEce : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		deployment:
		  ece: ga 9.0.0
		```
		""";

	[Fact(DisplayName = "renders ECE deployment")]
	public async Task RendersEceDeployment() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""ECE"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Enterprise\u003C/strong\u003E is a self-managed orchestration platform for deploying and managing the Elastic Stack at scale.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Enterprise update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class DeploymentEck : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		deployment:
		  eck: beta 9.0.0
		```
		""";

	[Fact(DisplayName = "renders ECK deployment")]
	public async Task RendersEckDeployment() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""ECK"" badge-lifecycle-text=""Planned"" lifecycle-class=""beta"" lifecycle-name=""Beta"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud on Kubernetes\u003C/strong\u003E extends Kubernetes orchestration capabilities to allow you to deploy and manage components of the Elastic Stack.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud on Kubernetes update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class DeploymentEss : MarkdownTest
{
	protected override string Markdown =>
		"""
		```{applies_to}
		deployment:
		  ess: preview 9.0.0
		```
		""";

	[Fact(DisplayName = "renders ECH deployment")]
	public async Task RendersEchDeployment() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""ECH"" badge-lifecycle-text=""Planned"" lifecycle-class=""preview"" lifecycle-name=""Preview"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Hosted\u003C/strong\u003E lets you manage and configure one or more deployments of the versioned Elastic Stack, hosted on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Hosted update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class DeploymentSelfManaged : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		deployment:
		  self: ga 9.0.0
		```
		""";

	[Fact(DisplayName = "renders self-managed deployment")]
	public async Task RendersSelfManagedDeployment() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Self-managed"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003ESelf-managed\u003C/strong\u003E deployments are Elastic Stack deployments managed without the assistance of an orchestrator.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Self-managed Elastic deployments update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false""></applies-to-popover>
</p>
"
		);
}

public class ApmAgentsFutureVersions : MarkdownTest
{
	protected override string Markdown =>
		"""
		```{applies_to}
		apm_agent_dotnet: ga 9.0.0
		apm_agent_java: beta 9.1.0
		apm_agent_python: preview 9.2.0
		```
		""";

	[Fact(DisplayName = "renders APM agents planned")]
	public async Task RendersApmAgentsPlanned() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""APM Agent .NET"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic APM .NET agent\u003C/strong\u003E enables you to trace the execution of operations in your .NET applications, sending performance metrics and errors to the Elastic APM server.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Application Performance Monitoring Agent for .NET update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""APM Agent Java"" badge-lifecycle-text=""Planned"" lifecycle-class=""beta"" lifecycle-name=""Beta"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic APM Java agent\u003C/strong\u003E enables you to trace the execution of operations in your Java applications, sending performance metrics and errors to the Elastic APM Server.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Application Performance Monitoring Agent for Java update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""APM Agent Python"" badge-lifecycle-text=""Planned"" lifecycle-class=""preview"" lifecycle-name=""Preview"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic APM Python agent\u003C/strong\u003E enables you to trace the execution of operations in your Python applications, sending performance metrics and errors to the Elastic APM Server.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Application Performance Monitoring Agent for Python update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class EdotAgentsFutureVersions : MarkdownTest
{
	protected override string Markdown =>
		"""
		```{applies_to}
		edot_dotnet: ga 9.0.0
		edot_java: beta 9.1.0
		edot_python: preview 9.2.0
		```
		""";

	[Fact(DisplayName = "renders EDOT agents planned")]
	public async Task RendersEdotAgentsPlanned() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""EDOT .NET"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Distribution of OpenTelemetry (EDOT) .NET SDK\u003C/strong\u003E collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Distribution of OpenTelemetry .NET update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""EDOT Java"" badge-lifecycle-text=""Planned"" lifecycle-class=""beta"" lifecycle-name=""Beta"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Distribution of OpenTelemetry (EDOT) Java SDK\u003C/strong\u003E collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Distribution of OpenTelemetry Java update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""EDOT Python"" badge-lifecycle-text=""Planned"" lifecycle-class=""preview"" lifecycle-name=""Preview"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Distribution of OpenTelemetry (EDOT) Python SDK\u003C/strong\u003E collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Distribution of OpenTelemetry Python update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class MixedUnreleasedLifecyclesFallsBackToPlanned : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga 8.8.0, preview 8.1.0
		```
		""";

	[Fact(DisplayName = "renders Planned when GA and Preview are both unreleased")]
	public async Task RendersPlannedWhenGaAndPreviewAreBothUnreleased() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""true"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class DeprecationPlanned : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: deprecated 9.1.0
		```
		""";

	[Fact(DisplayName = "renders deprecation planned for future version")]
	public async Task RendersDeprecationPlannedForFutureVersion() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Deprecation planned"" lifecycle-class=""deprecated"" lifecycle-name=""Deprecated"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned for deprecation&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is planned to be deprecated in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class RemovalPlanned : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: removed 9.1.0
		```
		""";

	[Fact(DisplayName = "renders removal planned for future version")]
	public async Task RendersRemovalPlannedForFutureVersion() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Removal planned"" lifecycle-class=""removed"" lifecycle-name=""Removed"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned for removal&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is planned to be removed in an upcoming Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class UnavailableLifecycle : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: unavailable
		```
		""";

	[Fact(DisplayName = "renders unavailable")]
	public async Task RendersUnavailable() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" lifecycle-class=""unavailable"" lifecycle-name=""Unavailable"" show-lifecycle-name=""true"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Unavailable&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is not available in Elastic Stack.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class ProductAllVersions : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		product: ga
		```
		""";

	[Fact(DisplayName = "renders product all versions")]
	public async Task RendersProductAllVersions() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:null,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false""></applies-to-popover>
</p>
"
		);
}

public class ProductPreview : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		product: preview 1.3.0
		```
		""";

	[Fact(DisplayName = "renders product preview")]
	public async Task RendersProductPreview() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-version=""1.3+"" lifecycle-class=""preview"" lifecycle-name=""Preview"" show-lifecycle-name=""true"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:null,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Preview since 1.3&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in technical preview and is ready for evaluation. Use with caution in production; it is not recommended for mission-critical workloads. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class ComplexMixedScenario : MarkdownTest
{
	protected override string Markdown =>
		"""
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
		""";

	[Fact(DisplayName = "renders complex mixed scenario")]
	public async Task RendersComplexMixedScenario() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Serverless Elasticsearch"" badge-lifecycle-text=""Planned"" lifecycle-class=""beta"" lifecycle-name=""Beta"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Serverless Elasticsearch projects update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""Serverless Observability"" badge-lifecycle-text=""Planned"" lifecycle-class=""preview"" lifecycle-name=""Preview"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Serverless Observability projects update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""ECK"" badge-lifecycle-text=""Planned"" lifecycle-class=""beta"" lifecycle-name=""Beta"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud on Kubernetes\u003C/strong\u003E extends Kubernetes orchestration capabilities to allow you to deploy and manage components of the Elastic Stack.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud on Kubernetes update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""ECE"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Enterprise\u003C/strong\u003E is a self-managed orchestration platform for deploying and managing the Elastic Stack at scale.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Enterprise update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""APM Agent .NET"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic APM .NET agent\u003C/strong\u003E enables you to trace the execution of operations in your .NET applications, sending performance metrics and errors to the Elastic APM server.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Application Performance Monitoring Agent for .NET update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""APM Agent Java"" badge-lifecycle-text=""Planned"" lifecycle-class=""beta"" lifecycle-name=""Beta"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic APM Java agent\u003C/strong\u003E enables you to trace the execution of operations in your Java applications, sending performance metrics and errors to the Elastic APM Server.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Application Performance Monitoring Agent for Java update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackAndEceFutureVersions : MarkdownTest
{
	protected override string Markdown =>
		"""
		```{applies_to}
		stack: beta 9.1.0
		deployment:
		  ece: ga 9.1.0
		```
		""";

	[Fact(DisplayName = "renders stack and ece planned")]
	public async Task RendersStackAndEcePlanned() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Planned"" lifecycle-class=""beta"" lifecycle-name=""Beta"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
	<applies-to-popover badge-key=""ECE"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Enterprise\u003C/strong\u003E is a self-managed orchestration platform for deploying and managing the Elastic Stack at scale.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Enterprise update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackEmptyDefaultsToGa : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack:
		```
		""";

	[Fact(DisplayName = "no version defaults to ga")]
	public async Task NoVersionDefaultsToGa() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class AllProductsFutureVersionCoverage : MarkdownTest
{
	protected override string Markdown =>
		"""
		```{applies_to}
		stack: ga 9.0.0
		serverless: ga
		deployment:
		  ece: ga 9.0.0
		  eck: ga 9.0.0
		  ess: ga 9.0.0
		  self: ga 9.0.0
		product: ga 9.0.0
		```
		""";

	[Fact(DisplayName = "renders VersioningSystemId coverage")]
	public async Task RendersVersioningSystemIdCoverage() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Serverless"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Serverless\u003C/strong\u003E projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Serverless interfaces and procedures might differ from classic Elastic Stack deployments.&quot;,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false""></applies-to-popover>
	<applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false""></applies-to-popover>
	<applies-to-popover badge-key=""ECH"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Hosted\u003C/strong\u003E lets you manage and configure one or more deployments of the versioned Elastic Stack, hosted on Elastic Cloud.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Hosted update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false""></applies-to-popover>
	<applies-to-popover badge-key=""ECK"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud on Kubernetes\u003C/strong\u003E extends Kubernetes orchestration capabilities to allow you to deploy and manage components of the Elastic Stack.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud on Kubernetes update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false""></applies-to-popover>
	<applies-to-popover badge-key=""ECE"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003EElastic Cloud Enterprise\u003C/strong\u003E is a self-managed orchestration platform for deploying and managing the Elastic Stack at scale.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Cloud Enterprise update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false""></applies-to-popover>
	<applies-to-popover badge-key=""Self-managed"" badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;\u003Cstrong\u003ESelf-managed\u003C/strong\u003E deployments are Elastic Stack deployments managed without the assistance of an orchestrator.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Self-managed Elastic deployments update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false""></applies-to-popover>
	<applies-to-popover badge-lifecycle-text=""Planned"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:null,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future  update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:null,&quot;showVersionNote&quot;:false,&quot;versionNote&quot;:null}"" show-popover=""true"" is-inline=""false""></applies-to-popover>
</p>
"
		);
}

public class GaWithBetaUsesVersionInference : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga 8.0.0, beta 8.1.0
		```
		""";

	[Fact(DisplayName = "renders multiple lifecycles with ellipsis and shows GA lifecycle")]
	public async Task RendersMultipleLifecyclesWithEllipsisAndShowsGaLifecycle() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""8.0"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""true"" show-version=""true"" has-multiple-lifecycles=""true"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available in 8.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackGaReleasedVersion : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga 7.0.0
		```
		""";

	[Fact(DisplayName = "renders ga since released version")]
	public async Task RendersGaSinceReleasedVersion() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.0+"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available since 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackPreviewReleasedVersion : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: preview 7.0.0
		```
		""";

	[Fact(DisplayName = "renders preview since released version")]
	public async Task RendersPreviewSinceReleasedVersion() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.0+"" lifecycle-class=""preview"" lifecycle-name=""Preview"" show-lifecycle-name=""true"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Preview since 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in technical preview and is ready for evaluation. Use with caution in production; it is not recommended for mission-critical workloads. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackBetaReleasedVersion : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: beta 7.0.0
		```
		""";

	[Fact(DisplayName = "renders beta since released version")]
	public async Task RendersBetaSinceReleasedVersion() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.0+"" lifecycle-class=""beta"" lifecycle-name=""Beta"" show-lifecycle-name=""true"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Beta since 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in beta and is not ready for production usage. For beta features, the design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackDeprecatedReleasedVersion : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: deprecated 7.0.0
		```
		""";

	[Fact(DisplayName = "renders deprecated since released version")]
	public async Task RendersDeprecatedSinceReleasedVersion() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.0+"" lifecycle-class=""deprecated"" lifecycle-name=""Deprecated"" show-lifecycle-name=""true"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Deprecated since 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is deprecated. You can still use it, but it\u0027ll be removed in a future Elastic Stack update.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackRemovedReleasedVersion : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: removed 7.0.0
		```
		""";

	[Fact(DisplayName = "renders removed in released version")]
	public async Task RendersRemovedInReleasedVersion() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.0"" lifecycle-class=""removed"" lifecycle-name=""Removed"" show-lifecycle-name=""true"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Removed in 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality was removed. You can no longer use it if you\u0027re running on this version or a later one.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackGaExactVersionReleased : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga =7.5
		```
		""";

	[Fact(DisplayName = "renders ga in exact released version")]
	public async Task RendersGaInExactReleasedVersion() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.5"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available in 7.5&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackGaRangeBothReleased : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga 7.0-8.0
		```
		""";

	[Fact(DisplayName = "renders ga from-to when both ends released")]
	public async Task RendersGaFromToWhenBothEndsReleased() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.0-8.0"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available from 7.0 to 8.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class StackGaRangeMaxUnreleased : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga 7.0-9.0
		```
		""";

	[Fact(DisplayName = "renders ga since min when max unreleased")]
	public async Task RendersGaSinceMinWhenMaxUnreleased() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.0+"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available since 7.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class PreviewAndGaBothReleased : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: preview 7.0, ga 7.5
		```
		""";

	[Fact(DisplayName = "renders ga badge with both lifecycles in popover")]
	public async Task RendersGaBadgeWithBothLifecyclesInPopover() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.5+"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""true"" show-version=""true"" has-multiple-lifecycles=""true"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available since 7.5&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;},{&quot;text&quot;:&quot;Preview from 7.0 to 7.4&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in technical preview and is ready for evaluation. Use with caution in production; it is not recommended for mission-critical workloads. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class ExplicitPatchVersionWithExclamationMark : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: preview 7.5.4!
		```
		""";

	[Fact(DisplayName = "renders patch version when explicitly requested with exclamation mark")]
	public async Task RendersPatchVersionWhenExplicitlyRequestedWithExclamationMark() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.5.4+"" lifecycle-class=""preview"" lifecycle-name=""Preview"" show-lifecycle-name=""true"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Preview since 7.5.4&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in technical preview and is ready for evaluation. Use with caution in production; it is not recommended for mission-critical workloads. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class PatchVersionHiddenWithoutExclamationMark : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: preview 7.5.4
		```
		""";

	[Fact(DisplayName = "hides patch version when no exclamation mark used")]
	public async Task HidesPatchVersionWhenNoExclamationMarkUsed() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.5+"" lifecycle-class=""preview"" lifecycle-name=""Preview"" show-lifecycle-name=""true"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Preview since 7.5&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in technical preview and is ready for evaluation. Use with caution in production; it is not recommended for mission-critical workloads. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class RangeWithExplicitPatchOnBothEnds : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: beta 7.0.3!-7.5.2!
		```
		""";

	[Fact(DisplayName = "renders range with patch versions when both have exclamation marks")]
	public async Task RendersRangeWithPatchVersionsWhenBothHaveExclamationMarks() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.0.3-7.5.2"" lifecycle-class=""beta"" lifecycle-name=""Beta"" show-lifecycle-name=""true"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Beta from 7.0.3 to 7.5.2&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in beta and is not ready for production usage. For beta features, the design and code is less mature than official GA features and is being provided as-is with no warranties. Beta features are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class RangeWithExplicitPatchOnlyOnMinimumVersion : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga 7.0.5!-7.5
		```
		""";

	[Fact(DisplayName = "renders range with patch on mininum version only when explicit operator is used")]
	public async Task RendersRangeWithPatchOnMinimumVersionOnlyWhenExplicitOperatorIsUsed() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.0.5-7.5"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available from 7.0.5 to 7.5&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class RangeWithExplicitPatchOnlyOnMaximumVersion : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga 7.0-7.5.3!
		```
		""";

	[Fact(DisplayName = "renders range with patch on maxinum version only when explicit operator is used")]
	public async Task RendersRangeWithPatchOnMaximumVersionOnlyWhenExplicitOperatorIsUsed() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.0-7.5.3"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available from 7.0 to 7.5.3&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}

public class ExactVersionWithExplicitPatch : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		stack: ga =7.5.3!
		```
		""";

	[Fact(DisplayName = "renders exact version with patch when explicit operator is used")]
	public async Task RendersExactVersionWithPatchWhenExplicitOperatorIsUsed() =>
		await Docs.ConvertsToHtml(
			@"
<p class=""applies applies-block"">
	<applies-to-popover badge-key=""Stack"" badge-version=""7.5.3"" lifecycle-class=""ga"" lifecycle-name=""GA"" show-lifecycle-name=""false"" show-version=""true"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Generally available in 7.5.3&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is generally available and ready for production usage.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""false"">
</applies-to-popover>
</p>
"
		);
}
