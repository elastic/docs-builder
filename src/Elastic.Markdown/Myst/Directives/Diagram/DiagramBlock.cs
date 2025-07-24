// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using Elastic.Documentation.Configuration.Diagram;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Myst.Directives.Diagram;

public class DiagramBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "diagram";

	/// <summary>
	/// The diagram type (e.g., "mermaid", "d2", "graphviz", "plantuml")
	/// </summary>
	public string? DiagramType { get; private set; }

	/// <summary>
	/// The raw diagram content
	/// </summary>
	public string? Content { get; private set; }

	/// <summary>
	/// The encoded diagram URL for Kroki service
	/// </summary>
	public string? EncodedUrl { get; private set; }

	/// The local SVG Url
	public string? LocalSvgUrl { get; private set; }

	/// <summary>
	/// Content hash for unique identification and caching
	/// </summary>
	public string? ContentHash { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		// Extract diagram type from arguments or default to "mermaid"
		DiagramType = !string.IsNullOrWhiteSpace(Arguments) ? Arguments.ToLowerInvariant() : "mermaid";

		// Extract content from the directive body
		Content = ExtractContent();

		if (string.IsNullOrWhiteSpace(Content))
		{
			this.EmitError("Diagram directive requires content.");
			return;
		}

		// Generate content hash for caching
		ContentHash = GenerateContentHash(DiagramType, Content);

		// Generate the local path and url for cached SVG
		var localPath = GenerateLocalPath(context);
		LocalSvgUrl = localPath.Replace(Path.DirectorySeparatorChar, '/');

		// Generate the encoded URL for Kroki
		try
		{
			EncodedUrl = DiagramEncoder.GenerateKrokiUrl(DiagramType, Content);
		}
		catch (Exception ex)
		{
			this.EmitError($"Failed to encode diagram: {ex.Message}", ex);
			return;
		}

		// only register SVG if we can look up the Markdown
		if (context.DocumentationFileLookup(context.MarkdownSourcePath) is MarkdownFile currentMarkdown)
		{
			var path = context.Build.ReadFileSystem.FileInfo.New(Path.Combine(currentMarkdown.ScopeDirectory.FullName, localPath));
			context.DiagramRegistry.RegisterDiagramForCaching(path, EncodedUrl);
		}
		else
			this.EmitError($"Can not locate markdown source for {context.MarkdownSourcePath} to register diagram for caching.");
	}

	private string? ExtractContent()
	{
		if (!this.Any())
			return null;

		var lines = new List<string>();
		foreach (var block in this)
		{
			if (block is Markdig.Syntax.LeafBlock leafBlock)
			{
				var content = leafBlock.Lines.ToString();
				if (!string.IsNullOrWhiteSpace(content))
					lines.Add(content);
			}
		}

		return lines.Count > 0 ? string.Join("\n", lines) : null;
	}

	private string GenerateContentHash(string diagramType, string content)
	{
		var input = $"{diagramType}:{content}";
		var bytes = Encoding.UTF8.GetBytes(input);
		var hash = SHA256.HashData(bytes);
		return Convert.ToHexString(hash)[..12].ToLowerInvariant();
	}

	private string GenerateLocalPath(ParserContext context)
	{
		var markdownFileName = Path.GetFileNameWithoutExtension(context.MarkdownSourcePath.Name);

		var filename = $"{markdownFileName}-diagram-{DiagramType}-{ContentHash}.svg";
		var localPath = Path.Combine("images", "generated-graphs", filename);

		// Normalize path separators to forward slashes for web compatibility
		return localPath;
	}


}
