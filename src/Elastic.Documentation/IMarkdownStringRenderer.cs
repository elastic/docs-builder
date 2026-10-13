// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Documentation;

public readonly record struct MarkdownRenderResult(string Html, string? Title, string? MetaTitle);

public interface IMarkdownStringRenderer
{
	string Render(string markdown, IFileInfo? source);

	/// <summary>
	/// Renders markdown without removing the first level-1 heading. <see cref="Render"/> strips it so the layout can render the title separately; API intro/outro pages keep the hash heading as the main title in HTML.
	/// </summary>
	string RenderPreservingFirstHeading(string markdown, IFileInfo? source) => Render(markdown, source);

	/// <summary>
	/// Renders markdown without removing the first level-1 heading and returns its page metadata.
	/// </summary>
	MarkdownRenderResult RenderPreservingFirstHeadingWithMetadata(string markdown, IFileInfo? source) =>
		new(RenderPreservingFirstHeading(markdown, source), null, null);

	/// <summary>
	/// Renders OpenAPI description fragments without docset link or code-block validation.
	/// </summary>
	string RenderApiDescription(string markdown, IFileInfo? source) => Render(markdown, source);
}

public class NoopMarkdownStringRenderer : IMarkdownStringRenderer
{
	private NoopMarkdownStringRenderer() { }

	public static NoopMarkdownStringRenderer Instance { get; } = new();

	/// <inheritdoc />
	public string Render(string markdown, IFileInfo? source) => string.Empty;
}
