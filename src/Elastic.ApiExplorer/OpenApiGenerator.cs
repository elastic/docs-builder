// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO.Abstractions;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Schemas;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer;

public interface IApiModel : INavigationModel, IPageRenderer<ApiRenderContext>;

public interface IApiGroupingModel : IApiModel;

public record ApiClassification(string Name, string Description, IReadOnlyCollection<ApiTag> Tags) : IApiGroupingModel
{
	/// <inheritdoc />
	public Task RenderAsync(FileSystemStream stream, ApiRenderContext context, CancellationToken ctx = default) => Task.CompletedTask;
}

public record ApiTag(string Name, string DisplayName, string Description, IReadOnlyCollection<ApiEndpoint> Endpoints) : IApiGroupingModel
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
	private const string TagOnlyClassificationKey = "__api_explorer_tag_only__";
	private const string UnknownTagGroupName = "unknown";

	private readonly ILogger _logger = logFactory.CreateLogger<OpenApiGenerator>();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;
	private readonly StaticFileContentHashProvider _contentHashProvider = new(new EmbeddedOrPhysicalFileProvider(context));

	public LandingNavigationItem CreateNavigation(string apiUrlSuffix, OpenApiDocument openApiDocument, ResolvedApiConfiguration? apiConfig = null)
	{
		var url = $"{context.UrlPathPrefix}/api/" + apiUrlSuffix;
		var rootNavigation = new LandingNavigationItem(url);

		// Parse x-displayName from OpenAPI tags for user-friendly display names
		var tagDisplayNames = ParseTagDisplayNames(openApiDocument);
		var xTagGroups = TryParseXTagGroups(openApiDocument);
		var orphanTagsLogged = new HashSet<string>(StringComparer.Ordinal);

		var ops = openApiDocument.Paths
			.SelectMany(p => (p.Value.Operations ?? []).Select(op => (Path: p, Operation: op)))
			.Select(pair =>
			{
				var op = pair.Operation;
				var extensions = op.Value.Extensions;
				var ns = (extensions?.TryGetValue("x-namespace", out var n) ?? false) && n is JsonNodeExtension anyNs
					? anyNs.Node.GetValue<string>()
					: null;
				var api = (extensions?.TryGetValue("x-api-name", out var a) ?? false) && a is JsonNodeExtension anyApi
					? anyApi.Node.GetValue<string>()
					: null;
				var tag = op.Value.Tags?.FirstOrDefault()?.Reference.Id;
				var tagClassification = ResolveTagClassification(tag, xTagGroups, orphanTagsLogged);

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

		// intermediate grouping of models to create the navigation tree
		// this is two-phased because we need to know if an endpoint has one or more operations
		var presentClassifications = ops.Select(o => o.Classification).ToHashSet();
		var orderedClassificationKeys = xTagGroups is null
			? [TagOnlyClassificationKey]
			: GetOrderedClassificationKeys(xTagGroups, presentClassifications);

		var classifications = new List<ApiClassification>();
		foreach (var classKey in orderedClassificationKeys)
		{
			var classOps = ops.Where(o => o.Classification == classKey).ToArray();
			if (classOps.Length == 0)
				continue;

			var tags = new List<ApiTag>();
			foreach (var tagGroup in classOps.GroupBy(o => o.Tag))
			{
				var apis = new List<ApiEndpoint>();
				foreach (var apiGroup in tagGroup.GroupBy(o => o.Api))
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
				var tagName = tagGroup.Key ?? "unknown";
				var displayName = tagDisplayNames.TryGetValue(tagName, out var foundDisplayName) ? foundDisplayName : tagName;
				var tag = new ApiTag(tagName, displayName, "", apis);
				tags.Add(tag);
			}

			// Sort tags alphabetically by display name, fallback to canonical name
			tags = tags.OrderBy(t => t.DisplayName ?? t.Name, StringComparer.OrdinalIgnoreCase).ToList();

			classifications.Add(new ApiClassification(classKey, "", tags));
		}

		var topLevelNavigationItems = new List<IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem>>();
		var hasClassifications = classifications.Count > 1;
		foreach (var classification in classifications)
		{
			if (hasClassifications)
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
		// Add schema type pages for shared types
		CreateSchemaNavigationItems(apiUrlSuffix, openApiDocument, rootNavigation, topLevelNavigationItems);

		// Collect operation monikers for collision detection
		var operationMonikers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var path in openApiDocument.Paths)
		{
			foreach (var operation in path.Value.Operations ?? [])
			{
				// Use same moniker logic as OperationNavigationItem
				var moniker = !string.IsNullOrWhiteSpace(operation.Value.OperationId)
					? operation.Value.OperationId
					: path.Key.Replace("}", "").Replace("{", "").Replace('/', '-');
				_ = operationMonikers.Add(moniker);
			}
		}

		// Add intro and outro markdown pages if available
		var finalNavigationItems = new List<INavigationItem>();
		var markdownSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Add intro pages first
		if (apiConfig?.IntroMarkdownFiles.Count > 0)
		{
			foreach (var introFile in apiConfig.IntroMarkdownFiles)
			{
				var introNavItem = CreateMarkdownNavigationItem(apiUrlSuffix, introFile, rootNavigation, rootNavigation, operationMonikers, markdownSlugs);
				finalNavigationItems.Add(introNavItem);
			}
		}

		// Add existing navigation items (OpenAPI generated content)
		if (topLevelNavigationItems.Count > 0)
			finalNavigationItems.AddRange(topLevelNavigationItems);
		else if (rootNavigation.NavigationItems.Count > 0)
			finalNavigationItems.AddRange(rootNavigation.NavigationItems);

		// Add outro pages last
		if (apiConfig?.OutroMarkdownFiles.Count > 0)
		{
			foreach (var outroFile in apiConfig.OutroMarkdownFiles)
			{
				var outroNavItem = CreateMarkdownNavigationItem(apiUrlSuffix, outroFile, rootNavigation, rootNavigation, operationMonikers, markdownSlugs);
				finalNavigationItems.Add(outroNavItem);
			}
		}

		// Set the final navigation items
		if (finalNavigationItems.Count > 0)
			rootNavigation.NavigationItems = finalNavigationItems;

		return rootNavigation;
	}

	private SimpleMarkdownNavigationItem CreateMarkdownNavigationItem(
		string apiUrlSuffix,
		IFileInfo markdownFile,
		LandingNavigationItem rootNavigation,
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		HashSet<string> operationMonikers,
		HashSet<string> markdownSlugs)
	{
		var slug = SimpleMarkdownNavigationItem.CreateSlugFromFile(markdownFile);

		// Check for duplicate markdown slugs
		if (!markdownSlugs.Add(slug))
		{
			throw new InvalidOperationException(
				$"Duplicate markdown slug '{slug}' found in API product '{apiUrlSuffix}'. " +
				$"File: {markdownFile.FullName}");
		}

		SimpleMarkdownNavigationItem.ValidateSlugForCollisions(slug, apiUrlSuffix, markdownFile.FullName, operationMonikers);

		var url = $"{context.UrlPathPrefix}/api/{apiUrlSuffix}/{slug}/";
		var title = GetNavigationTitleFromFile(markdownFile);

		// Create simple navigation item - will be handled by regular documentation system
		var navItem = new SimpleMarkdownNavigationItem(url, title, markdownFile, rootNavigation)
		{
			Parent = parent
		};

		return navItem;
	}

	private string GetNavigationTitleFromFile(IFileInfo markdownFile)
	{
		try
		{
			// Read file content to parse frontmatter
			var content = context.ReadFileSystem.File.ReadAllText(markdownFile.FullName);

			// Simple frontmatter parsing - look for navigation_title
			if (content.StartsWith("---"))
			{
				var lines = content.Split('\n');
				var frontMatterEndIndex = -1;

				for (var i = 1; i < lines.Length; i++)
				{
					if (lines[i].TrimStart().StartsWith("---"))
					{
						frontMatterEndIndex = i;
						break;
					}
				}

				if (frontMatterEndIndex > 0)
				{
					for (var i = 1; i < frontMatterEndIndex; i++)
					{
						var line = lines[i].Trim();
						if (line.StartsWith("navigation_title:"))
						{
							var title = line.Substring("navigation_title:".Length).Trim();
							// Remove quotes if present
							if ((title.StartsWith('"') && title.EndsWith('"')) ||
								(title.StartsWith('\'') && title.EndsWith('\'')))
							{
								title = title.Substring(1, title.Length - 2);
							}
							if (!string.IsNullOrEmpty(title))
							{
								return title;
							}
						}
					}
				}
			}
		}
		catch (Exception)
		{
			// Fall back to filename-based title if parsing fails
		}

		// Fallback: Extract a friendly navigation title from the filename
		var fileName = Path.GetFileNameWithoutExtension(markdownFile.Name);

		// Convert kebab-case/snake_case to title case
		return fileName
			.Replace('-', ' ')
			.Replace('_', ' ')
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(word => char.ToUpper(word[0]) + word[1..].ToLower())
			.Aggregate((current, next) => $"{current} {next}");
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
		// Use the new API configurations if available, otherwise fall back to legacy OpenApiSpecifications
		if (context.Configuration.ApiConfigurations is not null)
		{
			foreach (var (prefix, apiConfig) in context.Configuration.ApiConfigurations)
			{
				// Validate assumption of single spec per product
				if (apiConfig.SpecFiles.Count > 1)
					throw new InvalidOperationException($"API product '{prefix}' has {apiConfig.SpecFiles.Count} spec files, but only one spec file per product is currently supported.");

				var openApiDocument = await OpenApiReader.Create(apiConfig.PrimarySpecFile);
				if (openApiDocument is null)
					continue;

				await GenerateApiProduct(prefix, openApiDocument, apiConfig, ctx);
			}
		}
		else if (context.Configuration.OpenApiSpecifications is not null)
		{
			// Legacy fallback
			foreach (var (prefix, path) in context.Configuration.OpenApiSpecifications)
			{
				var openApiDocument = await OpenApiReader.Create(path);
				if (openApiDocument is null)
					continue;

				await GenerateApiProduct(prefix, openApiDocument, null, ctx);
			}
		}
	}

	private async Task GenerateApiProduct(string prefix, OpenApiDocument openApiDocument, ResolvedApiConfiguration? apiConfig, Cancel ctx)
	{
		var navigation = CreateNavigation(prefix, openApiDocument, apiConfig);
		_logger.LogInformation("Generating OpenApiDocument {Title}", openApiDocument.Info?.Title ?? "<no title>");

		var navigationRenderer = new IsolatedBuildNavigationHtmlWriter(context, navigation);

		var renderContext = new ApiRenderContext(context, openApiDocument, _contentHashProvider)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = navigation,
			MarkdownRenderer = markdownStringRenderer,
			ApiExplorerLog = _logger
		};

		await RenderNavigationItems(prefix, renderContext, navigationRenderer, navigation, navigation, ctx);
	}

	private async Task RenderNavigationItems(
		string prefix,
		ApiRenderContext renderContext,
		IsolatedBuildNavigationHtmlWriter navigationRenderer,
		INavigationItem currentNavigation,
		INavigationItem rootNavigation,
		Cancel ctx)
	{
		if (currentNavigation is INodeNavigationItem<IApiModel, INavigationItem> node)
		{
			_ = await Render(prefix, node, node.Index.Model, renderContext, navigationRenderer, ctx);

			foreach (var child in node.NavigationItems)
				await RenderNavigationItems(prefix, renderContext, navigationRenderer, child, rootNavigation, ctx);
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

		var navigationRenderResult = await navigationRenderer.RenderNavigation(current.NavigationRoot, current, ctx);
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
			var fileInfo = _writeFileSystem.FileInfo.New(Path.Join(context.OutputDirectory.FullName, fileName.Trim('/')));
			return fileInfo;
		}
	}

	private void CreateSchemaNavigationItems(
		string apiUrlSuffix,
		OpenApiDocument openApiDocument,
		LandingNavigationItem rootNavigation,
		List<IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem>> topLevelNavigationItems
	)
	{
		var schemas = openApiDocument.Components?.Schemas;
		if (schemas is null || schemas.Count == 0)
			return;

		var typesCategory = new SchemaCategory("Types", "Shared type definitions");
		var typesCategoryNav = new SchemaCategoryNavigationItem(typesCategory, rootNavigation, rootNavigation);
		var categoryNavigationItems = new List<IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem>>();

		// Query DSL - only show QueryContainer (individual queries are shown as properties within it)
		var queryContainerSchema = schemas
			.FirstOrDefault(s => s.Key == "_types.query_dsl.QueryContainer");

		if (queryContainerSchema.Value is not null)
		{
			var queryCategory = new SchemaCategory("Query DSL", "Query type definitions");
			var queryCategoryNav = new SchemaCategoryNavigationItem(queryCategory, rootNavigation, typesCategoryNav);
			var apiSchema = new ApiSchema(queryContainerSchema.Key, "QueryContainer", "query-dsl", queryContainerSchema.Value);
			var queryNavigationItems = new List<INavigationItem>
			{
				new SchemaNavigationItem(context.UrlPathPrefix, apiUrlSuffix, apiSchema, rootNavigation, queryCategoryNav)
			};
			queryCategoryNav.NavigationItems = queryNavigationItems;
			categoryNavigationItems.Add(queryCategoryNav);
		}

		// Aggregations - only show AggregationContainer and Aggregate
		var aggContainerSchema = schemas
			.FirstOrDefault(s => s.Key == "_types.aggregations.AggregationContainer");

		var aggregateSchema = schemas
			.FirstOrDefault(s => s.Key == "_types.aggregations.Aggregate");

		if (aggContainerSchema.Value is not null || aggregateSchema.Value is not null)
		{
			var aggCategory = new SchemaCategory("Aggregations", "Aggregation type definitions");
			var aggCategoryNav = new SchemaCategoryNavigationItem(aggCategory, rootNavigation, typesCategoryNav);
			var aggNavigationItems = new List<INavigationItem>();

			if (aggContainerSchema.Value is not null)
			{
				var apiSchema = new ApiSchema(aggContainerSchema.Key, "AggregationContainer", "aggregations", aggContainerSchema.Value);
				aggNavigationItems.Add(new SchemaNavigationItem(context.UrlPathPrefix, apiUrlSuffix, apiSchema, rootNavigation, aggCategoryNav));
			}

			if (aggregateSchema.Value is not null)
			{
				var apiSchema = new ApiSchema(aggregateSchema.Key, "Aggregate", "aggregations", aggregateSchema.Value);
				aggNavigationItems.Add(new SchemaNavigationItem(context.UrlPathPrefix, apiUrlSuffix, apiSchema, rootNavigation, aggCategoryNav));
			}

			aggCategoryNav.NavigationItems = aggNavigationItems;
			categoryNavigationItems.Add(aggCategoryNav);
		}

		if (categoryNavigationItems.Count > 0)
		{
			typesCategoryNav.NavigationItems = categoryNavigationItems;
			topLevelNavigationItems.Add(typesCategoryNav);
		}
	}

	private static string FormatSchemaDisplayName(string schemaId)
	{
		// Convert schema IDs like "_types.query_dsl.QueryContainer" to "QueryContainer"
		var parts = schemaId.Split('.');
		return parts.Length > 0 ? parts[^1] : schemaId;
	}

	private string ResolveTagClassification(string? tag, XTagGroupsDocument? xTagGroups, HashSet<string> orphanTagsLogged)
	{
		if (xTagGroups is null)
			return TagOnlyClassificationKey;

		var tagName = tag ?? UnknownTagGroupName;
		if (xTagGroups.TagToGroup.TryGetValue(tagName, out var group))
			return group;

		if (orphanTagsLogged.Add(tagName))
		{
			_logger.LogWarning(
				"OpenAPI tag '{TagName}' is not listed in any x-tagGroups entry; navigation will group it under '{UnknownGroup}'.",
				tagName,
				UnknownTagGroupName);
		}

		return UnknownTagGroupName;
	}

	private static List<string> GetOrderedClassificationKeys(XTagGroupsDocument xTagGroups, HashSet<string> presentClassifications)
	{
		var ordered = new List<string>();
		foreach (var g in xTagGroups.OrderedGroupNames)
		{
			if (presentClassifications.Contains(g))
				ordered.Add(g);
		}

		if (presentClassifications.Contains(UnknownTagGroupName) && !ordered.Contains(UnknownTagGroupName))
			ordered.Add(UnknownTagGroupName);

		foreach (var c in presentClassifications)
		{
			if (!ordered.Contains(c))
				ordered.Add(c);
		}

		return ordered;
	}

	private static XTagGroupsDocument? TryParseXTagGroups(OpenApiDocument openApiDocument)
	{
		if (openApiDocument.Extensions?.TryGetValue("x-tagGroups", out var extension) != true || extension is not JsonNodeExtension jsonExt)
			return null;

		if (jsonExt.Node is not JsonArray array || array.Count == 0)
			return null;

		var orderedGroupNames = new List<string>();
		var tagToGroup = new Dictionary<string, string>(StringComparer.Ordinal);

		foreach (var element in array)
		{
			if (element is not JsonObject groupObj)
				continue;

			if (!groupObj.TryGetPropertyValue("name", out var nameNode))
				continue;

			var groupName = nameNode?.GetValue<string>();
			if (string.IsNullOrWhiteSpace(groupName))
				continue;

			if (!orderedGroupNames.Contains(groupName))
				orderedGroupNames.Add(groupName);

			if (!groupObj.TryGetPropertyValue("tags", out var tagsNode) || tagsNode is not JsonArray tagNames)
				continue;

			foreach (var tagElement in tagNames)
			{
				var tagName = tagElement?.GetValue<string>();
				if (string.IsNullOrEmpty(tagName))
					continue;

				if (!tagToGroup.ContainsKey(tagName))
					tagToGroup[tagName] = groupName;
			}
		}

		if (orderedGroupNames.Count == 0 || tagToGroup.Count == 0)
			return null;

		return new XTagGroupsDocument(orderedGroupNames, tagToGroup);
	}

	private sealed record XTagGroupsDocument(IReadOnlyList<string> OrderedGroupNames, IReadOnlyDictionary<string, string> TagToGroup);

	/// <summary>
	/// Parses x-displayName extensions from OpenAPI tag objects to build a mapping of tag names to display names.
	/// Falls back to the canonical tag name when no x-displayName is present.
	/// </summary>
	private static Dictionary<string, string> ParseTagDisplayNames(OpenApiDocument openApiDocument)
	{
		var displayNames = new Dictionary<string, string>();

		if (openApiDocument.Tags is null)
			return displayNames;

		foreach (var tag in openApiDocument.Tags)
		{
			var tagName = tag.Name;
			if (string.IsNullOrEmpty(tagName))
				continue;

			var displayName = tagName; // Default fallback

			// Look for x-displayName extension
			if (tag.Extensions?.TryGetValue("x-displayName", out var extension) == true &&
				extension is JsonNodeExtension jsonExtension)
			{
				var displayNameValue = jsonExtension.Node.GetValue<string>();
				if (!string.IsNullOrWhiteSpace(displayNameValue))
				{
					displayName = displayNameValue;
				}
			}

			displayNames[tagName] = displayName;
		}

		return displayNames;
	}
}
