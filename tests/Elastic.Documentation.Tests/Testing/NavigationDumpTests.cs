// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Documentation.Tests.Testing;

/// <summary>
/// Navigation.Tests snapshots <see cref="NavigationDump.ToText"/>, so a bug here would be accepted into every one of those snapshots.
/// In particular a broken <c>[wrong parent]</c> check would hide the regression it exists to catch.
/// </summary>
public class NavigationDumpTests
{
	[Test]
	public void ToText_ListsTheIndexFirstThenTheChildren_IndentedByDepth_WithFlagsAndTrimmedTypeNames()
	{
		var island = new FakeNode("/docs/island", "Island", "Island index") { IsIsland = true };
		var root = new FakeNode("/docs", "Docs", "Docs index");
		root.Add(new FakeLeaf("/docs/a", "A"));
		root.Add(new FakeLeaf("/docs/b", "B") { Hidden = true });
		root.Add(island);
		island.Add(new FakeGenericLeaf<string>("/docs/island/x", "X"));

		NavigationDump
			.ToText(root)
			.Should()
			.MatchInlineSnapshot(
				"""
			FakeNode  /docs  "Docs"
			  index: FakeLeaf  /docs  "Docs index"
			  FakeLeaf  /docs/a  "A"
			  FakeLeaf  /docs/b  "B" [hidden]
			  FakeNode  /docs/island  "Island" [island]
			    index: FakeLeaf  /docs/island  "Island index"
			    FakeGenericLeaf  /docs/island/x  "X"
			"""
			);
	}

	[Test]
	public void ToText_ChildWhoseParentIsAnotherNode_IsFlaggedWrongParent()
	{
		var other = new FakeNode("/docs/other", "Other", "Other index");
		var root = new FakeNode("/docs", "Docs", "Docs index");
		root.Add(new FakeLeaf("/docs/fine", "Fine"));
		// listed under root, but it points back at another node
		root.Add(new FakeLeaf("/docs/stray", "Stray"), parent: other);
		root.Add(other);

		NavigationDump
			.ToText(root)
			.Should()
			.MatchInlineSnapshot(
				"""
			FakeNode  /docs  "Docs"
			  index: FakeLeaf  /docs  "Docs index"
			  FakeLeaf  /docs/fine  "Fine"
			  FakeLeaf  /docs/stray  "Stray" [wrong parent]
			  FakeNode  /docs/other  "Other"
			    index: FakeLeaf  /docs/other  "Other index"
			"""
			);
	}

	[Test]
	public void ToText_IndexPage_IsNotCheckedForItsParent()
	{
		// a virtual file's index has a different parent than its node, so it must not be flagged
		var root = new FakeNode("/docs", "Docs", "Docs index");
		root.Index.Parent = new FakeNode("/elsewhere", "Elsewhere", "Elsewhere index");

		NavigationDump
			.ToText(root)
			.Should()
			.MatchInlineSnapshot("""
			FakeNode  /docs  "Docs"
			  index: FakeLeaf  /docs  "Docs index"
			""");
	}

	private sealed class FakeModel : INavigationModel;

	private class FakeLeaf(string url, string title) : ILeafNavigationItem<INavigationModel>
	{
		public string Url { get; } = url;
		public string NavigationTitle { get; } = title;
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => null!;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden { get; init; }
		public bool IsIsland { get; init; }
		public int NavigationIndex { get; set; }
		public INavigationModel Model { get; } = new FakeModel();
	}

	private sealed class FakeGenericLeaf<T>(string url, string title) : FakeLeaf(url, title);

	private sealed class FakeNode : FakeLeaf, INodeNavigationItem<INavigationModel, INavigationItem>
	{
		private readonly List<INavigationItem> _children = [];

		public FakeNode(string url, string title, string indexTitle) : base(url, title) =>
			Index = new FakeLeaf(url, indexTitle) { Parent = null };

		public string Id => Url;
		public FakeLeaf Index { get; }
		ILeafNavigationItem<INavigationModel> INodeNavigationItem<INavigationModel, INavigationItem>.Index => Index;
		public IReadOnlyCollection<INavigationItem> NavigationItems => _children;

		public void Add(FakeLeaf child, INodeNavigationItem<INavigationModel, INavigationItem>? parent = null)
		{
			child.Parent = parent ?? this;
			_children.Add(child);
		}
	}
}
