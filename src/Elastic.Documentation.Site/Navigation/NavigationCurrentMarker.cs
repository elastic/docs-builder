// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Site.Navigation;

/// <summary>
/// Stamps <c>current</c> onto the cached sidebar HTML for the page being rendered,
/// and opens every ancestor group of that link. The tree itself is shared across
/// pages of the same island; only this changes per page, which is why it is applied
/// after the render cache.
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
		var edits = new List<SpanEdit>();
		CollectCurrentEdits(html, NormalizePath(currentUrl), edits);
		return edits.Count == 0 ? html : ApplyEdits(html, edits);
	}

	// Runs once per rendered page over the full sidebar HTML, so tags are inspected as spans of
	// the original string and only the few edited tags are materialized.
	private static void CollectCurrentEdits(string html, string target, List<SpanEdit> edits)
	{
		var stack = new List<LiFrame>();
		var searchFrom = 0;
		while (true)
		{
			var tagStart = html.IndexOf('<', searchFrom);
			if (tagStart < 0)
				break;

			var tagEnd = html.IndexOf('>', tagStart);
			if (tagEnd < 0)
				break;

			searchFrom = tagEnd + 1;
			var tag = html.AsSpan(tagStart, tagEnd - tagStart);
			if (IsTag(tag, "li", end: true))
			{
				if (stack.Count > 0)
					stack.RemoveAt(stack.Count - 1);
				continue;
			}

			if (IsTag(tag, "li", end: false))
			{
				stack.Add(new LiFrame { IsFolder = ElementHasClass(tag, "nav-folder"), InputStart = -1, ClipInsertAt = -1 });
				continue;
			}

			NoteCheckbox(stack, tag, tagStart, tagEnd);
			NoteClip(stack, tag, tagStart);
			NoteCurrentAnchor(html, target, stack, tag, tagStart, tagEnd, edits);
		}
	}

	private static void NoteCheckbox(List<LiFrame> stack, ReadOnlySpan<char> tag, int tagStart, int tagEnd)
	{
		if (stack.Count == 0 || !IsCheckbox(tag))
			return;

		var top = stack[^1];
		if (!top.IsFolder || top.InputStart >= 0)
			return;

		top.InputStart = tagStart;
		top.InputEnd = tagEnd;
		top.InputChecked = HasCheckedAttribute(tag);
		stack[^1] = top;
	}

	private static void NoteClip(List<LiFrame> stack, ReadOnlySpan<char> tag, int tagStart)
	{
		if (stack.Count == 0 || !IsTag(tag, "div", end: false) || !ElementHasClass(tag, "nav-subtree-clip"))
			return;

		var top = stack[^1];
		if (!top.IsFolder || top.ClipInsertAt >= 0)
			return;

		var classEnd = ClassValueEnd(tag, tagStart);
		if (classEnd < 0)
			return;

		top.ClipInsertAt = classEnd;
		top.ClipOpen = ElementHasClass(tag, "nav-subtree-clip--open");
		stack[^1] = top;
	}

	private static void NoteCurrentAnchor(
		string html,
		string target,
		List<LiFrame> stack,
		ReadOnlySpan<char> tag,
		int tagStart,
		int tagEnd,
		List<SpanEdit> edits
	)
	{
		if (!IsTag(tag, "a", end: false) || !ElementHasClass(tag, "sidebar-link"))
			return;

		if (!TryGetQuotedAttribute(tag, "href", out var href) || !NormalizePath(href).SequenceEqual(target))
			return;

		if (WithCurrentClass(tag) is { } marked)
			edits.Add(new SpanEdit(tagStart, tagEnd, marked));

		for (var i = 0; i < stack.Count; i++)
			OpenFolder(html, stack, i, edits);
	}

	private static void OpenFolder(string html, List<LiFrame> stack, int index, List<SpanEdit> edits)
	{
		var frame = stack[index];
		if (!frame.IsFolder)
			return;

		if (frame.InputStart >= 0 && !frame.InputChecked)
		{
			edits.Add(new SpanEdit(frame.InputStart, frame.InputEnd, html[frame.InputStart..frame.InputEnd] + " checked"));
			frame.InputChecked = true;
		}

		if (frame.ClipInsertAt >= 0 && !frame.ClipOpen)
		{
			edits.Add(new SpanEdit(frame.ClipInsertAt, frame.ClipInsertAt, " nav-subtree-clip--open"));
			frame.ClipOpen = true;
		}

		stack[index] = frame;
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
		var path = NormalizePath(url.AsSpan());
		return path.Length == url.Length ? url : path.ToString();
	}

	private static ReadOnlySpan<char> NormalizePath(ReadOnlySpan<char> url)
	{
		var path = url;
		var cut = path.IndexOfAny('?', '#');
		if (cut >= 0)
			path = path[..cut];

		path = path.TrimEnd('/');
		return path.Length == 0 ? "/".AsSpan() : path;
	}

	private static bool TryGetQuotedAttribute(ReadOnlySpan<char> tag, string name, out ReadOnlySpan<char> value)
	{
		value = default;
		var start = IndexOfAttribute(tag, name);
		if (start < 0)
			return false;

		var end = tag[start..].IndexOf('"');
		if (end < 0)
			return false;

		value = tag.Slice(start, end);
		return true;
	}

	private static int IndexOfAttribute(ReadOnlySpan<char> tag, string name)
	{
		var offset = 0;
		while (true)
		{
			var index = tag[offset..].IndexOf(name, StringComparison.Ordinal);
			if (index < 0)
				return -1;

			var valueStart = offset + index + name.Length;
			if (tag.Length > valueStart + 1 && tag[valueStart] == '=' && tag[valueStart + 1] == '"')
				return valueStart + 2;

			offset = offset + index + 1;
		}
	}

	/// <summary>Returns the tag with <c>current</c> added to its class list, or <see langword="null"/> when nothing changes.</summary>
	private static string? WithCurrentClass(ReadOnlySpan<char> tag)
	{
		const string prefix = " class=\"";
		var classStart = tag.IndexOf(prefix, StringComparison.Ordinal);
		if (classStart < 0)
			return null;

		var valueStart = classStart + prefix.Length;
		var valueLength = tag[valueStart..].IndexOf('"');
		if (valueLength < 0)
			return null;

		var valueEnd = valueStart + valueLength;
		if (HasClass(tag[valueStart..valueEnd], "current"))
			return null;

		return string.Concat(tag[..valueEnd], " current", tag[valueEnd..]);
	}

	private static bool HasClass(ReadOnlySpan<char> classes, string name)
	{
		foreach (var range in classes.Split(' '))
		{
			if (classes[range].Equals(name, StringComparison.Ordinal))
				return true;
		}

		return false;
	}

	private static bool ElementHasClass(ReadOnlySpan<char> tag, string name) =>
		TryGetQuotedAttribute(tag, "class", out var classes) && HasClass(classes, name);

	private static bool IsTag(ReadOnlySpan<char> tag, string name, bool end)
	{
		var isEndTag = tag.StartsWith("</", StringComparison.Ordinal);
		if (isEndTag != end)
			return false;

		var offset = end ? 2 : 1;
		if (tag.Length < offset + name.Length)
			return false;
		if (!tag.Slice(offset, name.Length).Equals(name, StringComparison.OrdinalIgnoreCase))
			return false;

		if (tag.Length == offset + name.Length)
			return true;

		var next = tag[offset + name.Length];
		return char.IsWhiteSpace(next) || next == '/';
	}

	private static bool IsCheckbox(ReadOnlySpan<char> tag) =>
		IsTag(tag, "input", end: false)
			&& TryGetQuotedAttribute(tag, "type", out var type)
			&& type.Equals("checkbox", StringComparison.OrdinalIgnoreCase);

	private static bool HasCheckedAttribute(ReadOnlySpan<char> tag)
	{
		const string attribute = "checked";
		var index = 0;
		while (true)
		{
			var found = tag[index..].IndexOf(attribute, StringComparison.OrdinalIgnoreCase);
			if (found < 0)
				return false;

			index += found;
			var before = index == 0 ? ' ' : tag[index - 1];
			var afterIndex = index + attribute.Length;
			var after = afterIndex < tag.Length ? tag[afterIndex] : ' ';
			if ((char.IsWhiteSpace(before) || before == '<') && (char.IsWhiteSpace(after) || after is '=' or '/'))
				return true;

			index = afterIndex;
		}
	}

	private static int ClassValueEnd(ReadOnlySpan<char> tag, int tagStart)
	{
		const string prefix = " class=\"";
		var classStart = tag.IndexOf(prefix, StringComparison.Ordinal);
		if (classStart < 0)
			return -1;

		var valueStart = classStart + prefix.Length;
		var valueLength = tag[valueStart..].IndexOf('"');
		return valueLength < 0 ? -1 : tagStart + valueStart + valueLength;
	}

	// Edits are applied in one pass; copying the full sidebar once per edit made pages with deep
	// ancestor chains copy the whole HTML several times.
	private static string ApplyEdits(string html, List<SpanEdit> edits)
	{
		edits.Sort(static (left, right) => left.Start.CompareTo(right.Start));
		var builder = new StringBuilder(html.Length + (edits.Count * 32));
		var position = 0;
		var lastStart = -1;
		foreach (var edit in edits)
		{
			if (edit.Start == lastStart || edit.Start < position)
				continue;

			lastStart = edit.Start;
			_ = builder.Append(html, position, edit.Start - position).Append(edit.Text);
			position = edit.End;
		}

		return builder.Append(html, position, html.Length - position).ToString();
	}

	private readonly record struct SpanEdit(int Start, int End, string Text);

	private struct LiFrame
	{
		public bool IsFolder;
		public int InputStart;
		public int InputEnd;
		public bool InputChecked;
		public int ClipInsertAt;
		public bool ClipOpen;
	}
}
