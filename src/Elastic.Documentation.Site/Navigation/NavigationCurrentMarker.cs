// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Site.Navigation;

/// <summary>
/// Stamps <c>current</c> onto the cached sidebar HTML for the page being rendered.
/// The tree itself is shared across pages of the same island; only
/// this class changes per page, which is why it is applied after the render cache.
/// </summary>
public static class NavigationCurrentMarker
{
	public static NavigationRenderResult Apply(NavigationRenderResult result, INavigationItem current) =>
		Apply(result, ResolveActiveUrl(current));

	public static NavigationRenderResult Apply(NavigationRenderResult result, string? currentUrl)
	{
		if (string.IsNullOrEmpty(result.Html) || string.IsNullOrEmpty(currentUrl))
			return result;

		var html = Apply(result.Html, currentUrl);
		return ReferenceEquals(html, result.Html) ? result : result with { Html = html };
	}

	public static string Apply(string html, string currentUrl)
	{
		var target = NormalizePath(currentUrl);
		var searchFrom = 0;
		string? updated = null;

		while (true)
		{
			var tagStart = html.IndexOf("<a ", searchFrom, StringComparison.Ordinal);
			if (tagStart < 0)
				break;

			var tagEnd = html.IndexOf('>', tagStart);
			if (tagEnd < 0)
				break;

			searchFrom = tagEnd + 1;
			var tag = html[tagStart..tagEnd];
			if (!tag.Contains("sidebar-link", StringComparison.Ordinal))
				continue;

			var href = GetQuotedAttribute(tag, "href");
			if (href is null || NormalizePath(href) != target)
				continue;

			var marked = WithCurrentClass(tag);
			if (marked.Equals(tag, StringComparison.Ordinal))
				continue;

			updated ??= html;
			updated = string.Concat(updated.AsSpan(0, tagStart), marked, updated.AsSpan(tagEnd));
			searchFrom = tagStart + marked.Length + 1;
			html = updated;
		}

		return updated ?? html;
	}

	/// <summary>
	/// Hidden pages have no sidebar row; highlight the nearest visible ancestor, matching
	/// <c>docs:nav-active</c>. Island pages keep their own URL because they have a sidebar.
	/// </summary>
	public static string ResolveActiveUrl(INavigationItem current)
	{
		if (!current.Hidden || current.FindIslandRoot() is not null)
			return current.Url;

		for (var parent = current.Parent; parent is not null; parent = parent.Parent)
		{
			if (!parent.Hidden)
				return parent.Url;
		}

		return current.Url;
	}

	internal static string NormalizePath(string url)
	{
		var path = url;
		var cut = path.IndexOfAny(['?', '#']);
		if (cut >= 0)
			path = path[..cut];

		path = path.TrimEnd('/');
		return path.Length == 0 ? "/" : path;
	}

	private static string? GetQuotedAttribute(string tag, string name)
	{
		var needle = name + "=\"";
		var start = tag.IndexOf(needle, StringComparison.Ordinal);
		if (start < 0)
			return null;

		start += needle.Length;
		var end = tag.IndexOf('"', start);
		return end < 0 ? null : tag[start..end];
	}

	private static string WithCurrentClass(string tag)
	{
		const string prefix = " class=\"";
		var classStart = tag.IndexOf(prefix, StringComparison.Ordinal);
		if (classStart < 0)
			return tag;

		var valueStart = classStart + prefix.Length;
		var valueEnd = tag.IndexOf('"', valueStart);
		if (valueEnd < 0)
			return tag;

		var classes = tag[valueStart..valueEnd];
		if (HasClass(classes, "current"))
			return tag;

		return string.Concat(tag.AsSpan(0, valueEnd), " current", tag.AsSpan(valueEnd));
	}

	private static bool HasClass(string classes, string name)
	{
		var start = 0;
		while (start < classes.Length)
		{
			while (start < classes.Length && classes[start] == ' ')
				start++;

			var end = classes.IndexOf(' ', start);
			if (end < 0)
				end = classes.Length;

			if (end > start && classes.AsSpan(start, end - start).Equals(name, StringComparison.Ordinal))
				return true;

			start = end + 1;
		}

		return false;
	}
}
