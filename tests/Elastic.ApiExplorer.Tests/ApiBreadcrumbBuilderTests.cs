// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation.Navigation;
using FakeItEasy;

namespace Elastic.ApiExplorer.Tests;

public class ApiBreadcrumbBuilderTests
{
	[Fact]
	public void Parents_Chain_IsRootFirstAndOmitsCurrent()
	{
		var root = Node("/api/es", "Api Overview", parent: null);
		var tag = Node("/api/es/search", "Search", root);
		var op = Leaf("/api/es/search-op", "search", tag);

		var crumbs = op.BreadcrumbParents();

		crumbs.Select(c => c.NavigationTitle).Should().Equal("Api Overview", "Search");
		crumbs.Select(c => c.Url).Should().Equal("/api/es", "/api/es/search");
	}

	[Fact]
	public void Parents_SameTitleAsCurrent_IsKept()
	{
		var root = Node("/api/es", "Api Overview", parent: null);
		var endpoint = Node("/api/es/search", "Run a search", root);
		var op = Leaf("/api/es/search-1", "search", endpoint);

		op.BreadcrumbParents().Select(c => c.NavigationTitle).Should().Equal("Api Overview", "Run a search");
	}

	[Fact]
	public void Parents_HiddenParent_IsKept()
	{
		var root = Node("/api/es", "Api Overview", parent: null);
		var hidden = Node("/api/es/hidden", "Hidden", root, hidden: true);
		var op = Leaf("/api/es/op", "op", hidden);

		op.BreadcrumbParents().Select(c => c.NavigationTitle).Should().Equal("Api Overview", "Hidden");
	}

	[Fact]
	public void Parents_SameUrlAsCurrent_IsOmitted()
	{
		var root = Node("/api/es", "Api Overview", parent: null);
		var endpoint = Node("/api/es/search", "search", root);
		var op = Leaf("/api/es/search", "search", endpoint);

		op.BreadcrumbParents().Select(c => c.Url).Should().Equal("/api/es");
	}

	[Fact]
	public void Parents_DuplicateUrl_KeepsNearest()
	{
		var root = Node("/api/es", "Api Overview", parent: null);
		var classification = Node("/api/es", "Search & Document APIs", root);
		var tag = Node("/api/es/tags/search/", "Search", classification);

		var crumbs = tag.BreadcrumbParents();

		crumbs.Select(c => c.NavigationTitle).Should().Equal("Search & Document APIs");
		crumbs[0].Url.Should().Be("/api/es");
	}

	[Fact]
	public void Landing_BreadcrumbIsTheCatalogLink()
	{
		var landing = new LandingNavigationItem("/api/doc/elasticsearch/");

		var crumbs = ApiBreadcrumbs.Build(landing.Index, "/docs/api/", "Elasticsearch API", onCatalog: false);

		crumbs.Should().ContainSingle();
		crumbs[0].NavigationTitle.Should().Be("APIs");
		crumbs[0].Url.Should().Be("/docs/api/");
	}

	[Fact]
	public void Operation_ReplacesApiOverviewWithSpecName()
	{
		var landing = new LandingNavigationItem("/api/doc/elasticsearch/");
		var tag = Node("/api/doc/elasticsearch/group/search", "Search", landing);
		var op = Leaf("/api/doc/elasticsearch/operation/search", "Run a search", tag);

		var crumbs = ApiBreadcrumbs.Build(op, "/docs/api/", "Elasticsearch API", onCatalog: false);

		crumbs.Select(c => c.NavigationTitle).Should().Equal("APIs", "Elasticsearch API", "Search");
		crumbs.Select(c => c.Url).Should().Equal("/docs/api/", "/api/doc/elasticsearch/", "/api/doc/elasticsearch/group/search");
	}

	[Fact]
	public void Classification_SpecRootCrumbUsesSpecName()
	{
		var landing = new LandingNavigationItem("/api/doc/elasticsearch/");
		var classification = Node("/api/doc/elasticsearch/", "Search", landing);
		var tag = Node("/api/doc/elasticsearch/group/search", "Search APIs", classification);
		var op = Leaf("/api/doc/elasticsearch/operation/search", "Run a search", tag, landing);

		var crumbs = ApiBreadcrumbs.Build(op, "/docs/api/", "Elasticsearch API", onCatalog: false);

		crumbs.Select(c => c.NavigationTitle).Should().Equal("APIs", "Elasticsearch API", "Search APIs");
		crumbs.Select(c => c.Url).Should().Equal("/docs/api/", "/api/doc/elasticsearch/", "/api/doc/elasticsearch/group/search");
	}

	[Fact]
	public void Catalog_OmitsTheCatalogLink()
	{
		var landing = new LandingNavigationItem("/api/");

		var crumbs = ApiBreadcrumbs.Build(landing.Index, "/api/", "API catalog", onCatalog: true);

		crumbs.Should().BeEmpty();
	}

	private static INodeNavigationItem<INavigationModel, INavigationItem> Node(
		string url,
		string title,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		bool hidden = false
	)
	{
		var node = A.Fake<INodeNavigationItem<INavigationModel, INavigationItem>>();
		A.CallTo(() => node.Url).Returns(url);
		A.CallTo(() => node.NavigationTitle).Returns(title);
		A.CallTo(() => node.Hidden).Returns(hidden);
		A.CallTo(() => node.Parent).Returns(parent);
		return node;
	}

	private static INavigationItem Leaf(
		string url,
		string title,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem>? root = null
	)
	{
		var leaf = A.Fake<INavigationItem>();
		A.CallTo(() => leaf.Url).Returns(url);
		A.CallTo(() => leaf.NavigationTitle).Returns(title);
		A.CallTo(() => leaf.Hidden).Returns(false);
		A.CallTo(() => leaf.Parent).Returns(parent);
		if (root is not null)
			A.CallTo(() => leaf.NavigationRoot).Returns(root);
		return leaf;
	}
}
