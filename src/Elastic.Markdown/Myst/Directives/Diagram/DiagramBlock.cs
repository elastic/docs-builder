// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
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

		// Register diagram for tracking and cleanup
		DiagramRegistry.RegisterDiagram(LocalSvgPath);

		// Cache diagram asynchronously - fire and forget
		// Use simplified approach without lock files to avoid orphaned locks
		_ = Task.Run(() => TryCacheDiagramAsync(context));
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
		if (context.MarkdownSourcePath?.FullName != null)
		{
			markdownFileName = Path.GetFileNameWithoutExtension(context.MarkdownSourcePath.FullName);
		}

		var filename = $"{markdownFileName}-diagram-{DiagramType}-{ContentHash}.svg";
		return Path.Combine("images", "generated-graphs", filename);
	}

	private async Task TryCacheDiagramAsync(ParserContext context)
	{
		if (string.IsNullOrEmpty(EncodedUrl) || string.IsNullOrEmpty(LocalSvgPath))
			return;

		try
		{
			// Determine the full output path
			var outputDirectory = context.Build.DocumentationOutputDirectory.FullName;
			var fullPath = Path.Combine(outputDirectory, LocalSvgPath);

			// Skip if file already exists - simple check without locking
			if (File.Exists(fullPath))
				return;

			// Create directory if it doesn't exist
			var directory = Path.GetDirectoryName(fullPath);
			if (directory != null && !Directory.Exists(directory))
			{
				_ = Directory.CreateDirectory(directory);
			}

			// Download SVG from Kroki using shared HttpClient
			var svgContent = await DiagramHttpClient.Instance.GetStringAsync(EncodedUrl);

			// Basic validation - ensure we got SVG content
			// SVG can start with XML declaration, DOCTYPE, or directly with <svg>
			if (string.IsNullOrWhiteSpace(svgContent) || !svgContent.Contains("<svg", StringComparison.OrdinalIgnoreCase))
			{
				// Invalid content - don't cache
				return;
			}

			// Write to local file atomically using a temp file
			var tempPath = fullPath + ".tmp";
			await File.WriteAllTextAsync(tempPath, svgContent);
			File.Move(tempPath, fullPath);
		}
		catch (HttpRequestException)
		{
			// Network-related failures - silent fallback to Kroki URLs
			// Caching is opportunistic, network issues shouldn't generate warnings
		}
		catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
		{
			// Timeout - silent fallback to Kroki URLs
			// Timeouts are expected in slow network conditions
		}
		catch (IOException)
		{
			// File system issues - silent fallback to Kroki URLs
			// Disk space or permission issues shouldn't break builds
		}
		catch (Exception)
		{
			// Unexpected errors - silent fallback to Kroki URLs
			// Caching is opportunistic, any failure should fallback gracefully
		}
	}
}
