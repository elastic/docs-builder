// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Navigation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
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

	public void Attach(OpenApiGenerator generator) => Generator = generator;

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

	public string Kind => "api";

	public string Key => urlKey;

	public string UrlRoot => $"api/doc/{urlKey}";

	public IFloatingSectionRoot CreateRoot(INodeNavigationItem<INavigationModel, INavigationItem>? parent) =>
		_root = new ApiFloatingRoot(ApiUrlBuilder.ProductRoot(context.UrlPathPrefix, urlKey), config.Product.DisplayName ?? urlKey, parent);

	public async Task ResolveAsync(Cancel ctx)
	{
		if (provider.Generator is not { } generator)
		{
			_root!.Hidden = true;
			return;
		}

		var trees = await generator.TreesFor(urlKey, config, ctx).ConfigureAwait(false);
		if (trees.Navigations.Count == 0)
		{
			// the generator has already reported why the spec could not be loaded
			_root!.Hidden = true;
			return;
		}

		// Only the main tree is visible. Older versions stay reachable for links but out of the sitemap and first-level walks.
		// The version trees keep no parent: the API pages render with these trees as their own roots.
		for (var i = 0; i < trees.Navigations.Count; i++)
			trees.Navigations[i].Hidden = trees.Resolved.Documents[i].Version.Moniker != "main";

		_root!.SetNavigationItems(trees.Navigations);
	}
}

internal sealed class ApiFloatingRoot : IFloatingSectionRoot, IAssignableIslandNavigation
{
	public ApiFloatingRoot(string url, string title, INodeNavigationItem<INavigationModel, INavigationItem>? parent)
	{
		Parent = parent;
		Index = new ApiIndexLeafNavigation<ApiLanding>(new ApiLanding(), url, title, this, this);
	}

	public string Url => Index.Url;
	public string NavigationTitle => Index.NavigationTitle;
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => this;
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public bool Hidden { get; set; }
	public int NavigationIndex { get; set; }
	public bool IsIsland { get; set; }
	public string Id => Index.NavigationTitle;
	public ILeafNavigationItem<INavigationModel> Index { get; }
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; private set; } = [];
	public bool IsUsingNavigationDropdown => false;
	public Uri Identifier => new($"api://{Url}");

	public void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) => NavigationItems = navigationItems;
}
