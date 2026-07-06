// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using Elastic.Markdown.Exporters.Elasticsearch;

namespace Elastic.Markdown.Tests.Search;

/// <summary>
/// `navigation_depth` and `navigation_table_of_contents` are `rank_feature` fields with NEGATIVE
/// score impact — the shared contract defaults missing values to 50, which is a penalty. These
/// tests guard that <see cref="ElasticsearchMarkdownExporter.CommonEnrichments"/> never leaves a
/// docs page relying on that default, across every navigation shape it can be called with
/// (root, node, leaf, and the OpenAPI/no-navigation path).
/// </summary>
public class NavigationEnrichmentTests
{
	private const int PenaltyDefault = 50;

	private static DocumentationDocument NewDoc() => new()
	{
		Url = "/docs/reference/some-page",
		Title = "Some Page",
		SearchTitle = "Some Page"
	};

	[Fact]
	public void LandingPageRoot_GetsLowDepthAndNonDefaultToc()
	{
		var root = new FakeRootNavigationItem { NavigationTitle = "Reference" };
		var doc = NewDoc();

		ElasticsearchMarkdownExporter.CommonEnrichments(doc, root);

		doc.NavigationDepth.Should().Be(0);
		doc.NavigationDepth.Should().NotBe(PenaltyDefault);
		doc.NavigationTableOfContents.Should().NotBe(PenaltyDefault);
		doc.NavigationSection.Should().Be("reference");
	}

	[Fact]
	public void ReleaseNotesRoot_IsDampenedRelativeToOtherRootsAtTheSameDepth()
	{
		var releaseNotesRoot = new FakeRootNavigationItem { NavigationTitle = "Release Notes", Parent = new FakeNodeNavigationItem { NavigationTitle = "Parent" } };
		var releaseNotesDoc = NewDoc();
		ElasticsearchMarkdownExporter.CommonEnrichments(releaseNotesDoc, releaseNotesRoot);

		var otherRoot = new FakeRootNavigationItem { NavigationTitle = "Reference", Parent = new FakeNodeNavigationItem { NavigationTitle = "Parent" } };
		var otherDoc = NewDoc();
		ElasticsearchMarkdownExporter.CommonEnrichments(otherDoc, otherRoot);

		releaseNotesDoc.NavigationSection.Should().Be("release notes");
		releaseNotesDoc.NavigationTableOfContents.Should().NotBe(PenaltyDefault);
		// navigation_table_of_contents has NEGATIVE score impact, so a HIGHER value means MORE
		// penalty. Release notes get effectively flattened by product, so its ranking is dampened
		// via a steeper multiplier (4x vs 2x depth, both capped at 48) than a regular root.
		releaseNotesDoc.NavigationTableOfContents.Should().BeGreaterThan(otherDoc.NavigationTableOfContents);
	}

	[Fact]
	public void SectionNode_GetsFixedNonDefaultToc()
	{
		var root = new FakeRootNavigationItem { NavigationTitle = "Reference" };
		var node = new FakeNodeNavigationItem { NavigationTitle = "Elasticsearch", Parent = root };
		var doc = NewDoc();

		ElasticsearchMarkdownExporter.CommonEnrichments(doc, node);

		doc.NavigationDepth.Should().BeGreaterThan(0);
		doc.NavigationDepth.Should().NotBe(PenaltyDefault);
		doc.NavigationTableOfContents.Should().Be(50);
	}

	[Fact]
	public void DeepLeafPage_GetsStructuralDepthAndNonDefaultToc()
	{
		var root = new FakeRootNavigationItem { NavigationTitle = "Reference" };
		var section = new FakeNodeNavigationItem { NavigationTitle = "Elasticsearch", Parent = root };
		var leaf = new FakeLeafNavigationItem { NavigationTitle = "Settings", Parent = section };
		var doc = NewDoc();

		ElasticsearchMarkdownExporter.CommonEnrichments(doc, leaf);

		doc.NavigationDepth.Should().Be(2);
		doc.NavigationDepth.Should().NotBe(PenaltyDefault);
		doc.NavigationTableOfContents.Should().Be(100);
		doc.NavigationSection.Should().Be("elasticsearch");
	}

	[Fact]
	public void NoNavigation_OpenApiPath_FallsBackToExplicitNonPenaltyValues()
	{
		var doc = NewDoc();

		ElasticsearchMarkdownExporter.CommonEnrichments(doc, null);

		// explicit fallback (20), not the contract's penalty default (50)
		doc.NavigationDepth.Should().Be(20);
		doc.NavigationDepth.Should().NotBe(PenaltyDefault);
		doc.NavigationTableOfContents.Should().Be(100);
		doc.NavigationTableOfContents.Should().NotBe(PenaltyDefault);
	}

	[Fact]
	public void ApiContentType_OverridesNavigationSectionRegardlessOfNavigation()
	{
		var root = new FakeRootNavigationItem { NavigationTitle = "Reference" };
		var doc = NewDoc();
		doc.ContentType = "api";

		ElasticsearchMarkdownExporter.CommonEnrichments(doc, root);

		doc.NavigationSection.Should().Be("api");
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
