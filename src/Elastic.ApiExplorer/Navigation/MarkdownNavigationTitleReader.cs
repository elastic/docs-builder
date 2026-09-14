// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using YamlDotNet.RepresentationModel;

namespace Elastic.ApiExplorer.Navigation;

/// <summary>
/// Reads the navigation title for an API intro/outro markdown page: the frontmatter's
/// <c>navigation_title</c> when present, otherwise a title-cased form of the file name.
/// </summary>
public static class MarkdownNavigationTitleReader
{
	public static ApiMarkdownPageMetadata GetMetadata(IFileSystem readFileSystem, IFileInfo markdownFile)
	{
		var fallback = TitleCaseFromFileName(markdownFile.Name);
		try
		{
			var content = readFileSystem.File.ReadAllText(markdownFile.FullName).ReplaceLineEndings("\n");
			var frontMatter = ReadFrontMatter(content);
			var navigationTitle = ReadScalar(frontMatter, "navigation_title");
			var metaTitle = ReadScalar(frontMatter, "meta_title");
			return new ApiMarkdownPageMetadata(navigationTitle ?? fallback, metaTitle);
		}
		catch
		{
			return new ApiMarkdownPageMetadata(fallback, null);
		}
	}

	public static string GetNavigationTitle(IFileSystem readFileSystem, IFileInfo markdownFile) =>
		GetMetadata(readFileSystem, markdownFile).NavigationTitle;

	private static YamlMappingNode? ReadFrontMatter(string content)
	{
		if (!content.StartsWith("---"))
			return null;

		var end = content.IndexOf("\n---", 3, StringComparison.Ordinal);
		if (end < 0)
			return null;

		// YamlStream is reflection-free and therefore native-AOT safe
		var yaml = new YamlStream();
		yaml.Load(new StringReader(content[3..end]));
		return yaml.Documents.Count == 0 ? null : yaml.Documents[0].RootNode as YamlMappingNode;
	}

	private static string? ReadScalar(YamlMappingNode? mapping, string keyName)
	{
		if (mapping is null)
			return null;
		foreach (var (key, value) in mapping.Children)
		{
			if (key is YamlScalarNode { Value: var yamlKey } && yamlKey == keyName && value is YamlScalarNode scalar)
				return string.IsNullOrWhiteSpace(scalar.Value) ? null : scalar.Value;
		}

		return null;
	}

	/// <summary>Converts kebab-case/snake_case file names to a title-cased navigation title.</summary>
	internal static string TitleCaseFromFileName(string fileName) =>
		Path
			.GetFileNameWithoutExtension(fileName)
			.Replace('-', ' ')
			.Replace('_', ' ')
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(word => char.ToUpper(word[0]) + word[1..].ToLower())
			.Aggregate((current, next) => $"{current} {next}");
}

public sealed record ApiMarkdownPageMetadata(string NavigationTitle, string? MetaTitle);
