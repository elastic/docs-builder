// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Codex.Navigation;

namespace Elastic.Codex.Group;

/// <summary>
/// View model for a group landing page (/g/slug) that lists only documentation sets in that group.
/// </summary>
public class GroupLandingViewModel(CodexRenderContext context) : CodexViewModel(context)
{
	/// <summary>
	/// The group navigation (landing + repos in this group).
	/// </summary>
	public required GroupNavigation Group { get; init; }

	/// <summary>
	/// Documentation sets in this group (for the group landing cards).
	/// </summary>
	public IEnumerable<CodexDocumentationSetInfo> DocumentationSets => Group.DocumentationSetInfos;
}
