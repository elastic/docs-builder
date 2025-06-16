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
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;

namespace Elastic.ApiExplorer;

public class ApiGroupingNavigationItem : INodeNavigationItem<ApiEndpoint, INavigationItem>
{
	public ApiGroupingNavigationItem(int depth, INodeNavigationItem<INavigationModel, INavigationItem> parentGroup, LandingNavigationItem rootNavigation)
	{
		Parent = parentGroup;
		Depth = depth;
		NavigationRoot = rootNavigation;
		Id = NavigationRoot.Id;

		Index = apiEndpoint;
		//TODO
		NavigationTitle = apiEndpoint.OpenApiPath.Summary;
	}

	public string Id { get; }
	public int Depth { get; }
	public ApiEndpoint Index { get; }
	public string Url => NavigationItems.First().Url;
	public string NavigationTitle { get; }
	public bool Hidden => false;

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; set; } = [];

	public INodeNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	public int NavigationIndex { get; set; }
}

public record ApiClassification(string Name, string Description, ApiTag[] Tags);
public record ApiTag(string Name, string Description);
public record ApiEndpoint(string Route, IOpenApiPathItem OpenApiPath);

public class OpenApiGenerator(BuildContext context, ILoggerFactory logger)
{
	private readonly ILogger _logger = logger.CreateLogger<OpenApiGenerator>();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;
	private readonly StaticFileContentHashProvider _contentHashProvider = new(new EmbeddedOrPhysicalFileProvider(context));

	public static LandingNavigationItem CreateNavigation(OpenApiDocument openApiDocument)
	{
		var url = "/api";
		var rootNavigation = new LandingNavigationItem(url);

		var grouped = openApiDocument.Paths
			.Select(p =>
			{
				var op = p.Value.Operations.First();
				var extensions = op.Value.Extensions;
				var ns = (extensions?.TryGetValue("x-namespace", out var n) ?? false) && n is OpenApiAny anyNs
					? anyNs.Node.GetValue<string>()
					: null;
				var api = (extensions?.TryGetValue("x-api-name", out var a) ?? false) && a is OpenApiAny anyApi
					? anyApi.Node.GetValue<string>()
					: null;
				var tag = op.Value.Tags?.FirstOrDefault()?.Reference.Id;
				if (tag is not null)
				{
				}
				var classification = openApiDocument.Info.Description == "Elasticsearch Request & Response Specification" ? ClassifyElasticsearchTag(tag ?? "global") : string.Empty;

				return new
				{
					Classification = classification,
					Namespace = ns,
					Api = api,
					Tag = tag,
					Path = p
				};
			})
			.GroupBy(g => g.Classification)
			.ToArray();

		var classifications = new List<ApiClassification>();
		foreach (var group in grouped)
		{
			var tags = new List<ApiTag>();
			foreach (var tagGroup in group.GroupBy(g => g.Tag))
			{
				var tag = new ApiTag(tagGroup.Key ?? "global", "");
				tags.Add(tag);
			}

			var classification = new ApiClassification(group.Key, "", tags.ToArray());
			classifications.Add(classification);
		}


		var rootItems = new List<ApiGroupingNavigationItem>();
		foreach (var group in grouped)
		{
			var cl = group.Key;

			var categoryItem = new ApiGroupingNavigationItem(1, rootNavigation, rootNavigation);
			var tagItems = new List<ApiGroupingNavigationItem>();
			foreach (var tagGroup in group.GroupBy(g => g.Tag))
			{
				var tag = tagGroup.Key ?? "global";

				var tagNavigationItem = new ApiGroupingNavigationItem(1, categoryItem, rootNavigation);

				var endpointItems = new List<EndpointNavigationItem>();
				foreach (var endpoint in tagGroup)
				{
					var api = endpoint.Namespace is null ? endpoint.Api ?? "global" : $"{endpoint.Namespace}.{endpoint.Api}";

					var path = endpoint.Path;
					var endpointUrl = $"{url}/{path.Key.Trim('/').Replace('/', '-').Replace("{", "").Replace("}", "")}";

					var apiEndpoint = new ApiEndpoint(endpoint.Path.Key, endpoint.Path.Value);
					var operationItems = new List<OperationNavigationItem>();
					var endpointNavigationItem = new EndpointNavigationItem(1, endpointUrl, apiEndpoint, tagNavigationItem, rootNavigation);
					if (endpoint.Path.Value.Operations.Count > 1)
					{
						foreach (var operation in endpoint.Path.Value.Operations)
						{
							var operationUrl = $"{endpointUrl}/{operation.Key.ToString().ToLowerInvariant()}";
							var apiOperation = new ApiOperation(operation.Key, operation.Value);
							var navigation = new OperationNavigationItem(2, operationUrl, apiOperation, endpointNavigationItem, rootNavigation);
							operationItems.Add(navigation);
						}
						endpointNavigationItem.NavigationItems = operationItems;
					}
					endpointItems.Add(endpointNavigationItem);
				}
				tagNavigationItem.NavigationItems = endpointItems;
				tagItems.Add(tagNavigationItem);
			}
			categoryItem.NavigationItems = tagItems;
			rootItems.Add(categoryItem);
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
			NavigationHtml = navigationHtml,
			CurrentNavigation = navigation,
		};
		_ = await Render(navigation.Index, renderContext, ctx);
		foreach (var endpoint in navigation.NavigationItems)
		{
			_ = await Render(endpoint.Index, renderContext, ctx);
			foreach (var operation in endpoint.NavigationItems)
				_ = await Render(operation.Model, renderContext, ctx);
		}
	}

	private async Task<IFileInfo> Render<T>(T page, ApiRenderContext renderContext, Cancel ctx)
		where T : INavigationModel, IPageRenderer<ApiRenderContext>
	{
		var outputFile = OutputFile(renderContext.CurrentNavigation);
		if (!outputFile.Directory!.Exists)
			outputFile.Directory.Create();

		await using var stream = _writeFileSystem.FileStream.New(outputFile.FullName, FileMode.OpenOrCreate);
		await page.RenderAsync(stream, renderContext, ctx);
		return outputFile;

		IFileInfo OutputFile(INavigationItem currentNavigation)
		{
			const string indexHtml = "index.html";
			var fileName = currentNavigation.Url + "/" + indexHtml;
			var fileInfo = _writeFileSystem.FileInfo.New(Path.Combine(context.DocumentationOutputDirectory.FullName, fileName.Trim('/')));
			return fileInfo;
		}
	}

	private static string ClassifyElasticsearchTag(string tag)
	{
#pragma warning disable IDE0066
		switch (tag)
#pragma warning restore IDE0066
		{
			case "sql":
			case "eql":
			case "esql":
			case "search":
			case "document":
				return "common";

			case "autoscaling":
			case "ccr":
			case "indices":
			case "data stream":
			case "ilm":
			case "slm":
			case "cluster":
			case "rollup":
			case "searchable_snapshots":
			case "shutdown":
			case "snapshot":
			case "script":
			case "search_application":
			case "connector":
				return "management";

			case "cat":
			case "license":
			case "info":
			case "tasks":
			case "xpack":
			case "health_report":
			case "features":
			case "migration":
			case "watcher":
				return "info";


			case "ml trained model":
			case "ml anomaly":
			case "ml data frame":
			case "ml":
			case "inference":
			case "text_structure":
			case "query_rules":
			case "analytics":
			case "graph":
				return "ai/ml";

			case "ingest":
			case "enrich":
			case "transform":
			case "fleet":
			case "logstash":
			case "synonyms":
				return "ingest";

			case "security":
				return "security";
		}
		return "unknown";
	}
}
