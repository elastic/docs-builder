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

public record ParserState(BuildContext Build)
{
	public ConfigurationFile Configuration { get; } = Build.Configuration;

	public required IFileInfo SourcePath { get; init; }
	public required YamlFrontMatter? YamlFrontMatter { get; init; }
	public required ICrossLinkResolver CrossLinkResolver { get; init; }
	public required Func<IFileInfo, DocumentationFile?> DocumentationFileLookup { get; init; }

	public IFileInfo? ParentMarkdownPath { get; set; }
	public bool SkipValidation { get; set; }
}

public class ParserContext : MarkdownParserContext
{
	public ConfigurationFile Configuration { get; }
	public ICrossLinkResolver LinksResolver { get; }
	public IFileInfo CurrentPath { get; }
	public string CurrentUrlPath { get; }
	public YamlFrontMatter? FrontMatter { get; }
	public BuildContext Build { get; }
	public bool SkipValidation { get; }
	public Func<IFileInfo, DocumentationFile?> GetDocumentationFile { get; }
	public IReadOnlyDictionary<string, string> Substitutions { get; }
	public IReadOnlyDictionary<string, string> ContextSubstitutions { get; }

	public ParserContext(ParserState state)
	{
		Build = state.Build;
		Configuration = state.Configuration;
		FrontMatter = state.YamlFrontMatter;
		SkipValidation = state.SkipValidation;

		LinksResolver = state.CrossLinkResolver;
		CurrentPath = state.SourcePath;
		GetDocumentationFile = state.DocumentationFileLookup;
		var parentPath = state.ParentMarkdownPath;

		CurrentUrlPath = GetDocumentationFile(parentPath ?? CurrentPath) is MarkdownFile md
			? md.Url
			: SkipValidation
				? string.Empty
				: throw new Exception($"Unable to find documentation file for {(parentPath ?? CurrentPath).FullName}");

		if (FrontMatter?.Properties is not { Count: > 0 })
			Substitutions = Configuration.Substitutions;
		else
		{
			var subs = new Dictionary<string, string>(Configuration.Substitutions);
			foreach (var (k, value) in FrontMatter.Properties)
			{
				var key = k.ToLowerInvariant();
				if (Configuration.Substitutions.TryGetValue(key, out _))
					this.EmitError($"{{{key}}} can not be redeclared in front matter as its a global substitution");
				else
					subs[key] = value;
			}

			Substitutions = subs;
		}

		var contextSubs = new Dictionary<string, string>();

		if (FrontMatter?.Title is { } title)
			contextSubs["context.page_title"] = title;

		ContextSubstitutions = contextSubs;
	}
}
