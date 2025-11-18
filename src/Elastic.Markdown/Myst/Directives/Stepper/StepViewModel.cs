// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Navigation;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Myst.Directives.Stepper;

public class StepViewModel : DirectiveViewModel
{
	public required string Title { get; init; }
	public required string Anchor { get; init; }
	public required int HeadingLevel { get; init; }

	public class StepCrossNavigationLookupProvider : INavigationTraversable
	{
		public static StepCrossNavigationLookupProvider Instance { get; } = new();

		/// <inheritdoc />
		public FrozenDictionary<int, INavigationItem> NavigationIndexedByOrder { get; } = new Dictionary<int, INavigationItem>().ToFrozenDictionary();

		/// <inheritdoc />
		public ConditionalWeakTable<IDocumentationFile, INavigationItem> NavigationDocumentationFileLookup { get; } = [];
	}

	public class StepCrossLinkResolver : ICrossLinkResolver
	{
		public static StepCrossLinkResolver Instance { get; } = new();
		/// <inheritdoc />
		public bool TryResolve(Action<string> errorEmitter, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri)
		{
			resolvedUri = null;
			return false;
		}

		/// <inheritdoc />
		public IUriEnvironmentResolver UriResolver { get; } = new IsolatedBuildEnvironmentUriResolver();
	}

	/// <summary>
	/// Renders the title with full markdown processing (substitutions, links, emphasis, etc.)
	/// </summary>
	public HtmlString RenderTitle()
	{
		// Parse the title as markdown with full pipeline (including substitutions)
		var directiveBlock = (DirectiveBlock)DirectiveBlock;

		// Get the YamlFrontMatter from the original document to support local substitutions
		var originalContext = directiveBlock.GetData("context") as ParserContext;
		var yamlFrontMatter = originalContext?.YamlFrontMatter;

		var context = new ParserContext(new ParserState(directiveBlock.Build)
		{
			MarkdownSourcePath = directiveBlock.CurrentFile,
			YamlFrontMatter = yamlFrontMatter,
			TryFindDocument = _ => null!,
			TryFindDocumentByRelativePath = _ => null!,
			CrossLinkResolver = StepCrossLinkResolver.Instance,
			NavigationTraversable = StepCrossNavigationLookupProvider.Instance
		});

		var document = Markdig.Markdown.Parse(Title, MarkdownParser.Pipeline, context);

		if (document.FirstOrDefault() is not Markdig.Syntax.ParagraphBlock firstBlock)
			return new(Title);

		// Use the HTML renderer to render the inline content with full processing
		var subscription = DocumentationObjectPoolProvider.HtmlRendererPool.Get();
		_ = subscription.HtmlRenderer.WriteLeafInline(firstBlock);

		var result = subscription.RentedStringBuilder?.ToString() ?? Title;
		DocumentationObjectPoolProvider.HtmlRendererPool.Return(subscription);
		return new HtmlString(result.EnsureTrimmed());
	}
}
