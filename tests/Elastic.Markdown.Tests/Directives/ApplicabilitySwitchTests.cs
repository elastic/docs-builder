// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.AppliesSwitch;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ApplicabilitySwitchTests(ITestOutputHelper output) : DirectiveTest<AppliesSwitchBlock>(output,
"""
:::::{applies-switch}

::::{applies-item} stack: preview 9.1
:::{tip}
This feature is in preview for Elastic Stack 9.1.
:::
::::

::::{applies-item} ess: preview 9.1
:::{note}
This feature is available for Elastic Cloud.
:::
::::

::::{applies-item} ece: removed
:::{warning}
This feature has been removed from Elastic Cloud Enterprise.
:::
::::

:::::
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ParsesApplicabilitySwitchItems()
	{
		var items = Block!.OfType<AppliesItemBlock>().ToArray();
		items.Should().NotBeNull().And.HaveCount(3);
		for (var i = 0; i < items.Length; i++)
			items[i].Index.Should().Be(i);
	}

	[Fact]
	public void ParsesAppliesToDefinitions()
	{
		var items = Block!.OfType<AppliesItemBlock>().ToArray();
		items[0].AppliesToDefinition.Should().Be("stack: preview 9.1");
		items[1].AppliesToDefinition.Should().Be("ess: preview 9.1");
		items[2].AppliesToDefinition.Should().Be("ece: removed");
	}

	[Fact]
	public void SetsCorrectDirectiveType()
	{
		Block!.Directive.Should().Be("applies-switch");
		var items = Block.OfType<AppliesItemBlock>().ToArray();
		foreach (var item in items)
			item.Directive.Should().Be("applies-item");
	}
}

public class MultipleApplicabilitySwitchTests(ITestOutputHelper output) : DirectiveTest<AppliesSwitchBlock>(output,
"""
:::::{applies-switch}
::::{applies-item} stack: ga 8.11
Content for GA version
::::
:::::

Paragraph

:::::{applies-switch}
::::{applies-item} stack: preview 9.1
Content for preview version
::::
:::::
"""
)
{
	[Fact]
	public void ParsesMultipleApplicabilitySwitches()
	{
		var switches = Document.OfType<AppliesSwitchBlock>().ToArray();
		switches.Length.Should().Be(2);
		for (var s = 0; s < switches.Length; s++)
		{
			var items = switches[s].OfType<AppliesItemBlock>().ToArray();
			items.Should().NotBeNull().And.HaveCount(1);
			for (var i = 0; i < items.Length; i++)
			{
				items[i].Index.Should().Be(i);
				items[i].AppliesSwitchIndex.Should().Be(switches[s].Line);
			}
		}
	}
}

public class GroupApplicabilitySwitchTests(ITestOutputHelper output) : DirectiveTest<AppliesSwitchBlock>(output,
"""
::::{applies-switch}
:::{applies-item} stack: ga 8.11
Content for GA version
:::

:::{applies-item} stack: preview 9.1
Content for preview version
:::

:::{applies-item} stack: removed
Content for removed version
:::

::::

::::{applies-switch}
:::{applies-item} stack: ga 8.11
Content for GA version
:::

:::{applies-item} stack: preview 9.1
Content for preview version
:::

:::{applies-item} stack: removed
Content for removed version
:::

::::
"""
)
{
	[Fact]
	public void ParsesMultipleApplicabilitySwitches()
	{
		var switches = Document.OfType<AppliesSwitchBlock>().ToArray();
		switches.Length.Should().Be(2);
		for (var s = 0; s < switches.Length; s++)
		{
			var items = switches[s].OfType<AppliesItemBlock>().ToArray();
			items.Should().NotBeNull().And.HaveCount(3);
			for (var i = 0; i < items.Length; i++)
			{
				items[i].Index.Should().Be(i);
				items[i].AppliesSwitchIndex.Should().Be(switches[s].Line);
			}
		}
	}

	[Fact]
	public void ParsesGroup()
	{
		var switches = Document.OfType<AppliesSwitchBlock>().ToArray();
		switches.Length.Should().Be(2);

		foreach (var s in switches)
			s.GetGroupKey().Should().Be("applies-switches");
	}

	[Fact]
	public void ParsesSyncKey()
	{
		var switchBlock = Document.OfType<AppliesSwitchBlock>().First();
		var items = switchBlock.OfType<AppliesItemBlock>().ToArray();
		items.Should().HaveCount(3);

		// Verify all sync keys have the expected hash-based format
		foreach (var item in items)
		{
			item.SyncKey.Should().StartWith("applies-", "Sync key should start with 'applies-' prefix");
			item.SyncKey.Should().MatchRegex(@"^applies-\d+$", "Sync key should be in format 'applies-{hash}'");
		}

		// Verify that different applies_to definitions produce different sync keys
		items[0].SyncKey.Should().NotBe(items[1].SyncKey, "Different applies_to definitions should produce different sync keys");
		items[1].SyncKey.Should().NotBe(items[2].SyncKey, "Different applies_to definitions should produce different sync keys");
		items[0].SyncKey.Should().NotBe(items[2].SyncKey, "Different applies_to definitions should produce different sync keys");
	}

	[Fact]
	public void NormalizesSyncKeyOrder()
	{
		// Test that different orderings of the same applies_to definition generate the same sync key
		var testCases = new[]
		{
			("stack: preview 9.0, ga 9.1", "stack: ga 9.1, preview 9.0"),
			("ess: preview 8.11, ga 8.10", "ess: ga 8.10, preview 8.11"),
			("stack: removed, preview 9.0", "stack: preview 9.0, removed")
		};

		foreach (var (definition1, definition2) in testCases)
		{
			var key1 = AppliesItemBlock.GenerateSyncKey(definition1, Block!.Build.ProductsConfiguration);
			var key2 = AppliesItemBlock.GenerateSyncKey(definition2, Block!.Build.ProductsConfiguration);
			key1.Should().Be(key2, $"Sync keys should be the same for '{definition1}' and '{definition2}'");
		}
	}

	[Fact]
	public void GeneratesConsistentSyncKeysForYamlObjects()
	{
		// Test that YAML object syntax and simple syntax produce the same sync key
		var testCases = new[]
		{
			("stack: ga 9.1", "stack: ga 9.1"), // Same format should produce same key
			("{ ece: all, ess: all }", "deployment: { ece: all, ess: all }"), // YAML object vs deployment object
			("{ stack: ga 9.1 }", "stack: ga 9.1"), // YAML object vs simple syntax
			("{ deployment: { ece: ga 9.0, ess: ga 9.1 } }", "deployment: { ece: ga 9.0, ess: ga 9.1 }"), // Nested YAML objects
		};

		foreach (var (yamlObject, equivalentSyntax) in testCases)
		{
			var key1 = AppliesItemBlock.GenerateSyncKey(yamlObject, Block!.Build.ProductsConfiguration);
			var key2 = AppliesItemBlock.GenerateSyncKey(equivalentSyntax, Block!.Build.ProductsConfiguration);
			key1.Should().Be(key2, $"Sync keys should be the same for YAML object '{yamlObject}' and equivalent syntax '{equivalentSyntax}'");

			// Also verify the key has the expected format
			key1.Should().StartWith("applies-", "Sync key should start with 'applies-' prefix");
			key1.Should().MatchRegex(@"^applies-\d+$", "Sync key should be in format 'applies-{hash}'");
		}
	}
}
