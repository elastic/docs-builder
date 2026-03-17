// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using RazorSlices;

namespace Elastic.Markdown.Page;

/// <summary>
/// Factory for creating the page view slice used when rendering markdown content.
/// Allows different layouts (e.g. codex vs standard docs) to be used based on build context.
/// </summary>
public interface IPageViewFactory
{
	/// <summary>
	/// Creates the page slice for the given view model.
	/// </summary>
	RazorSlice Create(IndexViewModel viewModel);
}

/// <summary>
/// Default implementation that uses the standard Elastic.Markdown layout.
/// </summary>
public class DefaultPageViewFactory : IPageViewFactory
{
	/// <inheritdoc />
	public RazorSlice Create(IndexViewModel viewModel) =>
		Index.Create(viewModel);
}
