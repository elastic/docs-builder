// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Codex.Navigation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;

namespace Elastic.Codex;

/// <summary>
/// Base view model for codex pages.
/// </summary>
public abstract class CodexViewModel(CodexRenderContext context)
{
	/// <summary>
	/// Pre-rendered navigation HTML.
	/// </summary>
	public string NavigationHtml { get; } = context.NavigationHtml;

	/// <summary>
	/// Static file content hash provider for cache busting.
	/// </summary>
	public StaticFileContentHashProvider StaticFileContentHashProvider { get; } = context.StaticFileContentHashProvider;

	/// <summary>
	/// The current navigation item being rendered.
	/// </summary>
	public INavigationItem CurrentNavigationItem { get; } = context.CurrentNavigation;

	/// <summary>
	/// The codex navigation root.
	/// </summary>
	public CodexNavigation CodexNavigation { get; } = context.CodexNavigation;

	/// <summary>
	/// The build context.
	/// </summary>
	public BuildContext BuildContext { get; } = context.BuildContext;

	/// <summary>
	/// Creates the global layout model for the page.
	/// </summary>
	public GlobalLayoutViewModel CreateGlobalLayoutModel() =>
		new()
		{
			DocsBuilderVersion = ShortId.Create(BuildContext.Version),
			DocSetName = CodexNavigation.NavigationTitle,
			Description = "Documentation Codex",
			CurrentNavigationItem = CurrentNavigationItem,
			Previous = null,
			Next = null,
			NavigationHtml = NavigationHtml,
			UrlPathPrefix = BuildContext.UrlPathPrefix,
			AllowIndexing = BuildContext.AllowIndexing,
			CanonicalBaseUrl = BuildContext.CanonicalBaseUrl,
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Features = new FeatureFlags([]),
			StaticFileContentHashProvider = StaticFileContentHashProvider,
			BuildType = BuildContext.BuildType
		};
}
