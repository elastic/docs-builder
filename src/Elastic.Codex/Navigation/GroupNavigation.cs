// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;

namespace Elastic.Codex.Navigation;

/// <summary>
/// Root navigation for a group (e.g. observability) that shows the group landing plus all repos in the group as top-level items.
/// Used so the same sidebar appears on the group landing and on every repo page in that group.
/// </summary>
[DebuggerDisplay("{Url}")]
public class GroupNavigation : IRootNavigationItem<IDocumentationFile, INavigationItem>
{
	public GroupNavigation(string groupSlug, string displayTitle, string url, string? description = null, string? icon = null)
	{
		GroupSlug = groupSlug;
		DisplayTitle = displayTitle;
		Description = description;
		Icon = icon;
		Url = url.TrimEnd('/');
		Id = ShortId.Create($"group-{groupSlug}");
		Identifier = new Uri($"codex://group/{groupSlug}");
		var leaf = new GroupIndexLeaf(new GroupIndexPage(displayTitle), Url, this);
		Index = leaf;
		leaf.Parent = this;
	}

	/// <summary>
	/// Gets the group slug (used in URL, e.g. "observability" for /g/observability).
	/// </summary>
	public string GroupSlug { get; }

	/// <summary>
	/// Gets the display title for the group.
	/// </summary>
	public string DisplayTitle { get; }

	/// <summary>
	/// Gets the optional description for the group landing page card.
	/// </summary>
	public string? Description { get; }

	/// <summary>
	/// Gets the optional icon identifier for the group landing page card.
	/// </summary>
	public string? Icon { get; }

	/// <summary>
	/// Gets information about documentation sets in this group for the group landing page.
	/// </summary>
	public FrozenSet<CodexDocumentationSetInfo> DocumentationSetInfos { get; set; } = [];

	/// <inheritdoc />
	public string Url { get; }

	/// <inheritdoc />
	public string NavigationTitle => DisplayTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => this;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public string Id { get; }

	/// <inheritdoc />
	public ILeafNavigationItem<IDocumentationFile> Index { get; }

	/// <inheritdoc />
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; private set; } = [];

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown => false;

	/// <inheritdoc />
	public Uri Identifier { get; }

	/// <inheritdoc />
	void IAssignableChildrenNavigation.SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) =>
		NavigationItems = navigationItems;
}

/// <summary>
/// Virtual index page for a group landing.
/// </summary>
public record GroupIndexPage(string NavigationTitle) : IDocumentationFile;

/// <summary>
/// Leaf navigation item for a group's index (landing) page.
/// </summary>
[DebuggerDisplay("{Url}")]
public class GroupIndexLeaf(
	GroupIndexPage model,
	string url,
	GroupNavigation groupRoot
) : ILeafNavigationItem<IDocumentationFile>
{
	/// <inheritdoc />
	public IDocumentationFile Model { get; } = model;

	/// <inheritdoc />
	public string Url { get; } = url;

	/// <inheritdoc />
	public string NavigationTitle => Model.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => groupRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }
}

/// <summary>
/// Model for a codex nav link to a group landing page.
/// </summary>
public record GroupLinkPage(string NavigationTitle, string Url) : IDocumentationFile;

/// <summary>
/// Leaf in the codex nav that links to a group landing page (/g/slug).
/// </summary>
[DebuggerDisplay("{Url}")]
public class GroupLinkLeaf(
	GroupLinkPage model,
	CodexNavigation codexRoot
) : ILeafNavigationItem<IDocumentationFile>
{
	/// <inheritdoc />
	public IDocumentationFile Model { get; } = model;

	/// <inheritdoc />
	public string Url => model.Url;

	/// <inheritdoc />
	public string NavigationTitle => Model.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => codexRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }
}
