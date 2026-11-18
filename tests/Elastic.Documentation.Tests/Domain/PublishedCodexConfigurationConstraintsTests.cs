// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Codex;
using Elastic.Documentation.Diagnostics;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class PublishedCodexConfigurationConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(
				PublishedCodexConfigurationConstraints.All,
				GenAuto.Of<PublishedCodexConfiguration>(
					AutoMode.Plausible,
					o => o.From(PublishedCodexConfigurationConstraints.All).Normalize<CodexConfiguration>(
						c => new CodexConfiguration { Environment = "internal", Title = c.Title }
					)
				)
			)
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	private static IReadOnlyDictionary<string, Func<PublishedCodexConfiguration, PublishedCodexConfiguration>> CounterExamples =>
		new Dictionary<string, Func<PublishedCodexConfiguration, PublishedCodexConfiguration>>
		{
			["environment-required"] = _ => new PublishedCodexConfiguration(new CodexConfiguration())
		};

	[Test]
	public void Check_CodexWithoutEnvironment_IsAnErrorThatNamesTheField()
	{
		var findings = PublishedCodexConfigurationConstraints
			.All
			.Report(new PublishedCodexConfiguration(new CodexConfiguration()))
			.ToDiagnostics("codex.yml");

		findings.Should().ContainSingle().Which.Severity.Should().Be(Severity.Error);
		findings[0].Message.Should().Be("Codex configuration must specify an 'environment' (e.g., 'internal', 'security').");
	}

	[Test]
	public void Check_CodexWithEnvironment_HasNoFindings() =>
		PublishedCodexConfigurationConstraints
			.All
			.Report(new PublishedCodexConfiguration(new CodexConfiguration { Environment = "internal" }))
			.ToDiagnostics("codex.yml")
			.Should()
			.BeEmpty();
}
