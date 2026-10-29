// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.AspNetCore.Html;

namespace Elastic.ApiExplorer.Landing;

/// <summary>
/// View model for intro/outro markdown pages in the API Explorer layout.
/// </summary>
public class MarkdownPageViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required string PageTitle { get; init; }
	public string? MetaTitle { get; init; }

	public required HtmlString BodyHtml { get; init; }

	public string? DescriptionMarkdown { get; init; }

	/// <summary>Set on the page that stands for the Authentication topic: the tabs follow its Markdown.</summary>
	public IReadOnlyList<AuthRequirement> AuthenticationRequirements { get; init; } = [];

	public bool AuthenticationOptional { get; init; }

	public AuthenticationSectionModel? Authentication =>
		AuthenticationRequirements.Count == 0
			? null
			: new AuthenticationSectionModel(AuthenticationRequirements, AuthenticationOptional, markdown => RenderMarkdown(markdown));

	/// <inheritdoc />
	protected override string? LayoutPageTitle => string.IsNullOrWhiteSpace(MetaTitle) ? PageTitle : MetaTitle;

	protected override string BreadcrumbCurrentTitle => PageTitle;

	protected override string? LayoutPageDescription => DescriptionMarkdown;

	protected override bool ShowPageActions => false;
}
