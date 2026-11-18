// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Testing.Linked;
using Nullean.Snapshots;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

internal static class BaselineDumps
{
	/// <summary>
	/// The top nav, one tab per line: <c>link Title -> url</c>, flagged <c>[external]</c> when it leaves the site and
	/// <c>[section]</c> when the tab is active for the pages of a section; a dropdown lists its groups and links below it.
	/// </summary>
	public static string TopNavToText(TopNavRenderModel? topNav)
	{
		if (topNav is null)
			return "(no top nav)\n";
		var sb = new StringBuilder();
		foreach (var item in topNav.Items)
		{
			switch (item)
			{
				case TopNavLinkItem link:
					AppendLink(sb, "link", link, 0);
					break;
				case TopNavDropdownItem dropdown:
					_ = sb.Append("dropdown ").Append(dropdown.Title).Append('\n');
					foreach (var group in dropdown.Groups)
					{
						_ = sb.Append("  group ").Append(group.Label ?? "(no label)").Append('\n');
						foreach (var link in group.Links)
							AppendLink(sb, "link", link, 4);
					}
					break;
				default:
					_ = sb.Append(item.GetType().Name).Append(' ').Append(item.Title).Append('\n');
					break;
			}
		}
		return sb.ToString();
	}

	private static void AppendLink(StringBuilder sb, string kind, TopNavLinkItem link, int indent)
	{
		_ = sb.Append(' ', indent).Append(kind).Append(' ').Append(link.Title).Append(" -> ").Append(link.Url);
		if (link.IsExternal)
			_ = sb.Append(" [external]");
		if (link.SectionId is not null)
			_ = sb.Append(" [section]");
		_ = sb.Append('\n');
	}

	/// <summary>
	/// What changed between two texts, line by line: <c>-</c> for a removed line and <c>+</c> for an added or re-indented one, with the
	/// unchanged line before each change as an anchor. Short enough to review, where the whole filtered file is not.
	/// </summary>
	public static string LineDiff(string before, string after)
	{
		var a = before.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
		var b = after.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
		var lcs = new int[a.Length + 1, b.Length + 1];
		for (var i = a.Length - 1; i >= 0; i--)
		{
			for (var j = b.Length - 1; j >= 0; j--)
				lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
		}

		var sb = new StringBuilder();
		string? anchor = null;
		var pendingAnchor = false;
		int x = 0, y = 0;
		while (x < a.Length || y < b.Length)
		{
			if (x < a.Length && y < b.Length && a[x] == b[y])
			{
				anchor = a[x];
				pendingAnchor = true;
				x++;
				y++;
				continue;
			}

			if (pendingAnchor)
			{
				_ = sb.Append("  ").Append(anchor).Append('\n');
				pendingAnchor = false;
			}
			if (y >= b.Length || (x < a.Length && lcs[x + 1, y] >= lcs[x, y + 1]))
				_ = sb.Append("- ").Append(a[x++]).Append('\n');
			else
				_ = sb.Append("+ ").Append(b[y++]).Append('\n');
		}
		return sb.ToString();
	}

	/// <summary>Every repository of the frozen assembler config that is marked private, whatever the build skips.</summary>
	public static IReadOnlySet<string> PrivateRepositories() =>
		AssemblyConfiguration.Deserialize(BaselineSite.ReadBaseline("assembler.yml")).AllPrivateRepositoryNames;

	/// <summary>
	/// Snapshot options that scrub the private repositories of the frozen config and volatile values, and fail a snapshot, or a
	/// verified file, that names a private repository of the shipped config.
	/// </summary>
	public static SnapshotOptions Scrubbed() =>
		PrivateRepositoryScrubbing.Options(PrivateRepositories()).Forbid(
			PrivateRepositoryScrubbing.Forbidden(PrivateRepositoryScrubbingTests.ShippedPrivateRepositories())
		);
}
