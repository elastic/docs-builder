// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Page;
using RazorSlices;

namespace Elastic.Codex.Page;

/// <summary>
/// Page view factory for codex builds. Uses the codex-specific layout with _CodexHeader and _CodexFooter.
/// </summary>
public class CodexPageViewFactory : IPageViewFactory
{
	/// <inheritdoc />
	public RazorSlice Create(IndexViewModel viewModel) =>
		Index.Create(viewModel);
}
