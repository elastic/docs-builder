// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Extensions;
using SharpYaml.Model;

namespace Elastic.ApiExplorer.Navigation;

/// <summary>
/// Reads the navigation title for an API intro/outro markdown page: the frontmatter's
/// <c>navigation_title</c> when present, otherwise a title-cased form of the file name.
/// </summary>
public static class MarkdownNavigationTitleReader
{
	public static string GetNavigationTitle(IFileSystem readFileSystem, IFileInfo markdownFile)
	{
		try
		{
			var content = readFileSystem.File.ReadAllText(markdownFile.FullName);
			var title = ReadFrontMatterNavigationTitle(content);
			if (!string.IsNullOrEmpty(title))
				return title;
		}
		catch
		{
			// Fall back to filename-based title if reading or parsing fails
		}

		return TitleCaseFromFileName(markdownFile.Name);
	}

	/// <summary>The text of the first level-one heading after the front matter, or null when the file has none or cannot be read.</summary>
	public static string? GetFirstHeading(IFileSystem readFileSystem, IFileInfo markdownFile)
	{
		try
		{
			var inFrontMatter = false;
			var lineNumber = 0;
			foreach (var line in readFileSystem.File.ReadLines(markdownFile.FullName))
			{
				lineNumber++;
				if (line.Trim() == "---" && (lineNumber == 1 || inFrontMatter))
				{
					inFrontMatter = !inFrontMatter;
					continue;
				}

				if (!inFrontMatter && line.StartsWith("# ", StringComparison.Ordinal))
					return line[2..].Trim();
			}
		}
		catch (IOException)
		{
			// A missing child file is reported where the page is built.
		}

		return null;
	}

	private static string? ReadFrontMatterNavigationTitle(string content)
	{
		if (!content.StartsWith("---"))
			return null;

		var end = content.IndexOf("\n---", 3, StringComparison.Ordinal);
		if (end < 0)
			return null;

		// YamlStream is reflection-free and therefore native-AOT safe
		var yaml = YamlStream.Load(new StringReader(content[3..end]));
		if (yaml.Count == 0 || yaml[0].Contents is not YamlMapping mapping)
			return null;

		foreach (var (key, value) in mapping)
		{
			if (key is YamlValue { Value: "navigation_title" } && value is YamlValue scalar)
				return scalar.Value;
		}

		return null;
	}

	/// <summary>Converts kebab-case/snake_case file names to a title-cased navigation title.</summary>
	internal static string TitleCaseFromFileName(string fileName) =>
		System.IO.Path.GetFileNameWithoutExtension(fileName).TitleCaseFromSlug();
}
