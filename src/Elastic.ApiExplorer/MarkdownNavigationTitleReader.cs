// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using YamlDotNet.RepresentationModel;

namespace Elastic.ApiExplorer;

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

	private static string? ReadFrontMatterNavigationTitle(string content)
	{
		if (!content.StartsWith("---"))
			return null;

		var end = content.IndexOf("\n---", 3, StringComparison.Ordinal);
		if (end < 0)
			return null;

		// YamlStream is reflection-free and therefore native-AOT safe
		var yaml = new YamlStream();
		yaml.Load(new StringReader(content[3..end]));
		if (yaml.Documents.Count == 0 || yaml.Documents[0].RootNode is not YamlMappingNode mapping)
			return null;

		foreach (var (key, value) in mapping.Children)
		{
			if (key is YamlScalarNode { Value: "navigation_title" } && value is YamlScalarNode scalar)
				return scalar.Value;
		}

		return null;
	}

	/// <summary>Converts kebab-case/snake_case file names to a title-cased navigation title.</summary>
	internal static string TitleCaseFromFileName(string fileName) =>
		Path.GetFileNameWithoutExtension(fileName)
			.Replace('-', ' ')
			.Replace('_', ' ')
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(word => char.ToUpper(word[0]) + word[1..].ToLower())
			.Aggregate((current, next) => $"{current} {next}");
}
