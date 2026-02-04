// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Codex.Navigation;

namespace Elastic.Codex.Category;

/// <summary>
/// View model for category index pages.
/// </summary>
public class CategoryViewModel(CodexRenderContext context) : CodexViewModel(context)
{
	/// <summary>
	/// The category navigation item.
	/// </summary>
	public required CategoryNavigation Category { get; init; }

	/// <summary>
	/// Documentation sets in this category.
	/// </summary>
	public IEnumerable<CodexDocumentationSetInfo> DocumentationSets =>
		CodexNavigation.DocumentationSetInfos.Where(ds => ds.Category == Category.CategoryName);
}
