// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
			var tag = html[tagStart..tagEnd];
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

	private static void NoteCheckbox(List<LiFrame> stack, string tag, int tagStart, int tagEnd)
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

	private static void NoteClip(List<LiFrame> stack, string tag, int tagStart)
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
		string tag,
		int tagStart,
		int tagEnd,
		List<SpanEdit> edits
	)
	{
		if (!IsTag(tag, "a", end: false) || !ElementHasClass(tag, "sidebar-link"))
			return;

		var href = GetQuotedAttribute(tag, "href");
		if (href is null || NormalizePath(href) != target)
			return;

		var marked = WithCurrentClass(tag);
		if (!marked.Equals(tag, StringComparison.Ordinal))
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

	private static bool ElementHasClass(string tag, string name)
	{
		var classes = GetQuotedAttribute(tag, "class");
		return classes is not null && HasClass(classes, name);
	}

	private static bool IsTag(string tag, string name, bool end)
	{
		if (end)
		{
			if (!tag.StartsWith("</", StringComparison.Ordinal))
				return false;
		}
		else if (tag.StartsWith("</", StringComparison.Ordinal))
			return false;

		var offset = end ? 2 : 1;
		if (tag.Length < offset + name.Length)
			return false;
		if (!tag.AsSpan(offset, name.Length).Equals(name, StringComparison.OrdinalIgnoreCase))
			return false;

		if (tag.Length == offset + name.Length)
			return true;

		var next = tag[offset + name.Length];
		return char.IsWhiteSpace(next) || next == '/';
	}

	private static bool IsCheckbox(string tag)
	{
		if (!IsTag(tag, "input", end: false))
			return false;

		var type = GetQuotedAttribute(tag, "type");
		return type is not null && type.Equals("checkbox", StringComparison.OrdinalIgnoreCase);
	}

	private static bool HasCheckedAttribute(string tag)
	{
		var index = 0;
		while ((index = tag.IndexOf("checked", index, StringComparison.OrdinalIgnoreCase)) >= 0)
		{
			var before = index == 0 ? ' ' : tag[index - 1];
			var afterIndex = index + "checked".Length;
			var after = afterIndex < tag.Length ? tag[afterIndex] : ' ';
			if ((char.IsWhiteSpace(before) || before == '<') && (char.IsWhiteSpace(after) || after is '=' or '/'))
				return true;

			index = afterIndex;
		}

		return false;
	}

	private static int ClassValueEnd(string tag, int tagStart)
	{
		const string prefix = " class=\"";
		var classStart = tag.IndexOf(prefix, StringComparison.Ordinal);
		if (classStart < 0)
			return -1;

		var valueEnd = tag.IndexOf('"', classStart + prefix.Length);
		return valueEnd < 0 ? -1 : tagStart + valueEnd;
	}

	private static string ApplyEdits(string html, List<SpanEdit> edits)
	{
		edits.Sort(static (left, right) => right.Start.CompareTo(left.Start));
		var seen = new HashSet<int>();
		foreach (var edit in edits)
		{
			if (!seen.Add(edit.Start))
				continue;

			html = string.Concat(html.AsSpan(0, edit.Start), edit.Text, html.AsSpan(edit.End));
		}

		return html;
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
