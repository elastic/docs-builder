// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>Renders a site navigation as indented lines, so tests can assert on its shape in text.</summary>
internal static class SiteNavigationText
{
	public static string Describe(SiteNavigationDefinition navigation)
	{
		var sb = new StringBuilder();
		foreach (var phantom in navigation.Phantoms)
			_ = sb.AppendLine($"phantom: {phantom.Source}");
		foreach (var entry in navigation.TableOfContents)
		{
			if (entry.Value is SiteSectionRef section)
			{
				_ = sb.AppendLine($"section: {section.Title}");
				foreach (var child in section.Children)
					Describe(child, 1, sb);
			}
			else if (entry.Value is SiteTableOfContentsRef toc)
				Describe(toc, 0, sb);
		}
		return sb.ToString();
	}

	private static void Describe(SiteTableOfContentsRef toc, int depth, StringBuilder sb)
	{
		var indent = new string(' ', depth * 2);
		_ = sb.AppendLine($"{indent}toc: {toc.Source.OriginalString} path_prefix: {toc.PathPrefix}{(toc.Island ? " island: true" : "")}");
		foreach (var child in toc.Children)
			Describe(child, depth + 1, sb);
	}
}
