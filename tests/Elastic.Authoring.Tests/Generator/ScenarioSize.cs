// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Authoring.Tests.Generator;

/// <summary>
/// Every scenario renders each of its pages through the full pipeline, so the number of pages is the cost of a test.
/// These tests keep the default at the pages a test supplies.
/// </summary>
public class DefaultScenarioOnlyRendersItsOwnPages : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files => [Index("# Index"), Page("other.md", "# Other")];

	[Test, DisplayName("a scenario renders the pages the test supplies and nothing else")]
	public async Task RendersOnlyTheSuppliedPages() => (await Docs.PagesAsync()).Should().Equal("index.md", "other.md");
}

public class RedirectFixturesAreOptIn : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files => [Index("# Index")];

	protected override SetupOptions Options => SetupOptions.WithRedirectFixtures;

	[Test, DisplayName("the redirect fixtures add the redirect target pages")]
	public async Task AddsTheRedirectTargetPages()
	{
		var pages = await Docs.PagesAsync();
		pages.Should().Contain("index.md");
		pages.Should().Contain("testing/redirects/first-page.md");
		pages.Length.Should().BeGreaterThan(20);
	}
}
