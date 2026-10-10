// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.ApiExplorer.StyleGuide;

/// <summary>
/// The API pages of the style guide's previews. A made-up spec goes through the real generator, navigation and views, so the
/// catalog, an API's landing page and its operation pages are the pages a build writes, around placeholder content.
/// </summary>
public static class ApiStyleGuidePreviews
{
	public static IReadOnlyList<string> Ids { get; } = ["api-catalog", "api-product", "api-operation", "api-operation-plain"];

	private sealed class PassthroughMarkdown : IMarkdownStringRenderer
	{
		public string Render(string markdown, IFileInfo? source) => $"<p>{System.Net.WebUtility.HtmlEncode(markdown)}</p>";
	}

	/// <summary>Renders a preview as a complete HTML document, or <c>null</c> when the id is unknown.</summary>
	/// <param name="template">The context of the running site. A copy of it, with the new design on, renders the pages.</param>
	public static async Task<string?> RenderAsync(
		string id,
		BuildContext template,
		EmbeddedOrPhysicalFileProvider staticFiles,
		Cancel ctx = default
	)
	{
		if (!Ids.Contains(id))
			return null;

		var context = Context(template);
		var markdown = new PassthroughMarkdown();
		var collector = new DiagnosticsCollector([]);
		using var spec = new MemoryStream(Encoding.UTF8.GetBytes(LoremApiSpec.Json));
		var document = await OpenApiReader.Instance.ReadAsync(spec, LoremApiSpec.FileName, collector)
			?? throw new InvalidOperationException("The placeholder API spec could not be read.");

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, markdown);
		var navigation = generator.CreateNavigation("lorem", document);
		var hashes = new StaticFileContentHashProvider(staticFiles);

		return id == "api-catalog"
			? await RenderCatalogAsync(context, markdown, hashes, ctx)
			: await RenderPageAsync(context, document, navigation, markdown, hashes, id, ctx);
	}

	private static BuildContext Context(BuildContext template)
	{
		var configuration = new ConfigurationContext
		{
			VersionsConfiguration = template.VersionsConfiguration,
			ConfigurationFileProvider = template.ConfigurationFileProvider,
			Endpoints = template.Endpoints,
			ProductsConfiguration = template.ProductsConfiguration,
			LegacyUrlMappings = template.LegacyUrlMappings,
			SearchConfiguration = template.SearchConfiguration
		};
		var context = new BuildContext(template.Collector, template.FileSystem, configuration);
		context.Configuration.Features.NavigationPreviewEnabled = true;
		return context;
	}

	private static async Task<string> RenderCatalogAsync(
		BuildContext context,
		IMarkdownStringRenderer markdown,
		StaticFileContentHashProvider hashes,
		Cancel ctx
	)
	{
		ApiCatalogEntry[] entries =
		[
			new("lorem", "Lorem API", "/api/doc/lorem/", Description: "Lorem ipsum dolor sit amet, consectetur adipiscing elit."),
			new("dolor", "Dolor API", "/api/doc/dolor/", Description: "Sed do eiusmod tempor incididunt ut labore."),
			new("amet", "Amet API", "/api/doc/amet/", Description: "Ut enim ad minim veniam, quis nostrud.")
		];
		var navigation = new ApiCatalogNavigationItem("/api/", entries);
		var document = new Microsoft.OpenApi.OpenApiDocument
		{
			Info = new Microsoft.OpenApi.OpenApiInfo { Title = ApiCatalog.PageTitle, Version = "1.0" }
		};
		var renderContext = new ApiRenderContext(context, document, hashes)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = navigation.Index,
			MarkdownRenderer = markdown
		};
		var page = navigation.Index.Model;
		return await WriteAsync(stream => page.RenderAsync(stream, renderContext, ctx));
	}

	private static async Task<string?> RenderPageAsync(
		BuildContext context,
		Microsoft.OpenApi.OpenApiDocument document,
		LandingNavigationItem navigation,
		IMarkdownStringRenderer markdown,
		StaticFileContentHashProvider hashes,
		string id,
		Cancel ctx
	)
	{
		INavigationItem? current = id switch
		{
			"api-product" => navigation,
			"api-operation" => Operations(navigation).FirstOrDefault(o => o.Model.Operation.OperationId == "search"),
			_ => Operations(navigation).FirstOrDefault(o => o.Model.Operation.OperationId == "get")
		};
		if (current is null)
			return null;

		var renderer = new IsolatedBuildNavigationHtmlWriter(context, navigation, suppressNavigationDropdown: true);
		var rendered = await renderer.RenderNavigation(current.NavigationRoot, current, ctx);
		var renderContext = new ApiRenderContext(context, document, hashes)
		{
			NavigationHtml = rendered.Html,
			CurrentNavigation = current,
			MarkdownRenderer = markdown
		};

		IApiModel model = current switch
		{
			OperationNavigationItem operation => operation.Model,
			_ => navigation.Index.Model
		};
		var pageModel = model.CreatePageModel(renderContext);
		return await WriteAsync(stream => model.RenderAsync(stream, renderContext, pageModel, ctx));
	}

	private static IEnumerable<OperationNavigationItem> Operations(INavigationItem item)
	{
		if (item is OperationNavigationItem operation)
			yield return operation;
		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			yield break;
		foreach (var child in node.NavigationItems)
		{
			foreach (var found in Operations(child))
				yield return found;
		}
	}

	// The views write to a file stream, so a page is rendered to a temporary file and read back.
	private static async Task<string> WriteAsync(Func<FileSystemStream, Task> render)
	{
		var fileSystem = new FileSystem();
		var path = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), $"style-guide-api-{Guid.NewGuid():N}.html");
		try
		{
			await using (var stream = fileSystem.FileStream.New(path, FileMode.Create))
				await render(stream);
			return await fileSystem.File.ReadAllTextAsync(path);
		}
		finally
		{
			fileSystem.File.Delete(path);
		}
	}
}
