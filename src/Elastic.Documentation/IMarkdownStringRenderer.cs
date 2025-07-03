// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Documentation;

public interface IMarkdownStringRenderer
{
	string Render(string markdown, IFileInfo? source);
}
public class NoopMarkdownStringRenderer : IMarkdownStringRenderer
{
	private NoopMarkdownStringRenderer() { }

	public static NoopMarkdownStringRenderer Instance { get; } = new();

	/// <inheritdoc />
	public string Render(string markdown, IFileInfo? source) => string.Empty;
}
