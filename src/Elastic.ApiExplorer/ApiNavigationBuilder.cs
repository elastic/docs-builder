// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Schemas;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer;

/// <summary>
/// Builds the navigation tree for one API product: classifications from x-tagGroups, tags,
/// endpoints grouped by x-namespace/x-api-name, schema type pages and intro/outro markdown pages.
/// </summary>
public class ApiNavigationBuilder(ILogger logger, BuildContext context)
{
	private const string TagOnlyClassificationKey = "__api_explorer_tag_only__";
	private const string UnknownTagGroupName = "unknown";

	private readonly ILogger _logger = logger;

	public LandingNavigationItem CreateNavigation(string apiUrlSuffix, OpenApiDocument openApiDocument, ResolvedApiConfiguration? apiConfig = null)
	{
		var url = $"{context.UrlPathPrefix}/api/" + apiUrlSuffix;
		var rootNavigation = new LandingNavigationItem(url);

		var tagMetadataByName = OpenApiExtensionReader.ParseTagMetadata(openApiDocument);
		var xTagGroups = OpenApiExtensionReader.ParseXTagGroups(openApiDocument);
		var orphanTagsLogged = new HashSet<string>(StringComparer.Ordinal);

		var ops = openApiDocument.Paths
			.SelectMany(p => (p.Value.Operations ?? []).Select(op => (Path: p, Operation: op)))
			.Select(pair =>
			{
				var op = pair.Operation;
				var (ns, api) = OpenApiExtensionReader.GetNamespaceAndApiName(op.Value);
				var tag = op.Value.Tags?.FirstOrDefault()?.Reference.Id;
				var tagClassification = ResolveTagClassification(tag, xTagGroups, orphanTagsLogged);

				// Fall back to a deterministic route:method key; Guid.NewGuid() made grouping keys
				// (and thus navigation titles) change on every build.
				var apiString = ns is null
					? api ?? op.Value.Summary ?? $"{pair.Path.Key}:{op.Key}" : $"{ns}.{api}";
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

		var distinctTagNames = ops
			.Select(o => o.Tag ?? "unknown")
			.Distinct()
			.ToList();
		var tagNameToUrlSegment = BuildTagMonikerMap(distinctTagNames);

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
				if (!tagMetadataByName.TryGetValue(tagName, out var tagMeta))
					tagMeta = new OpenApiTagMetadata(tagName, string.Empty, null);
				if (!tagNameToUrlSegment.TryGetValue(tagName, out var urlSegment))
					throw new InvalidOperationException(
						$"Internal error: no URL segment for OpenAPI tag '{tagName}'.");

				var tag = new ApiTag(
					tagName,
					tagMeta.DisplayName,
					tagMeta.Description,
					tagMeta.ExternalDocs,
					urlSegment,
					apis);
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
				_ = operationMonikers.Add(ApiUrlBuilder.OperationMoniker(operation.Value.OperationId, path.Key));
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
		var title = MarkdownNavigationTitleReader.GetNavigationTitle(context.ReadFileSystem, markdownFile);

		// Create simple navigation item - will be handled by regular documentation system
		var navItem = new SimpleMarkdownNavigationItem(url, title, markdownFile, rootNavigation)
		{
			Parent = parent
		};

		return navItem;
	}

	private void CreateTagNavigationItems(
		string apiUrlSuffix,
		ApiClassification classification,
		IRootNavigationItem<IApiGroupingModel, INavigationItem> rootNavigation,
		IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parent,
		List<IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem>> parentNavigationItems
	)
	{
		foreach (var tag in classification.Tags)
		{
			var endpointNavigationItems = new List<IEndpointOrOperationNavigationItem>();
			var tagNavigationItem = new TagNavigationItem(tag, context.UrlPathPrefix, apiUrlSuffix, rootNavigation, parent);
			CreateEndpointNavigationItems(apiUrlSuffix, rootNavigation, tag, tagNavigationItem, endpointNavigationItems);
			parentNavigationItems.Add(tagNavigationItem);
			tagNavigationItem.NavigationItems = endpointNavigationItems;
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
	private string ResolveTagClassification(string? tag, XTagGroups? xTagGroups, HashSet<string> orphanTagsLogged)
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

	private static List<string> GetOrderedClassificationKeys(XTagGroups xTagGroups, HashSet<string> presentClassifications)
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

	private static IReadOnlyDictionary<string, string> BuildTagMonikerMap(IReadOnlyList<string> distinctTagNames)
	{
		var toSegment = new Dictionary<string, string>(StringComparer.Ordinal);
		var segmentToTagName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var name in distinctTagNames)
		{
			var segment = ApiUrlBuilder.TagMoniker(name);
			if (segmentToTagName.TryGetValue(segment, out var existing) && !string.Equals(existing, name, StringComparison.Ordinal))
			{
				throw new InvalidOperationException(
					$"OpenAPI tag URL segment conflict: tags '{existing}' and '{name}' both normalize to the same path segment '{segment}'. " +
					"Rename one of the tag names in the spec.");
			}

			segmentToTagName[segment] = name;
			toSegment[name] = segment;
		}

		return toSegment;
	}
}
