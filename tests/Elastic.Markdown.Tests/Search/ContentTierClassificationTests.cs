// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Navigation;
using Elastic.Internal.Search;
using Elastic.Markdown.Exporters.Elasticsearch;

namespace Elastic.Markdown.Tests.Search;

/// <summary>
/// content_tier is a shared keyword field (primary/reference/supplementary/peripheral) with a
/// NEUTRAL default of "reference" — unlike navigation_depth/navigation_table_of_contents which
/// default to a penalty. docs-builder classifies its own docs/api taxonomy; these tests pin that
/// classification to the shared <see cref="ContentTiers"/> constants so it can't silently drift
/// from the values website-search-data agrees on.
/// </summary>
public class ContentTierClassificationTests
{
	[Fact]
	public void ReleaseNotesRoot_IsPeripheral()
	{
		var root = new FakeRootNavigationItem { NavigationTitle = "Release Notes" };

		var tier = ElasticsearchMarkdownExporter.ClassifyContentTier(root, "/docs/release-notes/elasticsearch");

		tier.Should().Be(ContentTiers.Peripheral);
	}

	[Fact]
	public void ReleaseNotesUrlWithoutNavigation_IsPeripheral()
	{
		var tier = ElasticsearchMarkdownExporter.ClassifyContentTier(null, "/docs/release-notes/8.15.0");

		tier.Should().Be(ContentTiers.Peripheral);
	}

	[Theory]
	[InlineData("deprecated features")]
	[InlineData("plugins")]
	[InlineData("glossary")]
	public void SupplementarySection_IsSupplementary(string sectionTitle)
	{
		var root = new FakeRootNavigationItem { NavigationTitle = "Reference" };
		var leaf = new FakeLeafNavigationItem { NavigationTitle = sectionTitle, Parent = root };

		var tier = ElasticsearchMarkdownExporter.ClassifyContentTier(leaf, "/docs/reference/some-page");

		tier.Should().Be(ContentTiers.Supplementary);
	}

	[Fact]
	public void PluginExtendUrl_IsSupplementary()
	{
		var tier = ElasticsearchMarkdownExporter.ClassifyContentTier(null, "/docs/extend/logstash");

		tier.Should().Be(ContentTiers.Supplementary);
	}

	[Theory]
	[InlineData("get started")]
	[InlineData("getting started")]
	[InlineData("overview")]
	public void GetStartedOrOverviewSection_IsPrimary(string sectionTitle)
	{
		var root = new FakeRootNavigationItem { NavigationTitle = "Reference" };
		var leaf = new FakeLeafNavigationItem { NavigationTitle = sectionTitle, Parent = root };

		var tier = ElasticsearchMarkdownExporter.ClassifyContentTier(leaf, "/docs/reference/some-page");

		tier.Should().Be(ContentTiers.Primary);
	}

	[Fact]
	public void SectionRootPage_IsPrimary()
	{
		var root = new FakeRootNavigationItem { NavigationTitle = "Elasticsearch" };

		var tier = ElasticsearchMarkdownExporter.ClassifyContentTier(root, "/docs/reference/elasticsearch");

		tier.Should().Be(ContentTiers.Primary);
	}

	[Fact]
	public void OrdinaryLeafPage_DefaultsToReference()
	{
		var root = new FakeRootNavigationItem { NavigationTitle = "Reference" };
		var section = new FakeNodeNavigationItem { NavigationTitle = "Elasticsearch", Parent = root };
		var leaf = new FakeLeafNavigationItem { NavigationTitle = "Settings", Parent = section };

		var tier = ElasticsearchMarkdownExporter.ClassifyContentTier(leaf, "/docs/reference/elasticsearch/settings");

		tier.Should().Be(ContentTiers.Reference);
	}

	[Fact]
	public void NoNavigation_DefaultsToReference()
	{
		var tier = ElasticsearchMarkdownExporter.ClassifyContentTier(null, "/docs/reference/some-page");

		tier.Should().Be(ContentTiers.Reference);
	}

	private sealed class FakeNavigationModel : INavigationModel;

	private abstract class FakeNavigationItemBase : INavigationItem
	{
		// GetParents() dedupes by Url, so each fake needs a distinct one — not a fixed constant.
		public required string NavigationTitle { get; init; }
		public string Url { get; init; } = Guid.NewGuid().ToString();
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => null!;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden { get; init; }
		public int NavigationIndex { get; set; }
	}

	private sealed class FakeLeafNavigationItem : FakeNavigationItemBase, ILeafNavigationItem<INavigationModel>
	{
		public INavigationModel Model { get; } = new FakeNavigationModel();
	}

	private class FakeNodeNavigationItem : FakeNavigationItemBase, INodeNavigationItem<INavigationModel, INavigationItem>
	{
		public string Id => NavigationTitle;
		public ILeafNavigationItem<INavigationModel> Index => null!;
		public IReadOnlyCollection<INavigationItem> NavigationItems { get; init; } = [];
	}

	private sealed class FakeRootNavigationItem : FakeNodeNavigationItem, IRootNavigationItem<INavigationModel, INavigationItem>
	{
		public bool IsUsingNavigationDropdown => false;
		public Uri Identifier => new("https://elastic.co/docs");
		public void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) { }
	}
}
