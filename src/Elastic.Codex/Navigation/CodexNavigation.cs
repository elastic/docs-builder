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
/// Unlike SiteNavigation, CodexNavigation uses a simplified structure with optional group grouping.
/// </summary>
[DebuggerDisplay("{Url}")]
public class CodexNavigation : IRootNavigationItem<IDocumentationFile, INavigationItem>, INavigationTraversable
{
	/// <summary>
	/// Creates a new codex navigation from a codex configuration, documentation set references, and navigations.
	/// </summary>
	public CodexNavigation(
		CodexConfiguration configuration,
		IReadOnlyList<CodexDocumentationSetReference> documentationSetReferences,
		ICodexDocumentationContext context,
		IReadOnlyDictionary<string, IDocumentationSetNavigation> documentationSetNavigations)
	{
		Url = string.IsNullOrEmpty(configuration.SitePrefix) ? "" : configuration.SitePrefix;
		NavigationRoot = this;
		Parent = null;
		Hidden = false;
		Id = ShortId.Create("codex");
		IsUsingNavigationDropdown = false;
		NavigationTitle = configuration.Title;

		var codexIndexLeaf = new CodexIndexLeaf(new CodexIndexPage(configuration.Title), this);
		var builder = new NavigationBuilder(this, context, documentationSetNavigations, configuration);
		var result = builder.Build(documentationSetReferences);

		Index = codexIndexLeaf;
		NavigationItems = result.NavigationItems;
		GroupNavigations = result.Groups.Values.ToFrozenSet();
		DocumentationSetInfos = result.DocumentationSetInfos.ToFrozenSet();

		// Don't call UpdateNavigationIndex here — it would mutate NavigationIndex on
		// items shared with individual DocumentationSet instances, corrupting their
		// NavigationIndexedByOrder lookups and breaking prev/next buttons.
		// Codex-level pages (landing, group) don't use prev/next navigation,
		// so empty traversal lookups are sufficient.
		NavigationDocumentationFileLookup = [];
		NavigationIndexedByOrder = FrozenDictionary<int, INavigationItem>.Empty;
	}

	/// <summary>
	/// Encapsulates the logic for building navigation items from configuration.
	/// </summary>
	private sealed class NavigationBuilder(
		CodexNavigation codex,
		ICodexDocumentationContext context,
		IReadOnlyDictionary<string, IDocumentationSetNavigation> documentationSetNavigations,
		CodexConfiguration configuration)
	{
		private readonly List<INavigationItem> _items = [];
		private readonly List<CodexDocumentationSetInfo> _docSetInfos = [];
		private readonly Dictionary<string, GroupNavigation> _groups = [];
		private readonly Dictionary<string, List<CodexDocumentationSetInfo>> _groupDocSetInfos = [];
		private int _navigationIndex;

		public sealed record BuildResult(
			IReadOnlyCollection<INavigationItem> NavigationItems,
			Dictionary<string, GroupNavigation> Groups,
			List<CodexDocumentationSetInfo> DocumentationSetInfos);

		public BuildResult Build(IReadOnlyList<CodexDocumentationSetReference> docSetRefs)
		{
			foreach (var docSetRef in docSetRefs)
				ProcessDocumentationSet(docSetRef);

			FinalizeGroupDocumentationSetInfos();

			return new BuildResult(_items.ToArray(), _groups, _docSetInfos);
		}

		private void ProcessDocumentationSet(CodexDocumentationSetReference docSetRef)
		{
			var repoName = docSetRef.ResolvedRepoName;

			if (!documentationSetNavigations.TryGetValue(repoName, out var docSetNav))
			{
				context.EmitError($"Documentation set '{docSetRef.Name}' (repo_name: {repoName}) not found");
				return;
			}

			if (docSetNav is not IRootNavigationItem<IDocumentationFile, INavigationItem> rootNavItem)
				return;

			var pathPrefix = $"{codex.Url}/r/{repoName}";
			var docSetInfo = CreateDocumentationSetInfo(docSetRef, rootNavItem, repoName);
			_docSetInfos.Add(docSetInfo);

			if (!string.IsNullOrEmpty(docSetRef.Group))
				AttachToGroup(docSetRef, docSetNav, rootNavItem, pathPrefix, docSetInfo);
			else
				AttachToCodexRoot(docSetNav, rootNavItem, pathPrefix);
		}

		private CodexDocumentationSetInfo CreateDocumentationSetInfo(
			CodexDocumentationSetReference docSetRef,
			IRootNavigationItem<IDocumentationFile, INavigationItem> rootNavItem,
			string repoName) =>
			new()
			{
				Name = repoName,
				Title = rootNavItem.Index.Model.Title ?? repoName,
				Url = $"{codex.Url}/r/{repoName}",
				Group = docSetRef.Group,
				PageCount = CountPages(rootNavItem),
				Description = docSetRef.Description,
				Icon = docSetRef.Icon
			};

		private void AttachToGroup(
			CodexDocumentationSetReference docSetRef,
			IDocumentationSetNavigation docSetNav,
			IRootNavigationItem<IDocumentationFile, INavigationItem> rootNavItem,
			string pathPrefix,
			CodexDocumentationSetInfo docSetInfo)
		{
			var groupId = docSetRef.Group!;
			var groupNav = GetOrCreateGroup(groupId);

			if (docSetNav is INavigationHomeAccessor homeAccessor)
				homeAccessor.HomeProvider = new NavigationHomeProvider(pathPrefix, groupNav);

			rootNavItem.Parent = groupNav;

			var groupChildren = groupNav.NavigationItems.ToList();
			groupChildren.Add(rootNavItem);
			((IAssignableChildrenNavigation)groupNav).SetNavigationItems(groupChildren);

			if (!_groupDocSetInfos.TryGetValue(groupId, out var list))
			{
				list = [];
				_groupDocSetInfos[groupId] = list;
			}
			list.Add(docSetInfo);
		}

		private GroupNavigation GetOrCreateGroup(string groupId)
		{
			if (_groups.TryGetValue(groupId, out var existing))
				return existing;

			var groupUrl = $"{codex.Url}/g/{groupId}";
			var groupDef = configuration.Groups.FirstOrDefault(g => g.Id == groupId);
			var displayTitle = groupDef?.Name ?? FormatGroupTitle(groupId);
			var description = groupDef?.Description;
			var icon = groupDef?.Icon;
			var groupNav = new GroupNavigation(groupId, displayTitle, groupUrl, description, icon);
			_groups[groupId] = groupNav;

			var groupLink = new GroupLinkLeaf(new GroupLinkPage(groupNav.DisplayTitle, groupUrl), codex)
			{
				NavigationIndex = ++_navigationIndex
			};
			_items.Add(groupLink);

			return groupNav;
		}

		private void AttachToCodexRoot(
			IDocumentationSetNavigation docSetNav,
			IRootNavigationItem<IDocumentationFile, INavigationItem> rootNavItem,
			string pathPrefix)
		{
			if (docSetNav is INavigationHomeAccessor homeAccessor)
				homeAccessor.HomeProvider = new NavigationHomeProvider(pathPrefix, rootNavItem);

			_items.Add(rootNavItem);
		}

		private void FinalizeGroupDocumentationSetInfos()
		{
			foreach (var (slug, infos) in _groupDocSetInfos)
			{
				if (_groups.TryGetValue(slug, out var group))
					group.DocumentationSetInfos = infos.ToFrozenSet();
			}
		}

		private static string FormatGroupTitle(string slug) =>
			string.Join(" ", slug
				.Replace('-', ' ')
				.Replace('_', ' ')
				.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
	}

	/// <summary>
	/// Gets the group navigations (one per group id) for rendering group landing pages.
	/// </summary>
	public FrozenSet<GroupNavigation> GroupNavigations { get; }

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

	private static int CountPages(INavigationItem item) =>
		item switch
		{
			INodeNavigationItem<INavigationModel, INavigationItem> node =>
				1 + node.NavigationItems.Sum(CountPages),
			ILeafNavigationItem<IDocumentationFile> => 1,
			_ => 0
		};
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
