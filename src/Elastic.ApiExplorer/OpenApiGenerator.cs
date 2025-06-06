// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Endpoints;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Elastic.ApiExplorer;

public class OpenApiGenerator(BuildContext context, ILoggerFactory logger)
{
	private readonly ILogger _logger = logger.CreateLogger<OpenApiGenerator>();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;
	private readonly StaticFileContentHashProvider _contentHashProvider = new(new EmbeddedOrPhysicalFileProvider(context));

	public static LandingNavigationItem CreateNavigation(OpenApiDocument openApiDocument)
	{
		var url = "/api";
		var rootNavigation = new LandingNavigationItem(url);
		var rootItems = new List<EndpointNavigationItem>();

		foreach (var path in openApiDocument.Paths)
		{
			var endpointUrl = $"{url}/{path.Key.Trim('/').Replace('/', '-').Replace("{", "").Replace("}", "")}";
			var apiEndpoint = new ApiEndpoint(endpointUrl, path.Key, path.Value, rootNavigation);
			var endpointNavigationItem = new EndpointNavigationItem(1, apiEndpoint, rootNavigation, rootNavigation);
			var endpointNavigationItems = new List<OperationNavigationItem>();
			foreach (var operation in path.Value.Operations)
			{
				var operationUrl = $"{endpointUrl}/{operation.Key.ToString().ToLowerInvariant()}";
				var apiOperation = new ApiOperation(operationUrl, operation.Key, operation.Value, rootNavigation);
				var navigation = new OperationNavigationItem(2, apiOperation, endpointNavigationItem, rootNavigation);
				endpointNavigationItems.Add(navigation);
			}

			endpointNavigationItem.NavigationItems = endpointNavigationItems;
			rootItems.Add(endpointNavigationItem);
		}

		rootNavigation.NavigationItems = rootItems;
		return rootNavigation;
	}

	public async Task Generate(Cancel ctx = default)
	{
		if (context.Configuration.OpenApiSpecification is null)
			return;

		var openApiDocument = await OpenApiReader.Create(context.Configuration.OpenApiSpecification);
		if (openApiDocument is null)
			return;

		var navigation = CreateNavigation(openApiDocument);
		_logger.LogInformation("Generating OpenApiDocument {Title}", openApiDocument.Info.Title);

		var navigationRenderer = new IsolatedBuildNavigationHtmlWriter(context, navigation);
		var navigationHtml = await navigationRenderer.RenderNavigation(navigation, new Uri("http://ignored.example"), ctx);

		var renderContext = new ApiRenderContext(context, openApiDocument, _contentHashProvider)
		{
			NavigationHtml = navigationHtml
		};
		_ = await Render(navigation.Index, renderContext, ctx);
		foreach (var endpoint in navigation.NavigationItems.OfType<EndpointNavigationItem>())
		{
			_ = await Render(endpoint.Index, renderContext, ctx);
			foreach (var operation in endpoint.NavigationItems.OfType<OperationNavigationItem>())
				_ = await Render(operation.Current, renderContext, ctx);
		}
	}

	private async Task<IFileInfo> Render<T>(T page, ApiRenderContext renderContext, Cancel ctx)
		where T : IPageInformation, IPageRenderer<ApiRenderContext>
	{
		var outputFile = OutputFile(page);
		if (!outputFile.Directory!.Exists)
			outputFile.Directory.Create();

		await using var stream = _writeFileSystem.FileStream.New(outputFile.FullName, FileMode.OpenOrCreate);
		await page.RenderAsync(stream, renderContext, ctx);
		return outputFile;

		IFileInfo OutputFile(IPageInformation pageInformation)
		{
			const string indexHtml = "index.html";
			var fileName = pageInformation.Url + "/" + indexHtml;
			var fileInfo = _writeFileSystem.FileInfo.New(Path.Combine(context.DocumentationOutputDirectory.FullName, fileName.Trim('/')));
			return fileInfo;
		}
	}
}


