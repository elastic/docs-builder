// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.Myst.FrontMatter;
using Markdig;
using Markdig.Parsers;

namespace Elastic.Markdown.Myst;

public static class ParserContextExtensions
{
	public static BuildContext GetBuildContext(this InlineProcessor processor) =>
		processor.GetContext().Build;

	public static BuildContext GetBuildContext(this BlockProcessor processor) =>
		processor.GetContext().Build;

	public static ParserContext GetContext(this InlineProcessor processor) =>
		processor.Context as ParserContext
		?? throw new InvalidOperationException($"Provided context is not a {nameof(ParserContext)}");

	public static ParserContext GetContext(this BlockProcessor processor) =>
		processor.Context as ParserContext
		?? throw new InvalidOperationException($"Provided context is not a {nameof(ParserContext)}");
}

public class ParserContext : MarkdownParserContext
{
	public ParserContext(
		MarkdownParser markdownParser,
		IFileInfo path,
		YamlFrontMatter? frontMatter,
		BuildContext context,
		ConfigurationFile configuration)
	{
		Parser = markdownParser;
		Path = path;
		FrontMatter = frontMatter;
		Build = context;
		Configuration = configuration;

		foreach (var (key, value) in configuration.Substitutions)
			Properties[key] = value;

		if (frontMatter?.Properties is { } props)
		{
			foreach (var (key, value) in props)
			{
				if (configuration.Substitutions.TryGetValue(key, out _))
					this.EmitError($"{{{key}}} can not be redeclared in front matter as its a global substitution");
				else
					Properties[key] = value;
			}

		}

		if (frontMatter?.Title is { } title)
			Properties["page_title"] = title;
	}

	public ConfigurationFile Configuration { get; }
	public MarkdownParser Parser { get; }
	public IFileInfo Path { get; }
	public YamlFrontMatter? FrontMatter { get; }
	public BuildContext Build { get; }
	public bool SkipValidation { get; init; }
	public Func<IFileInfo, DocumentationFile?>? GetDocumentationFile { get; init; }
}
