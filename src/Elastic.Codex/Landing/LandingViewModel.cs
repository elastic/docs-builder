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
	/// Group navigations for rendering group cards on the landing page.
	/// Each group aggregates one or more documentation sets.
	/// </summary>
	public IEnumerable<GroupNavigation> Groups =>
		CodexNavigation?.GroupNavigations?.OrderBy(g => g.DisplayTitle)
		?? Enumerable.Empty<GroupNavigation>();

	/// <summary>
	/// Documentation sets that are not part of any group, for rendering individual docset cards.
	/// </summary>
	public IEnumerable<CodexDocumentationSetInfo> UngroupedDocumentationSets =>
		CodexNavigation?.DocumentationSetInfos?
			.Where(ds => string.IsNullOrEmpty(ds.Group))
			.OrderBy(ds => ds.Title)
		?? Enumerable.Empty<CodexDocumentationSetInfo>();
}
