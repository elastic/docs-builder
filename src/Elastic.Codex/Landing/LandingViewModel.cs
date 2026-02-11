// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Codex.Navigation;

namespace Elastic.Codex.Landing;

/// <summary>
/// View model for the codex landing page.
/// </summary>
public class LandingViewModel(CodexRenderContext context) : CodexViewModel(context)
{
	/// <summary>
	/// The codex index page model.
	/// </summary>
	public required CodexIndexPage IndexPage { get; init; }

	/// <summary>
	/// Information about all documentation sets grouped by category.
	/// </summary>
	public ILookup<string?, CodexDocumentationSetInfo> DocumentationSetsByCategory =>
		CodexNavigation?.DocumentationSetInfos?.ToLookup(ds => ds.Category)
		?? Array.Empty<CodexDocumentationSetInfo>().ToLookup(ds => ds.Category);
}
