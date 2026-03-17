// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.IO.Abstractions;

namespace Elastic.Changelog.Rendering.Markdown;

/// <summary>
/// Coordinates rendering of all markdown changelog files.
/// </summary>
public class ChangelogMarkdownRenderer(IFileSystem fileSystem)
{
	/// <summary>
	/// Renders all markdown changelog files (index, breaking changes, deprecations, known issues, highlights).
	/// </summary>
	public async Task RenderAsync(ChangelogRenderContext context, Cancel ctx)
	{
		// Check if there are any highlighted entries
		var hasHighlights = context.EntriesByType.Values
			.SelectMany(e => e)
			.Any(e => e.Highlight == true);

		var renderers = new List<IChangelogMarkdownRenderer>
		{
			new IndexMarkdownRenderer(fileSystem),
			new BreakingChangesMarkdownRenderer(fileSystem),
			new DeprecationsMarkdownRenderer(fileSystem),
			new KnownIssuesMarkdownRenderer(fileSystem)
		};

		// Only add highlights renderer if there are any highlights
		if (hasHighlights)
			renderers.Add(new HighlightsMarkdownRenderer(fileSystem));

		foreach (var renderer in renderers)
			await renderer.RenderAsync(context, ctx);
	}
}
