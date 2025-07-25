// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Diagram;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Links.CrossLinks;
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

public interface IParserResolvers
{
	ICrossLinkResolver CrossLinkResolver { get; }
	Func<IFileInfo, DocumentationFile?> DocumentationFileLookup { get; }
}

public record ParserResolvers : IParserResolvers
{
	public required ICrossLinkResolver CrossLinkResolver { get; init; }

	public required Func<IFileInfo, DocumentationFile?> DocumentationFileLookup { get; init; }
}

public record ParserState(BuildContext Build) : ParserResolvers
{
	public ConfigurationFile Configuration { get; } = Build.Configuration;

	public required IFileInfo MarkdownSourcePath { get; init; }
	public required YamlFrontMatter? YamlFrontMatter { get; init; }
	public required DiagramRegistry DiagramRegistry { get; init; }

	public IFileInfo? ParentMarkdownPath { get; init; }
	public bool SkipValidation { get; init; }
}

public class ParserContext : MarkdownParserContext, IParserResolvers
{
	public ConfigurationFile Configuration { get; }
	public ICrossLinkResolver CrossLinkResolver { get; }
	public IFileInfo MarkdownSourcePath { get; }
	public IFileInfo? MarkdownParentPath { get; }
	public string CurrentUrlPath { get; }
	public YamlFrontMatter? YamlFrontMatter { get; }
	public BuildContext Build { get; }
	public DiagramRegistry DiagramRegistry { get; }
	public bool SkipValidation { get; }
	public Func<IFileInfo, DocumentationFile?> DocumentationFileLookup { get; }
	public IReadOnlyDictionary<string, string> Substitutions { get; }
	public IReadOnlyDictionary<string, string> ContextSubstitutions { get; }

	public ParserContext(ParserState state)
	{
		Build = state.Build;
		Configuration = state.Configuration;
		YamlFrontMatter = state.YamlFrontMatter;
		SkipValidation = state.SkipValidation;
		MarkdownParentPath = state.ParentMarkdownPath;

		CrossLinkResolver = state.CrossLinkResolver;
		MarkdownSourcePath = state.MarkdownSourcePath;
		DocumentationFileLookup = state.DocumentationFileLookup;
		DiagramRegistry = state.DiagramRegistry;

		CurrentUrlPath = DocumentationFileLookup(state.ParentMarkdownPath ?? MarkdownSourcePath) is MarkdownFile md
			? md.Url
			: string.Empty;

		if (SkipValidation && string.IsNullOrEmpty(CurrentUrlPath))
		{
			//TODO investigate this deeper.
		}

		if (YamlFrontMatter?.Properties is not { Count: > 0 })
			Substitutions = Configuration.Substitutions;
		else
		{
			var subs = new Dictionary<string, string>(Configuration.Substitutions);
			foreach (var (k, value) in YamlFrontMatter.Properties)
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

		if (YamlFrontMatter?.Title is { } title)
			contextSubs["context.page_title"] = title;

		ContextSubstitutions = contextSubs;
	}
}
