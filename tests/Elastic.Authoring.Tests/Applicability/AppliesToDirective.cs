// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;

// Alias needed: file namespace contains 'AppliesToDirective' as a segment, which shadows the type.
using AppliesToDirectiveType = Elastic.Markdown.Myst.Directives.AppliesTo.AppliesToDirective;

namespace Elastic.Authoring.Tests.Applicability;

public class PiggyBackOffYamlFormatting : MarkdownTest
{
	protected override string Markdown =>
		"""
		```yaml {applies_to}
		serverless:
		  security: ga
		  elasticsearch: beta
		  observability: removed
		```
		""";

	[Test, DisplayName("parses to AppliesDirective")]
	public async Task ParsesAppliesDirective()
	{
		var parsesTask = Docs.Converts("index.md").Parses<AppliesToDirectiveType>();
		(await parsesTask).Should().HaveCount(1);
		await parsesTask.AppliesToDirective(new ApplicableTo
		{
			Serverless = new ServerlessProjectApplicability
			{
				Security = Applies("ga"),
				Elasticsearch = Applies("beta"),
				Observability = Applies("removed"),
			},
		});
	}
}

public class PlainBlock : MarkdownTest
{
	protected override string Markdown =>
		"""
		```{applies_to}
		serverless:
		  security: ga
		  elasticsearch: beta
		  observability: removed
		apm_agent_dotnet: ga 9.0
		apm_agent_node: ga 10.0
		```
		""";

	[Test, DisplayName("parses to AppliesDirective")]
	public async Task ParsesAppliesDirective()
	{
		var parsesTask = Docs.Converts("index.md").Parses<AppliesToDirectiveType>();
		(await parsesTask).Should().HaveCount(1);
		await parsesTask.AppliesToDirective(new ApplicableTo
		{
			Serverless = new ServerlessProjectApplicability
			{
				Security = Applies("ga"),
				Elasticsearch = Applies("beta"),
				Observability = Applies("removed"),
			},
			ProductApplicability = new ProductApplicability { ApmAgentDotnet = Applies("ga 9.0"), ApmAgentNode = Applies("ga 10.0"), },
		});
	}
}

public class WarnsOnOldSyntax : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		:hosted: all
		```
		""";

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Test, DisplayName("warns on bad syntax")]
	public async Task WarnsOnBadSyntax() => await Docs.HasWarning("Applies block does not use valid yaml keys: :hosted");
}

public class WarnsOnInvalidKeys : MarkdownTest
{
	protected override string Markdown => """
		```{applies_to}
		hosted: all
		```
		""";

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Test, DisplayName("warns on bad syntax")]
	public async Task WarnsOnBadSyntax() => await Docs.HasWarning("Applies block does not support the following keys: hosted");
}
