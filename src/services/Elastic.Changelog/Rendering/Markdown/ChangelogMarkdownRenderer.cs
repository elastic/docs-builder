// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Changelog.Rendering.Markdown;

/// <summary>
/// Coordinates rendering of all markdown changelog files.
/// </summary>
public class ChangelogMarkdownRenderer(IFileSystem fileSystem)
{
	/// <summary>
	/// Renders all markdown changelog files (index, breaking changes, deprecations, known issues).
	/// </summary>
	public async Task RenderAsync(ChangelogRenderContext context, Cancel ctx)
	{
		IChangelogMarkdownRenderer[] renderers =
		[
			new IndexMarkdownRenderer(fileSystem),
			new BreakingChangesMarkdownRenderer(fileSystem),
			new DeprecationsMarkdownRenderer(fileSystem),
			new KnownIssuesMarkdownRenderer(fileSystem)
		];

		foreach (var renderer in renderers)
			await renderer.RenderAsync(context, ctx);
	}
}
