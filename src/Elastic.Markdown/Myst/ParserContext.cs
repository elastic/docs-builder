// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.Myst.FrontMatter;
using Markdig;
using Markdig.Parsers;

namespace Elastic.Markdown.Myst;

public static class ParserContextExtensions
{
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
		ConfigurationFile configuration,
		ICrossLinkResolver linksResolver,
		Func<IFileInfo, DocumentationFile?> getDocumentationFile,
		IFileInfo? parentPath = null
	)
	{
		Parser = markdownParser;
		FrontMatter = frontMatter;
		Build = context;
		Configuration = configuration;
		LinksResolver = linksResolver;
		CurrentPath = path;
		CurrentPathRelative = Path.GetRelativePath(markdownParser.SourcePath.FullName, CurrentPath.FullName);
		GetDocumentationFile = getDocumentationFile;

		CurrentUrlPath = getDocumentationFile(parentPath ?? CurrentPath) is MarkdownFile md
			? md.Url
			: throw new Exception($"Unable to find documentation file for {(parentPath ?? CurrentPath).FullName}");

		if (frontMatter?.Properties is not { Count: > 0 })
			Substitutions = configuration.Substitutions;
		else
		{
			var subs = new Dictionary<string, string>(configuration.Substitutions);
			foreach (var (k, value) in frontMatter.Properties)
			{
				var key = k.ToLowerInvariant();
				if (configuration.Substitutions.TryGetValue(key, out _))
					this.EmitError($"{{{key}}} can not be redeclared in front matter as its a global substitution");
				else
					subs[key] = value;
			}

			Substitutions = subs;
		}

		var contextSubs = new Dictionary<string, string>();

		if (frontMatter?.Title is { } title)
			contextSubs["context.page_title"] = title;

		ContextSubstitutions = contextSubs;
	}

	public ConfigurationFile Configuration { get; }
	public ICrossLinkResolver LinksResolver { get; }
	public MarkdownParser Parser { get; }
	public IFileInfo CurrentPath { get; }
	public string CurrentPathRelative { get; }
	public string CurrentUrlPath { get; }
	public YamlFrontMatter? FrontMatter { get; }
	public BuildContext Build { get; }
	public bool SkipValidation { get; init; }
	public Func<IFileInfo, DocumentationFile?> GetDocumentationFile { get; init; }
	public IReadOnlyDictionary<string, string> Substitutions { get; }
	public IReadOnlyDictionary<string, string> ContextSubstitutions { get; }

}
