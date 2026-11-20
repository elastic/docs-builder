// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// A deserialized <c>navigation.yml</c> as indented text: the phantoms first, then one line per entry with its
/// source, path prefix and flags, and the children below it.
/// </summary>
public static class SiteNavDump
{
	public static string ToText(SiteNavigationFile file)
	{
		var sb = new StringBuilder();
		foreach (var phantom in file.Phantoms)
			sb.Append("phantom  ").Append(phantom.Source).Append('\n');
		foreach (var entry in file.TableOfContents)
			Write(sb, entry, 0);
		return sb.ToString();
	}

	private static void Write(StringBuilder sb, ISiteNavigationEntry entry, int depth)
	{
		sb.Append(' ', depth * 2);
		switch (entry)
		{
			case SiteTableOfContentsRef toc:
				sb.Append("toc  ").Append(toc.Source).Append("  path_prefix=\"").Append(toc.PathPrefix).Append('"');
				if (toc.Island)
					sb.Append(" [island]");
				if (toc.NavigationTitle is { } title)
					sb.Append(" navigation_title=\"").Append(title).Append('"');
				break;
			case SiteSectionRef section:
				sb.Append("section  \"").Append(section.Title).Append('"');
				if (section.ExternalUrl is { } url)
					sb.Append(" external=").Append(url);
				foreach (var link in section.DropdownLinks)
					sb.Append(" dropdown=\"").Append(link.Title).Append("\" ").Append(link.Url);
				break;
		}

		sb.Append('\n');
		foreach (var child in entry.Children)
			Write(sb, child, depth + 1);
	}
}
