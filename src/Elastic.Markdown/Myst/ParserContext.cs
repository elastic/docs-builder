// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
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
	public ParserContext(MarkdownParser markdownParser,
		IFileInfo path,
		YamlFrontMatter? frontMatter,
		BuildContext context)
	{
		Parser = markdownParser;
		Path = path;
		FrontMatter = frontMatter;
		Build = context;

		if (frontMatter?.Properties is { } props)
		{
			foreach (var (key, value) in props)
				Properties[key] = value;
		}
	}

	public MarkdownParser Parser { get; }
	public IFileInfo Path { get; }
	public YamlFrontMatter? FrontMatter { get; }
	public BuildContext Build { get; }
}
