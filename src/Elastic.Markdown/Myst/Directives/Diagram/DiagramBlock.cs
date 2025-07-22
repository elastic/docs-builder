// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using Elastic.Documentation.Configuration.Diagram;
using Elastic.Markdown.Diagnostics;

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

	/// <summary>
	/// The local SVG path relative to the output directory
	/// </summary>
	public string? LocalSvgPath { get; private set; }

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

		// Generate local path for cached SVG
		LocalSvgPath = GenerateLocalPath(context);

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

		// Register diagram for tracking, cleanup, and batch caching
		var outputDirectory = context.Build.DocumentationOutputDirectory.FullName;
		context.DiagramRegistry.RegisterDiagramForCaching(LocalSvgPath, EncodedUrl, outputDirectory);
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
		var markdownFileName = "unknown";
		if (context.MarkdownSourcePath?.Name is not null)
		{
			markdownFileName = Path.GetFileNameWithoutExtension(context.MarkdownSourcePath.Name);
		}

		var filename = $"{markdownFileName}-diagram-{DiagramType}-{ContentHash}.svg";
		var localPath = Path.Combine("images", "generated-graphs", filename);

		// Normalize path separators to forward slashes for web compatibility
		return localPath.Replace(Path.DirectorySeparatorChar, '/');
	}


}
