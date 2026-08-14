// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.Assembler;

/// <summary>
/// Navigation root for a top-level section (e.g. "Guides") that groups multiple
/// documentation-set roots under one tab in the secondary nav bar.
/// Marked as an island so <c>FindIslandRoot()</c> and <c>CreateBackLinks</c>
/// resolve back-link breadcrumbs correctly for pages inside any child root.
/// </summary>
[DebuggerDisplay("{Url}")]
public class SectionNavigation(string title) : IRootNavigationItem<IDocumentationFile, INavigationItem>, IAssignableIslandNavigation
{
	private SectionIndexLeaf? _index;

	public string Title { get; } = title;

	/// Set by <c>SiteNavigation</c> after children are resolved to the first child's index URL.
	public string Url { get; internal set; } = "/";

	/// <inheritdoc />
	public ILeafNavigationItem<IDocumentationFile> Index =>
		_index ??= new SectionIndexLeaf(new SectionIndexPage(Title), Url, this);

	/// <inheritdoc />
	public string NavigationTitle => Title;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => this;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public string Id { get; } = ShortId.Create($"section-{title}");

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown => false;

	/// <inheritdoc />
	public Uri Identifier { get; } = new Uri($"section://{title.ToLowerInvariant().Replace(' ', '-')}");

	/// <inheritdoc />
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; private set; } = [];

	/// <inheritdoc />
	public bool IsIsland { get; set; } = true;

	/// <inheritdoc />
	void IAssignableChildrenNavigation.SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) =>
		NavigationItems = navigationItems;
}

/// <summary>Synthetic page model for a section landing (no backing file on disk).</summary>
public record SectionIndexPage(string NavigationTitle) : IDocumentationFile
{
	/// <inheritdoc />
	public string Title => NavigationTitle;

	/// <inheritdoc />
	public string? Description => null;
}

/// <summary>Synthetic index leaf for a section landing page.</summary>
[DebuggerDisplay("{Url}")]
public class SectionIndexLeaf(SectionIndexPage model, string url, SectionNavigation sectionRoot)
	: ILeafNavigationItem<IDocumentationFile>
{
	/// <inheritdoc />
	public IDocumentationFile Model { get; } = model;

	/// <inheritdoc />
	public string Url { get; } = url;

	/// <inheritdoc />
	public string NavigationTitle => Model.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => sectionRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }
}
