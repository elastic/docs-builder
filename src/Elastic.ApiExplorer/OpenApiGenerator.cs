// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Elastic.ApiExplorer;

public interface IApiModel : INavigationModel, IPageRenderer<ApiRenderContext>;

public interface IApiGroupingModel : IApiModel;

public record ApiClassification(string Name, string Description, IReadOnlyCollection<ApiTag> Tags) : IApiGroupingModel
{
	/// <inheritdoc />
	public Task RenderAsync(FileSystemStream stream, ApiRenderContext context, CancellationToken ctx = default) => Task.CompletedTask;
}

public record ApiTag(string Name, string Description, IReadOnlyCollection<ApiEndpoint> Endpoints) : IApiGroupingModel
{
	/// <inheritdoc />
	public Task RenderAsync(FileSystemStream stream, ApiRenderContext context, CancellationToken ctx = default) => Task.CompletedTask;
}

public record ApiEndpoint(List<ApiOperation> Operations, string? Name) : IApiGroupingModel
{
	/// <inheritdoc />
	public Task RenderAsync(FileSystemStream stream, ApiRenderContext context, CancellationToken ctx = default) => Task.CompletedTask;
}

public class OpenApiGenerator(ILoggerFactory logFactory, BuildContext context, IMarkdownStringRenderer markdownStringRenderer)
{
	private readonly ILogger _logger = logFactory.CreateLogger<OpenApiGenerator>();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;
	private readonly StaticFileContentHashProvider _contentHashProvider = new(new EmbeddedOrPhysicalFileProvider(context));

	public LandingNavigationItem CreateNavigation(string apiUrlSuffix, OpenApiDocument openApiDocument)
	{
		var url = $"{context.UrlPathPrefix}/api/" + apiUrlSuffix;
		var rootNavigation = new LandingNavigationItem(url);

		var ops = openApiDocument.Paths
			.SelectMany(p => p.Value.Operations.Select(op => (Path: p, Operation: op)))
			.Select(pair =>
			{
				var op = pair.Operation;
				var extensions = op.Value.Extensions;
				var ns = (extensions?.TryGetValue("x-namespace", out var n) ?? false) && n is OpenApiAny anyNs
					? anyNs.Node.GetValue<string>()
					: null;
				var api = (extensions?.TryGetValue("x-api-name", out var a) ?? false) && a is OpenApiAny anyApi
					? anyApi.Node.GetValue<string>()
					: null;
				var tag = op.Value.Tags?.FirstOrDefault()?.Reference.Id;
				var tagClassification = (extensions?.TryGetValue("x-tag-group", out var g) ?? false) && g is OpenApiAny anyTagGroup
					? anyTagGroup.Node.GetValue<string>()
					: openApiDocument.Info.Title == "Elasticsearch Request & Response Specification"
						? ClassifyElasticsearchTag(tag ?? "unknown")
						: "unknown";

				var apiString = ns is null
					? api ?? op.Value.Summary ?? Guid.NewGuid().ToString("N") : $"{ns}.{api}";
				return new
				{
					Classification = tagClassification,
					Api = apiString,
					Tag = tag,
					pair.Path,
					pair.Operation
				};
			})
			.ToArray();

		var nestedGrouping =
			(
				from op in ops
				group op by op.Classification
				into classificationGroup
				from tagGroup in
				from op in classificationGroup
				group op by op.Tag
				into apiGroups
				from apiGroup in
				from op in apiGroups
				group op by op.Api
				group apiGroup by apiGroups.Key
				group tagGroup by classificationGroup.Key
			).ToArray();


		/*
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
				var classification = openApiDocument.Info.Title == "Elasticsearch Request & Response Specification"
					? ClassifyElasticsearchTag(tag ?? "unknown")
					: "unknown";

				var apiString = ns is null ? api ?? Guid.NewGuid().ToString("N") : $"{ns}.{api}";
				return new
				{
					Classification = classification,
					Api = apiString,
					Tag = tag,
					Path = p
				};
			})
			.GroupBy(g => g.Classification)
			.ToArray();
		*/

		// intermediate grouping of models to create the navigation tree
		// this is two-phased because we need to know if an endpoint has one or more operations
		var classifications = new List<ApiClassification>();
		foreach (var classificationGroup in nestedGrouping)
		{
			var tags = new List<ApiTag>();
			foreach (var tagGroup in classificationGroup)
			{
				var apis = new List<ApiEndpoint>();
				foreach (var apiGroup in tagGroup)
				{
					var operations = new List<ApiOperation>();
					foreach (var api in apiGroup)
					{
						var operation = api.Operation;
						var apiOperation = new ApiOperation(operation.Key, operation.Value, api.Path.Key, api.Path.Value, apiGroup.Key);
						operations.Add(apiOperation);
					}
					var apiEndpoint = new ApiEndpoint(operations, apiGroup.Key);
					apis.Add(apiEndpoint);
				}
				var tag = new ApiTag(tagGroup.Key ?? "unknown", "", apis);
				tags.Add(tag);
			}
			var classification = new ApiClassification(classificationGroup.Key, "", tags);
			classifications.Add(classification);
		}

		var topLevelNavigationItems = new List<IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem>>();
		var hasClassifications = classifications.Count > 1;
		foreach (var classification in classifications)
		{
			if (hasClassifications && classification.Name != "common")
			{
				var classificationNavigationItem = new ClassificationNavigationItem(classification, rootNavigation, rootNavigation);
				var tagNavigationItems = new List<IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem>>();

				CreateTagNavigationItems(apiUrlSuffix, classification, classificationNavigationItem, classificationNavigationItem, tagNavigationItems);
				topLevelNavigationItems.Add(classificationNavigationItem);
				// if there is only a single tag item will be added directly to the classificationNavigationItem, otherwise they will be added to the tagNavigationItems
				if (classificationNavigationItem.NavigationItems.Count == 0)
					classificationNavigationItem.NavigationItems = tagNavigationItems;
			}
			else
				CreateTagNavigationItems(apiUrlSuffix, classification, rootNavigation, rootNavigation, topLevelNavigationItems);
		}
		rootNavigation.NavigationItems = topLevelNavigationItems;
		return rootNavigation;
	}

	private void CreateTagNavigationItems(
		string apiUrlSuffix,
		ApiClassification classification,
		IRootNavigationItem<IApiGroupingModel, INavigationItem> rootNavigation,
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
				CreateEndpointNavigationItems(apiUrlSuffix, rootNavigation, tag, tagNavigationItem, endpointNavigationItems);
				parentNavigationItems.Add(tagNavigationItem);
				tagNavigationItem.NavigationItems = endpointNavigationItems;
			}
			else
			{
				CreateEndpointNavigationItems(apiUrlSuffix, rootNavigation, tag, parent, endpointNavigationItems);
				if (parent is ClassificationNavigationItem classificationNavigationItem)
					classificationNavigationItem.NavigationItems = endpointNavigationItems;
				else if (parent is LandingNavigationItem landingNavigationItem)
					landingNavigationItem.NavigationItems = endpointNavigationItems;
			}
		}
	}

	private void CreateEndpointNavigationItems(
		string apiUrlSuffix,
		IRootNavigationItem<IApiGroupingModel, INavigationItem> rootNavigation,
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
					var operationNavigationItem = new OperationNavigationItem(context.UrlPathPrefix, apiUrlSuffix, operation, rootNavigation, endpointNavigationItem)
					{
						Hidden = true
					};
					operationNavigationItems.Add(operationNavigationItem);
				}
				endpointNavigationItem.NavigationItems = operationNavigationItems;
				endpointNavigationItems.Add(endpointNavigationItem);
			}
			else
			{
				var operation = endpoint.Operations.First();
				var operationNavigationItem = new OperationNavigationItem(context.UrlPathPrefix, apiUrlSuffix, operation, rootNavigation, parentNavigationItem);
				endpointNavigationItems.Add(operationNavigationItem);

			}
		}
	}

	public async Task Generate(Cancel ctx = default)
	{
		if (context.Configuration.OpenApiSpecifications is null)
			return;

		foreach (var (prefix, path) in context.Configuration.OpenApiSpecifications)
		{
			var openApiDocument = await OpenApiReader.Create(path);
			if (openApiDocument is null)
				return;

			var navigation = CreateNavigation(prefix, openApiDocument);
			_logger.LogInformation("Generating OpenApiDocument {Title}", openApiDocument.Info.Title);

			var navigationRenderer = new IsolatedBuildNavigationHtmlWriter(context, navigation);

			var renderContext = new ApiRenderContext(context, openApiDocument, _contentHashProvider)
			{
				NavigationHtml = string.Empty,
				CurrentNavigation = navigation,
				MarkdownRenderer = markdownStringRenderer
			};
			_ = await Render(prefix, navigation, navigation.Index, renderContext, navigationRenderer, ctx);
			await RenderNavigationItems(prefix, renderContext, navigationRenderer, navigation, ctx);

		}
	}

	private async Task RenderNavigationItems(string prefix, ApiRenderContext renderContext, IsolatedBuildNavigationHtmlWriter navigationRenderer, INavigationItem currentNavigation, Cancel ctx)
	{
		if (currentNavigation is INodeNavigationItem<IApiModel, INavigationItem> node)
		{
			_ = await Render(prefix, node, node.Index, renderContext, navigationRenderer, ctx);
			foreach (var child in node.NavigationItems)
				await RenderNavigationItems(prefix, renderContext, navigationRenderer, child, ctx);
		}

		else
		{
			_ = currentNavigation is ILeafNavigationItem<IApiModel> leaf
				? await Render(prefix, leaf, leaf.Model, renderContext, navigationRenderer, ctx)
				: throw new Exception($"Unknown navigation item type {currentNavigation.GetType()}");
		}
	}

#pragma warning disable IDE0060
	private async Task<IFileInfo> Render<T>(string prefix, INavigationItem current, T page, ApiRenderContext renderContext,
#pragma warning restore IDE0060
		IsolatedBuildNavigationHtmlWriter navigationRenderer, Cancel ctx)
		where T : INavigationModel, IPageRenderer<ApiRenderContext>
	{
		var outputFile = OutputFile(current);
		if (!outputFile.Directory!.Exists)
			outputFile.Directory.Create();

		var navigationRenderResult = await navigationRenderer.RenderNavigation(current.NavigationRoot, INavigationHtmlWriter.AllLevels, ctx);
		renderContext = renderContext with
		{
			CurrentNavigation = current,
			NavigationHtml = navigationRenderResult.Html
		};
		await using var stream = _writeFileSystem.FileStream.New(outputFile.FullName, FileMode.OpenOrCreate);
		await page.RenderAsync(stream, renderContext, ctx);
		return outputFile;

		IFileInfo OutputFile(INavigationItem currentNavigation)
		{
			const string indexHtml = "index.html";
			var fileName = Regex.Replace(currentNavigation.Url + "/" + indexHtml, $"^{context.UrlPathPrefix}", string.Empty);
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
