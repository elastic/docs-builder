// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Codex.Navigation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Codex;

/// <summary>
/// Context for rendering codex pages.
/// </summary>
public record CodexRenderContext(
	BuildContext BuildContext,
	CodexNavigation CodexNavigation,
	StaticFileContentHashProvider StaticFileContentHashProvider
) : RenderContext<CodexNavigation>(BuildContext, CodexNavigation)
{
	/// <summary>
	/// Pre-rendered navigation HTML.
	/// </summary>
	public required string NavigationHtml { get; init; }

	/// <summary>
	/// The current navigation item being rendered.
	/// </summary>
	public required INavigationItem CurrentNavigation { get; init; }
}
