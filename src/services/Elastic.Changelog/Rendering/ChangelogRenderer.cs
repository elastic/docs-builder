// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.Rendering.Asciidoc;
using Elastic.Changelog.Rendering.Markdown;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Coordinates rendering of changelog output to different formats.
/// </summary>
public class ChangelogRenderer(IFileSystem fileSystem, ILogger logger)
{
	/// <summary>
	/// Renders changelog output based on the specified file type.
	/// </summary>
	public async Task RenderAsync(
		ChangelogFileType fileType,
		ChangelogRenderContext context,
		Cancel ctx)
	{
		switch (fileType)
		{
			case ChangelogFileType.Asciidoc:
				await RenderAsciidocAsync(context, ctx);
				break;

			case ChangelogFileType.Markdown:
				await RenderMarkdownAsync(context, ctx);
				break;

			default:
				throw new ArgumentException($"Unknown changelog file type: {fileType}", nameof(fileType));
		}
	}

	private async Task RenderAsciidocAsync(ChangelogRenderContext context, Cancel ctx)
	{
		var asciidocRenderer = new ChangelogAsciidocRenderer(fileSystem);
		await asciidocRenderer.RenderAsciidoc(context, ctx);
		logger.LogInformation("Rendered changelog asciidoc file to {OutputDir}", context.OutputDir);
	}

	private async Task RenderMarkdownAsync(ChangelogRenderContext context, Cancel ctx)
	{
		var markdownRenderer = new ChangelogMarkdownRenderer(fileSystem);
		await markdownRenderer.RenderAsync(context, ctx);
		logger.LogInformation("Rendered changelog markdown files to {OutputDir}", context.OutputDir);
	}
}
