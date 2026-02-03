// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Isolated.Node;

namespace Elastic.Codex.Navigation;

/// <summary>
/// Root navigation for a documentation codex that composes multiple isolated documentation sets.
/// Unlike SiteNavigation, CodexNavigation uses a simplified structure with optional category grouping.
/// </summary>
[DebuggerDisplay("{Url}")]
public class CodexNavigation : IRootNavigationItem<IDocumentationFile, INavigationItem>, INavigationTraversable
{
	/// <summary>
	/// Creates a new codex navigation from a codex configuration and documentation set navigations.
	/// </summary>
	/// <param name="configuration">The codex configuration.</param>
	/// <param name="context">The documentation context for error reporting.</param>
	/// <param name="documentationSetNavigations">The documentation set navigations keyed by name.</param>
	public CodexNavigation(
		CodexConfiguration configuration,
		ICodexDocumentationContext context,
		IReadOnlyDictionary<string, IDocumentationSetNavigation> documentationSetNavigations)
	{
		Url = NormalizeSitePrefix(configuration.SitePrefix) ?? "/docs";

		// Initialize root properties
		NavigationRoot = this;
		Parent = null;
		Hidden = false;
		Id = ShortId.Create("codex");
		IsUsingNavigationDropdown = false;
		NavigationTitle = configuration.Title;

		// Create the codex index page
		var codexIndexPage = new CodexIndexPage(configuration.Title);
		var codexIndexLeaf = new CodexIndexLeaf(codexIndexPage, this);

		// Build navigation items from configuration
		var items = new List<INavigationItem> { codexIndexLeaf };
		var documentationSetInfos = new List<CodexDocumentationSetInfo>();
		var navigationIndex = 0;

		// Group documentation sets by category
		var categories = new Dictionary<string, CategoryNavigation>();

		foreach (var docSetRef in configuration.DocumentationSets)
		{
			var repoName = docSetRef.ResolvedRepoName;

			// Find the matching documentation set navigation
			if (!documentationSetNavigations.TryGetValue(repoName, out var docSetNav))
			{
				context.EmitError($"Documentation set '{docSetRef.Name}' (repo_name: {repoName}) not found in built documentation sets");
				continue;
			}

			// Calculate the path prefix for this documentation set
			string pathPrefix;
			INodeNavigationItem<INavigationModel, INavigationItem> parentNode;

			if (!string.IsNullOrEmpty(docSetRef.Category))
			{
				// Get or create the category navigation
				if (!categories.TryGetValue(docSetRef.Category, out var categoryNav))
				{
					var categoryPathPrefix = $"{Url}/{docSetRef.Category}";
					var categoryDisplayTitle = FormatCategoryTitle(docSetRef.Category);
					categoryNav = new CategoryNavigation(
						docSetRef.Category,
						categoryDisplayTitle,
						categoryPathPrefix,
						this,
						this)
					{
						NavigationIndex = ++navigationIndex
					};
					categories[docSetRef.Category] = categoryNav;
					items.Add(categoryNav);
				}

				pathPrefix = $"{Url}/{docSetRef.Category}/{repoName}";
				parentNode = categoryNav;
			}
			else
			{
				pathPrefix = $"{Url}/{repoName}";
				parentNode = this;
			}

			// Re-home the documentation set navigation to the codex
			if (docSetNav is INavigationHomeAccessor homeAccessor)
			{
				homeAccessor.HomeProvider = new NavigationHomeProvider(pathPrefix, this);
			}

			// Get the actual navigation root (which implements the navigation interfaces)
			if (docSetNav is IRootNavigationItem<IDocumentationFile, INavigationItem> rootNavItem)
			{
				rootNavItem.Parent = parentNode;
				rootNavItem.NavigationIndex = ++navigationIndex;

				if (string.IsNullOrEmpty(docSetRef.Category))
				{
					// Direct child of codex
					items.Add(rootNavItem);
				}
				else if (categories.TryGetValue(docSetRef.Category, out var categoryNav))
				{
					// Add to category's children
					var categoryChildren = categoryNav.NavigationItems.ToList();
					categoryChildren.Add(rootNavItem);
					((IAssignableChildrenNavigation)categoryNav).SetNavigationItems(categoryChildren);
				}

				// Collect info for codex index display
				var pageCount = CountPages(rootNavItem);
				documentationSetInfos.Add(new CodexDocumentationSetInfo
				{
					Name = repoName,
					Title = docSetRef.DisplayName ?? rootNavItem.NavigationTitle ?? repoName,
					Url = rootNavItem.Url,
					Category = docSetRef.Category,
					PageCount = pageCount,
					Icon = docSetRef.Icon
				});
			}
		}

		// Set up index and navigation items
		Index = codexIndexLeaf;
		NavigationItems = items.Skip(1).ToArray(); // Skip the index leaf

		// Update navigation indices
		_ = this.UpdateNavigationIndex(context);

		// Build navigation lookup tables
		NavigationDocumentationFileLookup = [];
		NavigationIndexedByOrder = this.BuildNavigationLookups(NavigationDocumentationFileLookup);

		// Store documentation set infos for rendering
		DocumentationSetInfos = documentationSetInfos.ToFrozenSet();
	}

	/// <summary>
	/// Gets information about all documentation sets for rendering on the codex index.
	/// </summary>
	public FrozenSet<CodexDocumentationSetInfo> DocumentationSetInfos { get; }

	/// <inheritdoc />
	public Uri Identifier { get; } = new Uri("codex://");

	/// <inheritdoc />
	public string Url { get; }

	/// <inheritdoc />
	public string NavigationTitle { get; }

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden { get; }

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public string Id { get; }

	/// <inheritdoc />
	public ILeafNavigationItem<IDocumentationFile> Index { get; }

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown { get; }

	/// <inheritdoc />
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }

	/// <inheritdoc />
	void IAssignableChildrenNavigation.SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) =>
		throw new NotSupportedException("SetNavigationItems is not supported on CodexNavigation");

	/// <inheritdoc />
	public ConditionalWeakTable<IDocumentationFile, INavigationItem> NavigationDocumentationFileLookup { get; }

	/// <inheritdoc />
	public FrozenDictionary<int, INavigationItem> NavigationIndexedByOrder { get; }

	/// <summary>
	/// Normalizes the site prefix to ensure it has a leading slash and no trailing slash.
	/// </summary>
	private static string? NormalizeSitePrefix(string? sitePrefix)
	{
		if (string.IsNullOrWhiteSpace(sitePrefix))
			return null;

		var normalized = sitePrefix.Trim();

		if (!normalized.StartsWith('/'))
			normalized = "/" + normalized;

		normalized = normalized.TrimEnd('/');

		return normalized;
	}

	/// <summary>
	/// Formats a category name for display (e.g., "developer-tools" -> "Developer Tools").
	/// </summary>
	private static string FormatCategoryTitle(string categoryName)
	{
		if (string.IsNullOrEmpty(categoryName))
			return categoryName;

		// Replace hyphens and underscores with spaces, then title case
		var words = categoryName
			.Replace('-', ' ')
			.Replace('_', ' ')
			.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		return string.Join(" ", words.Select(w =>
			char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
	}

	/// <summary>
	/// Counts the number of pages in a navigation tree.
	/// </summary>
	private static int CountPages(INavigationItem item)
	{
		var count = 0;

		if (item is ILeafNavigationItem<IDocumentationFile>)
			count = 1;

		if (item is INodeNavigationItem<INavigationModel, INavigationItem> node)
		{
			count = 1; // Count the index
			foreach (var child in node.NavigationItems)
				count += CountPages(child);
		}

		return count;
	}
}

/// <summary>
/// Represents the leaf navigation item for the codex's index page.
/// </summary>
[DebuggerDisplay("{Url}")]
public class CodexIndexLeaf(CodexIndexPage model, CodexNavigation parent) : ILeafNavigationItem<IDocumentationFile>
{
	/// <inheritdoc />
	public IDocumentationFile Model { get; } = model;

	/// <inheritdoc />
	public string Url => parent.Url;

	/// <inheritdoc />
	public string NavigationTitle => Model.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => parent;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }
}
