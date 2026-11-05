// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Navigation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Links;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Floating;

namespace Elastic.ApiExplorer.Floating;

/// <summary>
/// Mounts the API pages of one API key where a toc has <c>- api: &lt;key&gt;</c>. The key is the one the API's URL
/// uses: <c>/api/doc/{key}/</c>.
/// </summary>
/// <remarks>
/// The provider is created before the <see cref="OpenApiGenerator"/> exists, because the docset needs the provider
/// to build its navigation and the generator needs the docset's Markdown renderer. The build calls
/// <see cref="Attach"/> once the generator exists and before the docset resolves its sections. A build that
/// skips the API Explorer never attaches one, and its sections stay empty and hidden.
/// </remarks>
public sealed class ApiFloatingSectionProvider(BuildContext context) : IFloatingSectionProvider
{
	/// <summary>The generator that builds the API trees. It stays unset when the build skips the API Explorer.</summary>
	internal OpenApiGenerator? Generator { get; private set; }

	public string Kind => "api";

	/// <summary>The page that lists every API, <c>/api/</c> under the site prefix.</summary>
	public string? HubPath => "api";

	public void Attach(OpenApiGenerator generator) => Generator = generator;

	public IReadOnlyList<IFloatingSection> GetSections() =>
		[
			.. OpenApiGenerator.ApiConfigsToRender(context).Select(
				c => (IFloatingSection)new ApiFloatingSection(c.Key, c.Value, this, context)
			)
		];

	public bool TryGetSection(string key, [NotNullWhen(true)] out IFloatingSection? section)
	{
		section = null;
		foreach (var (_, urlKey, config) in OpenApiGenerator.ApiConfigsToRender(context))
		{
			if (!string.Equals(urlKey, key, StringComparison.OrdinalIgnoreCase))
				continue;
			section = new ApiFloatingSection(urlKey, config, this, context);
			return true;
		}

		return false;
	}
}

internal sealed class ApiFloatingSection(
	string urlKey,
	ResolvedApiConfiguration config,
	ApiFloatingSectionProvider provider,
	BuildContext context
) : IFloatingSection
{
	private ApiFloatingRoot? _root;
	private ApiProductTrees? _trees;

	public string Kind => "api";

	public string Key => urlKey;

	public string UrlRoot => $"api/doc/{urlKey}";

	public IFloatingSectionRoot CreateRoot(INodeNavigationItem<INavigationModel, INavigationItem>? parent) =>
		_root = new ApiFloatingRoot(
			urlKey,
			ApiUrlBuilder.ProductRoot(context.UrlPathPrefix, urlKey),
			config.Product.DisplayName ?? urlKey,
			parent
		);

	public async Task ResolveAsync(Cancel ctx)
	{
		// Nothing mounted this section, so there is no root to fill.
		if (_root is null)
			return;

		if (provider.Generator is not { } generator)
		{
			_root.Hidden = true;
			return;
		}

		ApiProductTrees trees;
		try
		{
			trees = await generator.TreesFor(urlKey, config, ctx).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			// The generator awaits the same cached task and reports the failure with its stack trace, so don't report it twice.
			_root.Hidden = true;
			return;
		}

		// No main document means no page at the unversioned URL, so the root links nowhere.
		// The generator has already reported why a spec could not be loaded.
		if (!trees.Resolved.Documents.Any(d => d.Version.Moniker == "main"))
		{
			_root.Hidden = true;
			return;
		}

		// Only the main tree is visible. Older versions stay reachable for links but out of the sitemap and first-level walks.
		// The version trees keep no parent: the API pages render with these trees as their own roots.
		for (var i = 0; i < trees.Navigations.Count; i++)
			trees.Navigations[i].Hidden = trees.Resolved.Documents[i].Version.Moniker != "main";

		// The catalog titles an API the same way, so the navigation and the API hub agree.
		var main = trees.Resolved.Documents.First(d => d.Version.Moniker == "main");
		_root.Title = main.Document.Info?.Title ?? _root.Title;

		_trees = trees;
		_root.SetNavigationItems(trees.Navigations);
	}

	public FloatingLinkSection? ToLinkSection() => _trees is null ? null : ApiLinkSections.Create(urlKey, _trees);
}

internal static class ApiLinkSections
{
	/// <summary>The API Explorer owns its routing, so it states where each version lives. Only <c>main</c> is visible and current.</summary>
	private static FloatingLinkVersion Version(string apiKey, string moniker) =>
		new()
		{
			Name = moniker,
			Root = ApiUrlBuilder.ProductRoot(null, ApiUrlBuilder.ProductSuffix(apiKey, moniker)).Trim('/'),
			Hidden = moniker != "main",
			Current = moniker == "main"
		};

	/// <summary>
	/// One page path once, with the versions that contain it. A collapsed operation also answers to its former
	/// per-operation paths, which become aliases of the canonical path, kept per version.
	/// </summary>
	public static FloatingLinkSection Create(string apiKey, ApiProductTrees trees)
	{
		var monikers = trees.Resolved.Documents.Select(d => d.Version.Moniker).Take(FloatingLinkSection.MaxVersions).ToArray();
		var pages = new Dictionary<string, ulong>(StringComparer.Ordinal);
		var aliases = new Dictionary<string, Dictionary<string, string[]>>(StringComparer.Ordinal);

		for (var bit = 0; bit < monikers.Length; bit++)
		{
			var mask = 1UL << bit;

			var formerPaths = new List<(string Canonical, string Alias)>();
			foreach (var (name, all) in trees.PageNames[monikers[bit]])
			{
				var page = name.Trim('/');
				var canonical = all[0].Trim('/');
				if (page == canonical)
					pages[page] = pages.GetValueOrDefault(page) | mask;
				else
					formerPaths.Add((canonical, page));
			}

			// sorted, so a rebuild that changes nothing writes a byte-identical links.json
			if (formerPaths.Count > 0)
			{
				aliases[monikers[bit]] = formerPaths
					.GroupBy(p => p.Canonical, StringComparer.Ordinal)
					.OrderBy(g => g.Key, StringComparer.Ordinal)
					.ToDictionary(
						g => g.Key,
						g => g.Select(p => p.Alias).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
						StringComparer.Ordinal
					);
			}
		}

		return new FloatingLinkSection
		{
			Versions = [.. monikers.Select(moniker => Version(apiKey, moniker))],
			Pages = pages,
			Aliases = aliases.Count == 0 ? null : aliases
		};
	}
}

internal sealed class ApiFloatingRoot : IFloatingSectionRoot, IAssignableIslandNavigation
{
	private readonly RootIndexLeaf _index;

	public ApiFloatingRoot(string key, string url, string title, INodeNavigationItem<INavigationModel, INavigationItem>? parent)
	{
		Id = key;
		Parent = parent;
		_index = new RootIndexLeaf(url, title, this);
	}

	/// <summary>The title in the navigation. It starts as the product name and becomes the spec title once the spec is read.</summary>
	public string Title { get => _index.NavigationTitle; set => _index.NavigationTitle = value; }

	public string Url => _index.Url;
	public string NavigationTitle => _index.NavigationTitle;
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => this;
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public bool Hidden { get; set; }
	public int NavigationIndex { get; set; }
	public bool IsIsland { get; set; }
	public string Id { get; }
	public ILeafNavigationItem<INavigationModel> Index => _index;
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; private set; } = [];
	public bool IsUsingNavigationDropdown => false;
	public Uri Identifier => new($"api://{Url}");

	public void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) => NavigationItems = navigationItems;

	private sealed class RootIndexLeaf(string url, string title, ApiFloatingRoot root) : ILeafNavigationItem<ApiLanding>
	{
		public ApiLanding Model { get; } = new();
		public string Url { get; } = url;
		public string NavigationTitle { get; set; } = title;
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => root;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = root;
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
	}
}
