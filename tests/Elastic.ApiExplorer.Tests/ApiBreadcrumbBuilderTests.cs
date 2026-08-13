// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Navigation;
using FakeItEasy;

namespace Elastic.ApiExplorer.Tests;

public class ApiBreadcrumbBuilderTests
{
	[Fact]
	public void Collect_ParentChain_RootTitleThenCurrent()
	{
		var root = Node("/api/es", "Api Overview", parent: null);
		var tag = Node("/api/es/search", "Search", root);
		var op = Leaf("/api/es/search-op", "search", tag);

		var crumbs = ApiBreadcrumbBuilder.Collect(op, "Run a search", "Elasticsearch API");

		crumbs.Should().HaveCount(3);
		crumbs[0].Title.Should().Be("Elasticsearch API");
		crumbs[0].Url.Should().Be("/api/es");
		crumbs[1].Title.Should().Be("Search");
		crumbs[1].Url.Should().Be("/api/es/search");
		crumbs[2].Title.Should().Be("Run a search");
		crumbs[2].IsCurrent.Should().BeTrue();
	}

	[Fact]
	public void Collect_SameTitleAsCurrent_Skipped()
	{
		var root = Node("/api/es", "Api Overview", parent: null);
		var endpoint = Node("/api/es/search", "Run a search", root);
		var op = Leaf("/api/es/search-1", "search", endpoint);

		var crumbs = ApiBreadcrumbBuilder.Collect(op, "Run a search", "Elasticsearch API");

		crumbs.Select(c => c.Title).Should().Equal("Elasticsearch API", "Run a search");
	}

	[Fact]
	public void Collect_HiddenParent_Skipped()
	{
		var root = Node("/api/es", "Api Overview", parent: null);
		var hidden = Node("/api/es/hidden", "Hidden", root, hidden: true);
		var op = Leaf("/api/es/op", "op", hidden);

		var crumbs = ApiBreadcrumbBuilder.Collect(op, "Op", "Elasticsearch API");

		crumbs.Select(c => c.Title).Should().Equal("Elasticsearch API", "Op");
	}

	[Fact]
	public void Collect_TagLanding_ClassificationThenCurrent()
	{
		var root = Node("/api/es", "Api Overview", parent: null);
		var classification = Node("/api/es", "Search & Document APIs", root);
		var tag = Node("/api/es/tags/search/", "Search", classification);

		var crumbs = ApiBreadcrumbBuilder.Collect(tag, "Search", "Elasticsearch API");

		crumbs.Select(c => c.Title).Should().Equal("Search & Document APIs", "Search");
		crumbs[0].Url.Should().Be("/api/es");
		crumbs[1].IsCurrent.Should().BeTrue();
	}

	[Fact]
	public void Build_SingleCrumb_ShowsCurrent()
	{
		var landing = Leaf("/api/es", "Api Overview", parent: null);

		var trail = ApiBreadcrumbBuilder.Build(landing, "Elasticsearch API", "Elasticsearch API");

		trail.IsEmpty.Should().BeFalse();
		trail.Head.Should().ContainSingle();
		trail.Head[0].Title.Should().Be("Elasticsearch API");
		trail.Head[0].IsCurrent.Should().BeTrue();
	}

	[Fact]
	public void Split_FourItems_NoOverflow()
	{
		var items = Titles("a", "b", "c", "d");

		var trail = ApiBreadcrumbBuilder.Split(items);

		trail.HasOverflow.Should().BeFalse();
		trail.Head.Select(c => c.Title).Should().Equal("a", "b", "c", "d");
		trail.Tail.Should().BeEmpty();
	}

	[Fact]
	public void Split_FiveItems_FirstTwoOverflowLastTwo()
	{
		var items = Titles("a", "b", "c", "d", "e");

		var trail = ApiBreadcrumbBuilder.Split(items);

		trail.Head.Select(c => c.Title).Should().Equal("a", "b");
		trail.Overflow.Select(c => c.Title).Should().Equal("c");
		trail.Tail.Select(c => c.Title).Should().Equal("d", "e");
	}

	private static IReadOnlyList<ApiBreadcrumb> Titles(params string[] titles) =>
		titles.Select((t, i) => new ApiBreadcrumb(t, i == titles.Length - 1 ? null : $"/{t}")).ToArray();

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
		INodeNavigationItem<INavigationModel, INavigationItem>? parent
	)
	{
		var leaf = A.Fake<INavigationItem>();
		A.CallTo(() => leaf.Url).Returns(url);
		A.CallTo(() => leaf.NavigationTitle).Returns(title);
		A.CallTo(() => leaf.Hidden).Returns(false);
		A.CallTo(() => leaf.Parent).Returns(parent);
		return leaf;
	}
}
