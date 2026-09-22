// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;
using AppliesToRole = Elastic.Markdown.Myst.Roles.AppliesTo.AppliesToRole;

namespace Elastic.Authoring.Tests.Inline.AppliesToRoleTests;

public class ParsesInlineAppliesToRole : MarkdownTest
{
	protected override string Markdown => """

		This is an inline {applies_to}`stack: preview 9.1` element.
		""";

	[Fact(DisplayName = "parses to AppliesDirective")]
	public async Task ParsesToAppliesDirective()
	{
		var directives = await Docs.Converts("index.md").Parses<AppliesToRole>();
		directives.Should().HaveCount(1);
		await Task.FromResult(directives).AppliesToDirective(new ApplicableTo { Stack = Applies("preview 9.1.0") });
	}

	[Fact(DisplayName = "validate HTML: generates link and alt attr")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			@"
		<p>This is an inline
			<span class=""applies applies-inline"">
		        <applies-to-popover badge-key=""Stack"" badge-lifecycle-text=""Planned"" lifecycle-class=""preview"" lifecycle-name=""Preview"" show-lifecycle-name=""false"" show-version=""false"" has-multiple-lifecycles=""false"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Planned&quot;,&quot;lifecycleDescription&quot;:&quot;We plan to add this functionality in a future Elastic Stack update. Subject to changes.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""true"">
		        </applies-to-popover>
			</span>
			element.</p>
		"
		);
}

public class ParsesNestedEssMoniker : MarkdownTest
{
	protected override string Markdown => """

		This is an inline {applies_to}`ess: preview` element.
		""";

	[Fact(DisplayName = "parses to AppliesDirective")]
	public async Task ParsesToAppliesDirective()
	{
		var directives = await Docs.Converts("index.md").Parses<AppliesToRole>();
		directives.Should().HaveCount(1);
		await Task.FromResult(directives).AppliesToDirective(new ApplicableTo
		{
			Deployment = new DeploymentApplicability { Ess = Applies("preview") }
		});
	}
}

public class ParsesNestedEchMonikerAsEss : MarkdownTest
{
	protected override string Markdown => """

		This is an inline {applies_to}`ech: preview` element.
		""";

	[Fact(DisplayName = "parses to AppliesDirective")]
	public async Task ParsesToAppliesDirective()
	{
		var directives = await Docs.Converts("index.md").Parses<AppliesToRole>();
		directives.Should().HaveCount(1);
		await Task.FromResult(directives).AppliesToDirective(new ApplicableTo
		{
			Deployment = new DeploymentApplicability { Ess = Applies("preview") }
		});
	}
}

public class ParsesPreviewShortcut : MarkdownTest
{
	protected override string Markdown => """

		This is an inline {preview}`9.1` element.
		""";

	[Fact(DisplayName = "parses to AppliesDirective")]
	public async Task ParsesToAppliesDirective()
	{
		var directives = await Docs.Converts("index.md").Parses<AppliesToRole>();
		directives.Should().HaveCount(1);
		await Task.FromResult(directives).AppliesToDirective(new ApplicableTo { Product = Applies("preview 9.1.0") });
	}
}

public class ParsesAppliesToWithoutVersionInTable : MarkdownTest
{
	protected override string Markdown =>
		"""
		| col1 | col2                         |
		|------|------------------------------|
		| test | {applies_to}`ece: removed`   |
		""";

	[Fact(DisplayName = "parses to AppliesDirective")]
	public async Task ParsesToAppliesDirective()
	{
		var directives = await Docs.Converts("index.md").Parses<AppliesToRole>();
		directives.Should().HaveCount(1);
		await Task.FromResult(directives).AppliesToDirective(new ApplicableTo
		{
			Deployment = new DeploymentApplicability { Ece = Applies("removed") }
		});
	}
}

public class ParsesAppliesToWithTextAfterwards : MarkdownTest
{
	protected override string Markdown => """
		{applies_to}`ece: removed` hello world
		""";

	[Fact(DisplayName = "parses to AppliesDirective")]
	public async Task ParsesToAppliesDirective()
	{
		var directives = await Docs.Converts("index.md").Parses<AppliesToRole>();
		directives.Should().HaveCount(1);
		await Task.FromResult(directives).AppliesToDirective(new ApplicableTo
		{
			Deployment = new DeploymentApplicability { Ece = Applies("removed") }
		});
	}
}

public class ParsesMultipleAppliesToInOneLine : MarkdownTest
{
	protected override string Markdown => """
		{applies_to}`ece: removed` {applies_to}`ece: removed`
		""";

	[Fact(DisplayName = "parses to AppliesDirective")]
	public async Task ParsesToAppliesDirective()
	{
		var directives = await Docs.Converts("index.md").Parses<AppliesToRole>();
		directives.Should().HaveCount(2);
		await Task.FromResult(directives).AppliesToDirective(new ApplicableTo
		{
			Deployment = new DeploymentApplicability { Ece = Applies("removed") }
		});
	}
}

public class RenderPreviewForGaInFutureVersion : MarkdownTest
{
	protected override string Markdown => """

		This is an inline {applies_to}`stack: preview 8.0, ga 8.1` element.
		""";

	[Fact(DisplayName = "parses to AppliesDirective")]
	public async Task ParsesToAppliesDirective()
	{
		var directives = await Docs.Converts("index.md").Parses<AppliesToRole>();
		directives.Should().HaveCount(1);
		await Task.FromResult(directives).AppliesToDirective(new ApplicableTo { Stack = Applies("ga 8.1, preview 8.0") });
	}

	[Fact(DisplayName = "validate HTML: generates single combined badge")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			@"
		<p>This is an inline
			<span class=""applies applies-inline"">
		        <applies-to-popover badge-key=""Stack"" badge-version=""8.0"" lifecycle-class=""preview"" lifecycle-name=""Preview"" show-lifecycle-name=""true"" show-version=""true"" has-multiple-lifecycles=""true"" popover-data=""{&quot;productDescription&quot;:&quot;The \u003Cstrong\u003EElastic Stack\u003C/strong\u003E includes Elastic\u0027s core products such as Elasticsearch, Kibana, Logstash, and Beats.&quot;,&quot;availabilityItems&quot;:[{&quot;text&quot;:&quot;Preview in 8.0&quot;,&quot;lifecycleDescription&quot;:&quot;This functionality is in technical preview and is ready for evaluation. Use with caution in production; it is not recommended for mission-critical workloads. Elastic will work to fix any issues, but features in technical preview are not subject to the support SLA of official GA features. Specific Support terms apply.&quot;}],&quot;additionalInfo&quot;:&quot;Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.&quot;,&quot;showVersionNote&quot;:true,&quot;versionNote&quot;:&quot;This documentation corresponds to the latest patch available for each minor version. If you\u0027re not using the latest patch, check the \u003Ca href=\u0022https://www.elastic.co/docs/release-notes\u0022\u003Erelease notes\u003C/a\u003E for changes.&quot;}"" show-popover=""true"" is-inline=""true"">
		        </applies-to-popover>
			</span>
			element.</p>
		"
		);
}
