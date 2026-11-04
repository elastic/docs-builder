// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Navigation;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Structural;
using Elastic.ApiExplorer.Supplemental;
using Elastic.Documentation;
using Elastic.Documentation.Navigation;
using Microsoft.AspNetCore.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Landing;

/// <summary>
/// Lightweight navigation entry for intro/outro markdown files listed under <c>api:</c>.
/// These pages are rendered using the regular markdown processing pipeline and API Explorer layout.
/// </summary>
public class SimpleMarkdownNavigationItem(
	string url,
	string title,
	IFileInfo fileInfo,
	IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot
) : INavigationItem, IApiModel, ILeafNavigationItem<IApiModel>
{
	public string Url { get; } = url;
	public string NavigationTitle { get; } = title;
	public IFileInfo FileInfo { get; } = fileInfo;
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = navigationRoot;
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public bool Hidden => false;
	public int NavigationIndex { get; set; }
	public string Id { get; } = $"markdown-{Path.GetFileNameWithoutExtension(fileInfo.Name)}";
	public Uri Identifier { get; } = new("about:blank");
	public string Slug { get; } = CreateSlugFromFile(fileInfo);

	/// <summary>
	/// URLs of the spec's <c>x-topics</c> pages this page replaces. The version switcher matches pages by them; the build writes
	/// no redirect pages there, elastic/docs-infra redirects them.
	/// </summary>
	public IReadOnlyList<string> ReplacedTopicUrls { get; internal set; } = [];

	/// <summary>True for the page that stands for the Authentication topic: the spec's authentication tabs follow its Markdown.</summary>
	public bool ShowsAuthentication { get; internal set; }

	/// <summary>The pinned topic title, Authentication or Rate limiting, this page stands for, whatever its own navigation title is.</summary>
	public string? PinnedTitle { get; internal set; }

	/// <inheritdoc />
	public IApiModel Model => this;

	/// <summary>Creates a URL slug from a markdown filename. A <c>.vN</c> suffix is not part of the slug.</summary>
	public static string CreateSlugFromFile(IFileInfo markdownFile)
	{
		var fileName = Path.GetFileNameWithoutExtension(markdownFile.Name);
		if (ApiSupplementalName.TryParseVersionSuffix(markdownFile.Name, out var stem, out _))
			fileName = stem;
		return fileName.ToLowerInvariant().Replace(' ', '-').Replace('_', '-');
	}

	/// <summary>Throws if the slug collides with reserved API Explorer path segments.</summary>
	public static void ValidateSlugForCollisions(string slug, string productKey, string filePath)
	{
		if (ApiUrlBuilder.ReservedChildSegments.Contains(slug, StringComparer.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException(
				$"Markdown file slug '{slug}' (from '{filePath}') conflicts with reserved API Explorer segment in product '{productKey}'. Reserved segments: {string.Join(", ", ApiUrlBuilder.ReservedChildSegments)}"
			);
		}
	}

	/// <summary>
	/// Renders the markdown file using the API Explorer layout system.
	/// </summary>
	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var markdownContent = await context.BuildContext.ReadFileSystem.File.ReadAllTextAsync(FileInfo.FullName, ctx);
		var rendered = context.MarkdownRenderer.RenderPreservingFirstHeadingWithMetadata(markdownContent, FileInfo);
		var viewModel = new MarkdownPageViewModel(context)
		{
			PageTitle = rendered.Title ?? NavigationTitle,
			MetaTitle = rendered.MetaTitle,
			BodyHtml = new HtmlString(rendered.Html),
			DescriptionMarkdown = ApiSeoDescription.FirstParagraph(ApiMarkdownFrontMatter.StripLeadingFrontMatter(markdownContent)),
			AuthenticationRequirements = ShowsAuthentication ? StructuralViewModel.ReadRequirements(context.Model) : [],
			AuthenticationOptional = ShowsAuthentication && OpenApiAuthSchemeResolver.AllowsAnonymous(context.Model.Security)
		};
		var slice = MarkdownPageView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}

	public async Task<string?> RenderCommonMarkAsync(ApiRenderContext context, Cancel ctx = default)
	{
		var markdownContent = await context.BuildContext.ReadFileSystem.File.ReadAllTextAsync(FileInfo.FullName, ctx).ConfigureAwait(false);
		var apiBaseUrl = context.CurrentNavigation.NavigationRoot.Url;
		var markdown = ApiMarkdown.Prepare(markdownContent, apiBaseUrl);
		var requirements = ShowsAuthentication ? StructuralViewModel.ReadRequirements(context.Model) : [];
		if (requirements.Count == 0)
			return markdown;

		var tabs = new StringBuilder();
		ApiCommonMark.Heading(tabs, 2, "Authentication");
		OperationCommonMark.WriteAuthentication(
			tabs,
			requirements,
			OpenApiAuthSchemeResolver.AllowsAnonymous(context.Model.Security),
			apiBaseUrl
		);
		return $"{markdown?.TrimEnd()}\n\n{tabs}";
	}

	// pageModel is unused (no pre-built model for markdown pages); delegate to the real implementation.
	public Task<string?> RenderCommonMarkAsync(ApiRenderContext context, object? pageModel, Cancel ctx = default) =>
		RenderCommonMarkAsync(context, ctx);
}
