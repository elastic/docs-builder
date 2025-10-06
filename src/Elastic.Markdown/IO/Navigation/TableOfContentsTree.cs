// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Navigation;

namespace Elastic.Markdown.IO.Navigation;

[DebuggerDisplay("Toc: {Depth} {NavigationSource} > ({NavigationItems.Count} items)")]
public class TableOfContentsTree : DocumentationGroup, IRootNavigationItem<MarkdownFile, INavigationItem>
{
	public Uri Source { get; }

	public TableOfContentsTree(
		Uri source,
		BuildContext context,
		NavigationLookups lookups,
		ref int fileIndex)
		: base(".", context, lookups, source, ref fileIndex, 0, null, null)
	{
		NavigationRoot = this;

		Source = source;

		//edge case if a tree only holds a single group, ensure we collapse it down to the root (this)
		if (NavigationItems.Count == 1 && NavigationItems.First() is DocumentationGroup { NavigationItems.Count: 0 })
			NavigationItems = [];


	}

	internal TableOfContentsTree(
		Uri source,
		string folderName,
		BuildContext context,
		NavigationLookups lookups,
		ref int fileIndex,
		int depth,
		IRootNavigationItem<MarkdownFile, INavigationItem> toplevelTree,
		DocumentationGroup? parent
	) : base(folderName, context, lookups, source, ref fileIndex, depth, toplevelTree, parent)
	{
		Source = source;
		NavigationRoot = this;
	}

	protected override IRootNavigationItem<MarkdownFile, INavigationItem> DefaultNavigation => this;

	// We rely on IsPrimaryNavEnabled to determine if we should show the dropdown
	/// <inheritdoc />
	public bool IsUsingNavigationDropdown => false;
}
