// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
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

public interface IApiModel : INavigationModel;

public interface IApiGroupingModel : IApiModel;

public record ApiClassification(string Name, string Description, IReadOnlyCollection<ApiTag> Tags) : IApiGroupingModel;

public record ApiTag(string Name, string Description, IReadOnlyCollection<ApiEndpoint> Endpoints) : IApiGroupingModel;

public record ApiEndpoint(string Route, IOpenApiPathItem OpenApiPath, List<ApiOperation> Operations) : IApiGroupingModel;

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
				var classification = openApiDocument.Info.Title == "Elasticsearch Request & Response Specification" ? ClassifyElasticsearchTag(tag ?? "unknown") : "unknown";

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

		// intermediate grouping of models to create the navigation tree
		// this is two-phased because we need to know if an endpoint has one or more operations
		var classifications = new List<ApiClassification>();
		foreach (var group in grouped)
		{
			var tags = new List<ApiTag>();
			foreach (var tagGroup in group.GroupBy(g => g.Tag))
			{
				var endpoints = new List<ApiEndpoint>();
				foreach (var endpoint in tagGroup)
				{
					var api = endpoint.Namespace is null ? endpoint.Api ?? null : $"{endpoint.Namespace}.{endpoint.Api}";
					var operations = new List<ApiOperation>();
					foreach (var operation in endpoint.Path.Value.Operations)
					{
						var apiOperation = new ApiOperation(operation.Key, operation.Value);
						operations.Add(apiOperation);
					}
					var apiEndpoint = new ApiEndpoint(endpoint.Path.Key, endpoint.Path.Value, operations);
					endpoints.Add(apiEndpoint);
				}
				var tag = new ApiTag(tagGroup.Key ?? "unknown", "", endpoints);
				tags.Add(tag);
			}
			var classification = new ApiClassification(group.Key, "", tags);
			classifications.Add(classification);
		}

		var topLevelNavigationItems = new List<IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem>>();
		var hasClassifications = classifications.Count > 1;
		foreach (var classification in classifications)
		{
			if (hasClassifications)
			{
				var classificationNavigationItem = new ClassificationNavigationItem(classification, rootNavigation, rootNavigation);
				var tagNavigationItems = new List<IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem>>();

				CreateTagNavigationItems(classification, rootNavigation, classificationNavigationItem, tagNavigationItems);
				topLevelNavigationItems.Add(classificationNavigationItem);
				// if there is only a single tag item will be added directly to the classificationNavigationItem, otherwise they will be added to the tagNavigationItems
				if (classificationNavigationItem.NavigationItems.Count == 0)
					classificationNavigationItem.NavigationItems = tagNavigationItems;
			}
			else
				CreateTagNavigationItems(classification, rootNavigation, rootNavigation, topLevelNavigationItems);
		}
		rootNavigation.NavigationItems = topLevelNavigationItems;
		return rootNavigation;
	}

	private static void CreateTagNavigationItems(ApiClassification classification, LandingNavigationItem rootNavigation,
		IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parent,
		List<IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem>> parentNavigationItems
	)
	{
		var hasTags = classification.Tags.Count > 1;
		foreach (var tag in classification.Tags)
		{
			var endpointNavigationItems = new List<IEndpointOrOperationNavigationItem>();
			if (hasTags)
			{
				var tagNavigationItem = new TagNavigationItem(tag, rootNavigation, parent);
				CreateEndpointNavigationItems(rootNavigation, tag, tagNavigationItem, endpointNavigationItems);
				parentNavigationItems.Add(tagNavigationItem);
				tagNavigationItem.NavigationItems = endpointNavigationItems;
			}
			else
			{
				CreateEndpointNavigationItems(rootNavigation, tag, parent, endpointNavigationItems);
				if (parent is ClassificationNavigationItem classificationNavigationItem)
					classificationNavigationItem.NavigationItems = endpointNavigationItems;
				else if (parent is LandingNavigationItem landingNavigationItem)
					landingNavigationItem.NavigationItems = endpointNavigationItems;
			}
		}
	}

	private static void CreateEndpointNavigationItems(
		LandingNavigationItem rootNavigation,
		ApiTag tag,
		IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parentNavigationItem,
		List<IEndpointOrOperationNavigationItem> endpointNavigationItems
	)
	{
		foreach (var endpoint in tag.Endpoints)
		{
			if (endpoint.Operations.Count > 1)
			{
				var endpointNavigationItem = new EndpointNavigationItem(endpoint, rootNavigation, parentNavigationItem);
				var operationNavigationItems = new List<OperationNavigationItem>();
				foreach (var operation in endpoint.Operations)
				{
					var operationNavigationItem = new OperationNavigationItem(operation, rootNavigation, endpointNavigationItem);
					operationNavigationItems.Add(operationNavigationItem);
				}
				endpointNavigationItem.NavigationItems = operationNavigationItems;
				endpointNavigationItems.Add(endpointNavigationItem);
			}
			else
			{
				var operation = endpoint.Operations.First();
				var operationNavigationItem = new OperationNavigationItem(operation, rootNavigation, parentNavigationItem);
				endpointNavigationItems.Add(operationNavigationItem);

			}
		}
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
		//foreach (var endpoint in navigation.NavigationItems)
		//{
		//_ = await Render(endpoint.Index, renderContext, ctx);
		//foreach (var operation in endpoint.NavigationItems)
		//	_ = await Render(operation.Model, renderContext, ctx);
		//}
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
