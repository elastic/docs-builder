// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Changelog;

/// <summary>
/// Controls PR/issue link visibility for the changelog directive (aligns with changelog render link-visibility).
/// </summary>
public enum ChangelogLinkVisibility
{
	/// <summary>Use <see cref="ChangelogInlineRenderer.ShouldHideLinksForRepo"/> from bundle repo + assembler private list.</summary>
	Auto,

	/// <summary>Show PR/issue links even when the bundle source repo is treated as private.</summary>
	KeepLinks,

	/// <summary>Hide (comment) all PR/issue links for the bundle.</summary>
	HideLinks
}
