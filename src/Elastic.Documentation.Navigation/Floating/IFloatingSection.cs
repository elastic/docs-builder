// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

namespace Elastic.Documentation.Navigation.Floating;

/// <summary>
/// A set of pages that owns its URL root and its sidebar and whose models are not Markdown files,
/// for example the pages of one API. The section resolves its content before the navigation tree is built.
/// </summary>
public interface IFloatingSection
{
	/// <summary>The toc key that mounts this section, for example <c>api</c>.</summary>
	string Kind { get; }

	/// <summary>The value of the toc key, for example the API key <c>elasticsearch</c>.</summary>
	string Key { get; }

	/// <summary>The URL root this section owns, relative to the site root and without slashes at either end.</summary>
	string UrlRoot { get; }

	/// <summary>
	/// Creates the root of this section under <paramref name="parent"/>. The root is an island: the parent
	/// navigation renders a stub and the pages render in the section's own sidebar.
	/// </summary>
	IRootNavigationItem<INavigationModel, INavigationItem> CreateRoot(INodeNavigationItem<INavigationModel, INavigationItem>? parent);
}

/// <summary>Resolves the floating sections of one <see cref="IFloatingSection.Kind"/>.</summary>
public interface IFloatingSectionProvider
{
	string Kind { get; }

	bool TryGetSection(string key, [NotNullWhen(true)] out IFloatingSection? section);
}

/// <summary>The providers available to a build, looked up by toc key.</summary>
public sealed class FloatingSectionRegistry(IEnumerable<IFloatingSectionProvider>? providers = null)
{
	public static FloatingSectionRegistry Empty { get; } = new();

	private readonly Dictionary<string, IFloatingSectionProvider> _providers = (providers ?? []).ToDictionary(
		p => p.Kind,
		StringComparer.Ordinal
	);

	public bool TryGetSection(string kind, string key, [NotNullWhen(true)] out IFloatingSection? section)
	{
		section = null;
		return _providers.TryGetValue(kind, out var provider) && provider.TryGetSection(key, out section);
	}
}
