// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// The <c>top_nav:</c> entries of navigation.yml, as written. One shape is used at every depth:
/// a top level entry with children renders as a dropdown, a child with children as a group label
/// inside that dropdown, and anything carrying <c>url</c> or <c>page</c> as a link.
/// </summary>
public class TopNavItemCollection : List<TopNavItemConfig>;

public record TopNavItemConfig
{
	public string? Title { get; init; }

	/// <summary>A site relative path (<c>/reference/</c>) or an absolute <c>http(s)</c> URL.</summary>
	public string? Url { get; init; }

	/// <summary>A cross link URI (<c>docs-content://products/elasticsearch/v9.md</c>) resolved at assemble time.</summary>
	public Uri? Page { get; init; }

	public IReadOnlyList<TopNavItemConfig> Children { get; init; } = [];
}

/// <summary>
/// The resolved top navigation handed to the layout. Every URL here is final: cross links are
/// resolved and the environment path prefix is already applied, so templates render hrefs as is.
/// </summary>
public record TopNavRenderModel(IReadOnlyList<TopNavRenderItem> Items)
{
	/// <summary>
	/// The href of the entry that best covers <paramref name="currentUrl"/>, or null when none does.
	/// Matching is on whole path segments, so <c>/reference/</c> does not claim <c>/references/x</c>,
	/// and the longest match wins so a nested entry beats its parent.
	/// </summary>
	public string? ActiveUrl(string? currentUrl)
	{
		if (string.IsNullOrEmpty(currentUrl))
			return null;

		var current = WithTrailingSlash(currentUrl);
		string? best = null;

		foreach (var link in Items.SelectMany(EnumerateLinks))
		{
			if (link.IsExternal)
				continue;
			var candidate = WithTrailingSlash(link.Url);
			if (!current.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
				continue;
			if (best is null || candidate.Length > best.Length)
				best = candidate;
		}

		return best;
	}

	private static IEnumerable<TopNavLinkItem> EnumerateLinks(TopNavRenderItem item) => item switch
	{
		TopNavLinkItem link => [link],
		TopNavDropdownItem dropdown => dropdown.Groups.SelectMany(g => g.Links),
		_ => []
	};

	internal static string WithTrailingSlash(string url)
	{
		var path = url.Split('#')[0];
		return path.EndsWith('/') ? path : path + '/';
	}
}

public abstract record TopNavRenderItem(string Title)
{
	/// <summary>Whether this entry owns <paramref name="activeUrl"/>, as returned by <see cref="TopNavRenderModel.ActiveUrl"/>.</summary>
	public abstract bool IsActive(string? activeUrl);
}

public record TopNavLinkItem(string Title, string Url, bool IsExternal) : TopNavRenderItem(Title)
{
	public override bool IsActive(string? activeUrl) =>
		!IsExternal && activeUrl is not null && TopNavRenderModel.WithTrailingSlash(Url) == activeUrl;
}

public record TopNavDropdownItem(string Title, IReadOnlyList<TopNavGroup> Groups) : TopNavRenderItem(Title)
{
	public override bool IsActive(string? activeUrl) =>
		Groups.SelectMany(g => g.Links).Any(l => l.IsActive(activeUrl));
}

/// <summary>A run of links inside a dropdown. A null <paramref name="Label"/> means the links are ungrouped.</summary>
public record TopNavGroup(string? Label, IReadOnlyList<TopNavLinkItem> Links);

public class TopNavItemCollectionYamlConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(TopNavItemCollection);

	public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var collection = new TopNavItemCollection();

		if (!parser.TryConsume<SequenceStart>(out _))
			return collection;

		while (!parser.TryConsume<SequenceEnd>(out _))
		{
			if (rootDeserializer(typeof(TopNavItemConfig)) is TopNavItemConfig item)
				collection.Add(item);
		}

		return collection;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}

public class TopNavItemConfigYamlConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(TopNavItemConfig);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (!parser.TryConsume<MappingStart>(out _))
			return null;

		string? title = null;
		string? url = null;
		string? page = null;
		IReadOnlyList<TopNavItemConfig> children = [];

		while (!parser.TryConsume<MappingEnd>(out _))
		{
			var key = parser.Consume<Scalar>();

			if (parser.Accept<Scalar>(out var scalar))
			{
				switch (key.Value)
				{
					case "title":
						title = scalar.Value;
						break;
					case "url":
						url = scalar.Value;
						break;
					case "page":
						page = scalar.Value;
						break;
				}
				_ = parser.MoveNext();
			}
			else if (parser.Accept<SequenceStart>(out _))
			{
				if (key.Value == "children")
				{
					var list = new List<TopNavItemConfig>();
					_ = parser.Consume<SequenceStart>();
					while (!parser.TryConsume<SequenceEnd>(out _))
					{
						if (rootDeserializer(typeof(TopNavItemConfig)) is TopNavItemConfig child)
							list.Add(child);
					}
					children = list;
				}
				else
					parser.SkipThisAndNestedEvents();
			}
			else if (parser.Accept<MappingStart>(out _))
				parser.SkipThisAndNestedEvents();
		}

		Uri? pageUri = null;
		if (!string.IsNullOrEmpty(page) && !Uri.TryCreate(page, UriKind.Absolute, out pageUri))
			throw new InvalidOperationException($"Invalid top_nav page reference: '{page}' could not be parsed as a URI");

		return new TopNavItemConfig
		{
			Title = title,
			Url = url,
			Page = pageUri,
			Children = children
		};
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}
