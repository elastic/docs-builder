// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Links;

namespace Elastic.Documentation.Navigation.Floating;

/// <summary>
/// A set of pages that owns its URL root and its sidebar and whose models are not Markdown files,
/// for example the pages of one API. The root of a section exists when the navigation tree is built,
/// and the section fills in its content later.
/// </summary>
public interface IFloatingSection
{
	/// <summary>The kind of section, for example <c>api</c>.</summary>
	string Kind { get; }

	/// <summary>The key of the section within its kind, for example the API key <c>elasticsearch</c>.</summary>
	string Key { get; }

	/// <summary>The URL root this section owns, relative to the site root and without slashes at either end.</summary>
	string UrlRoot { get; }

	/// <summary>
	/// Creates the root of this section under <paramref name="parent"/>. This is synchronous and cheap, with no
	/// network access: the root knows its landing URL and title but has no children yet. The root is an island,
	/// so the parent navigation renders a stub and the pages render in the section's own sidebar.
	/// A <paramref name="hidden"/> root stays out of the sidebar of its parent.
	/// </summary>
	IFloatingSectionRoot CreateRoot(INodeNavigationItem<INavigationModel, INavigationItem>? parent, bool hidden = false);

	/// <summary>
	/// Fetches the content of the section and fills the children of the root. The build calls this once,
	/// before anything walks the children (<c>links.json</c>, the sitemap, <c>llms.txt</c>, page rendering).
	/// </summary>
	Task ResolveAsync(Cancel ctx);

	/// <summary>
	/// The pages of this section for <c>links.json</c>, or <c>null</c> when the section has none, for example when
	/// its content could not be loaded. Call it after <see cref="ResolveAsync"/>.
	/// </summary>
	FloatingLinkSection? ToLinkSection();
}

/// <summary>
/// The root of a floating section. The site assembler keeps these where a docset mounts them.
/// Its subtree is opaque to the numbering of the surrounding navigation:
/// the root takes one place in the global navigation order and its children keep their own numbering.
/// This lets a section fill its children after the surrounding tree is built, without renumbering it.
/// </summary>
public interface IFloatingSectionRoot : IRootNavigationItem<INavigationModel, INavigationItem>;

/// <summary>Resolves the floating sections of one <see cref="IFloatingSection.Kind"/>.</summary>
public interface IFloatingSectionProvider
{
	string Kind { get; }

	/// <summary>
	/// Every section of this kind the build can offer, in a stable order. A site-level mount, such as an
	/// <c>APIs</c> section in <c>navigation.yml</c>, uses this to place all of them without naming each one.
	/// </summary>
	IReadOnlyList<IFloatingSection> GetSections() => [];

	/// <summary>
	/// Whether a docset mounts every section of this kind that it declares, without a toc entry. The sections stay
	/// out of the sidebar of the docset. For example, a docset that declares an API has that API in its tree.
	/// </summary>
	bool MountsImplicitly => false;

	/// <summary>
	/// The page that lists the sections of this kind, relative to the site root and without slashes at either end,
	/// or <c>null</c> when there is none.
	/// </summary>
	string? HubPath => null;
}

/// <summary>The providers available to a build, looked up by kind.</summary>
public sealed class FloatingSectionRegistry(IEnumerable<IFloatingSectionProvider>? providers = null)
{
	public static FloatingSectionRegistry Empty { get; } = new();

	private readonly Dictionary<string, IFloatingSectionProvider> _providers = (providers ?? []).ToDictionary(
		p => p.Kind,
		StringComparer.Ordinal
	);

	/// <summary>The sections that every docset mounts without a toc entry, from the providers that opt in.</summary>
	public IReadOnlyList<IFloatingSection> GetImplicitSections() =>
		[.. _providers.Values.Where(p => p.MountsImplicitly).SelectMany(p => p.GetSections())];

	/// <summary>Every section of <paramref name="kind"/> the build can offer, or none when no provider handles the kind.</summary>
	public IReadOnlyList<IFloatingSection> GetSections(string kind) =>
		_providers.TryGetValue(kind, out var provider) ? provider.GetSections() : [];

	/// <summary>The hub page of <paramref name="kind"/>, relative to the site root, or <c>null</c> when it has none.</summary>
	public string? HubPath(string kind) => _providers.TryGetValue(kind, out var provider) ? provider.HubPath : null;
}
