// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Links;
using Elastic.Documentation.Navigation.Floating;

namespace Elastic.Documentation.Navigation.Tests;

internal sealed class DemoProvider : IFloatingSectionProvider
{
	public DemoSection? Section { get; private set; }

	public string Kind => "api";

	public string? HubPath => "api";

	public bool MountsImplicitly { get; init; } = true;

	public IReadOnlyList<IFloatingSection> GetSections() => [Section ??= new DemoSection()];
}

/// <summary>A section whose root exists at once and whose single page appears when <see cref="ResolveAsync"/> runs.</summary>
internal sealed class DemoSection : IFloatingSection
{
	private DemoRoot? _root;

	public int ResolveCount { get; private set; }

	/// <summary>The root this section created, so a test can hide it as a failed API would be.</summary>
	public DemoRoot? Root => _root;

	public string Kind => "api";
	public string Key => "demo";
	public string UrlRoot => "api/doc/demo";

	public IFloatingSectionRoot CreateRoot(INodeNavigationItem<INavigationModel, INavigationItem>? parent, bool hidden = false) =>
		_root = new DemoRoot(parent) { Hidden = hidden };

	public Task ResolveAsync(Cancel ctx)
	{
		ResolveCount++;
		_root!.SetNavigationItems([new DemoPage(_root)]);
		return Task.CompletedTask;
	}

	public FloatingLinkSection? ToLinkSection() => null;
}

internal sealed record DemoModel : INavigationModel;

internal sealed class DemoLandingLeaf(DemoRoot root) : ILeafNavigationItem<DemoModel>
{
	public DemoModel Model { get; } = new();
	public string Url => "/api/doc/demo";
	public string NavigationTitle => "Demo API";
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => root;
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = root;
	public bool Hidden => false;
	public int NavigationIndex { get; set; }
}

internal sealed class DemoPage(DemoRoot root) : ILeafNavigationItem<DemoModel>
{
	public DemoModel Model { get; } = new();
	public string Url => "/api/doc/demo/operation/operation-search";
	public string NavigationTitle => "Search";
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => root;
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = root;
	public bool Hidden => false;

	// A sentinel, so a lookup that wrongly walks into the section would register the page under 7.
	public int NavigationIndex { get; set; } = 7;
}

internal sealed class DemoRoot : IFloatingSectionRoot, IAssignableIslandNavigation
{
	public DemoRoot(INodeNavigationItem<INavigationModel, INavigationItem>? parent)
	{
		Parent = parent;
		Index = new DemoLandingLeaf(this);
	}

	public string Url => Index.Url;
	public string NavigationTitle => Index.NavigationTitle;
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => this;
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public bool Hidden { get; set; }
	public int NavigationIndex { get; set; }
	public bool IsIsland { get; set; }
	public string Id => "demo";
	public ILeafNavigationItem<INavigationModel> Index { get; }
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; private set; } = [];
	public bool IsUsingNavigationDropdown => false;
	public Uri Identifier => new("api://demo");

	public void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) => NavigationItems = navigationItems;
}
