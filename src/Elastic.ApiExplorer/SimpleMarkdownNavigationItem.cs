// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Navigation;
using Microsoft.AspNetCore.Html;
using RazorSlices;

namespace Elastic.ApiExplorer;

/// <summary>
/// Lightweight navigation entry for intro/outro markdown files listed under <c>api:</c>.
/// These pages are rendered using the regular markdown processing pipeline and API Explorer layout.
/// </summary>
public class SimpleMarkdownNavigationItem(
	string url,
	string title,
	IFileInfo fileInfo,
	IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot) : INavigationItem, IApiModel, ILeafNavigationItem<IApiModel>
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

	/// <inheritdoc />
	public IApiModel Model => this;

	/// <summary>Creates a URL slug from a markdown filename.</summary>
	public static string CreateSlugFromFile(IFileInfo markdownFile)
	{
		var fileName = Path.GetFileNameWithoutExtension(markdownFile.Name);
		return fileName.ToLowerInvariant()
			.Replace(' ', '-')
			.Replace('_', '-');
	}

	/// <summary>Throws if the slug collides with reserved API Explorer segments or an operation moniker.</summary>
	public static void ValidateSlugForCollisions(string slug, string productKey, string filePath, HashSet<string>? operationMonikers = null)
	{
		string[] reservedSegments = ["types", "tags"];

		if (reservedSegments.Contains(slug, StringComparer.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException(
				$"Markdown file slug '{slug}' (from '{filePath}') conflicts with reserved API Explorer segment in product '{productKey}'. Reserved segments: {string.Join(", ", reservedSegments)}");
		}

		if (operationMonikers != null && operationMonikers.Contains(slug))
		{
			throw new InvalidOperationException(
				$"Markdown file slug '{slug}' (from '{filePath}') conflicts with existing operation moniker in product '{productKey}'. Consider renaming the markdown file to avoid this collision.");
		}
	}

	/// <summary>
	/// Renders the markdown file using the API Explorer layout system.
	/// </summary>
	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var markdownContent = await context.BuildContext.ReadFileSystem.File.ReadAllTextAsync(FileInfo.FullName, ctx);
		var htmlContent = context.MarkdownRenderer.RenderPreservingFirstHeading(markdownContent, FileInfo);
		var viewModel = new MarkdownPageViewModel(context)
		{
			PageTitle = NavigationTitle,
			BodyHtml = new HtmlString(htmlContent ?? string.Empty)
		};
		var slice = MarkdownPageView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}
}
