// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.AppliesSwitch;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class ApplicabilitySwitchMalformedYamlWithSyncTests() : DirectiveTest<AppliesSwitchBlock>(
	"""
:::::{applies-switch}

::::{applies-item} stack: [ga
:sync: explicit-key
:::{tip}
This item has an applies_to definition that is not valid YAML.
:::
::::

:::::
"""
)
{
	[Test]
	public void ReportsTheDefinitionAsAnError() =>
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("Unable to parse applies_to definition: stack: [ga"));

	[Test]
	public void KeepsTheExplicitSyncKey() => Block!.OfType<AppliesItemBlock>().Single().SyncKey.Should().Be("explicit-key");
}

[InheritsTests]
public class ApplicabilitySwitchMalformedYamlTests() : DirectiveTest<AppliesSwitchBlock>(
	"""
:::::{applies-switch}

::::{applies-item} stack: [ga
:::{tip}
This item has an applies_to definition that is not valid YAML.
:::
::::

:::::
"""
)
{
	[Test]
	public void ReportsTheDefinitionAsAnError() =>
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("Unable to parse applies_to definition: stack: [ga"));

	[Test]
	public void FallsBackToAKeyDerivedFromTheDefinition() =>
		Block!.OfType<AppliesItemBlock>().Single().SyncKey.Should().StartWith("applies-");
}
