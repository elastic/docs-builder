// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Structural;
using Elastic.ApiExplorer.Supplemental;
using Elastic.ApiExplorer.Topics;
using Elastic.ApiExplorer.Types;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Navigation;

/// <summary>
/// Builds the navigation tree for one API product: tags, operations, and intro/outro markdown pages.
/// </summary>
public class ApiNavigationBuilder(ILogger logger, BuildContext context, IMarkdownStringRenderer markdownRenderer)
{
	private const string TagOnlyClassificationKey = "__api_explorer_tag_only__";
	private const string UnknownTagGroupName = "unknown";

	private readonly ILogger _logger = logger;

	public LandingNavigationItem CreateNavigation(
		string apiUrlSuffix,
		OpenApiDocument openApiDocument,
		ResolvedApiConfiguration? apiConfig = null,
		int? versionMajor = null
	)
	{
		var url = ApiUrlBuilder.ProductRoot(context.UrlPathPrefix, apiUrlSuffix);
		var rootNavigation = new LandingNavigationItem(url);

		var tagMetadataByName = OpenApiExtensionReader.ParseTagMetadata(openApiDocument);
		var xTagGroups = OpenApiExtensionReader.ParseXTagGroups(openApiDocument);
		var orphanTagsLogged = new HashSet<string>(StringComparer.Ordinal);

		var ops = openApiDocument
			.Paths
			.SelectMany(p => (p.Value.Operations ?? []).Select(op => (Path: p, Operation: op)))
			.Select(pair =>
			{
				var op = pair.Operation;
				var (ns, api) = OpenApiExtensionReader.GetNamespaceAndApiName(op.Value);
				var tag = op.Value.Tags?.FirstOrDefault()?.Reference.Id;
				var tagClassification = ResolveTagClassification(tag, xTagGroups, orphanTagsLogged);

				// Fall back to the operationId, then a deterministic route:method key; Guid.NewGuid() made grouping keys
				// (and thus navigation titles) change on every build.
				var apiString = ns is null
					? api ?? OpenApiExtensionReader.GetSummary(op.Value) ?? NullIfBlank(op.Value.OperationId) ?? $"{pair.Path.Key}:{op.Key}"
					: $"{ns}.{api}";
				return new { Classification = tagClassification, Api = apiString, Tag = tag, pair.Path, pair.Operation };
			})
			.ToArray();

		// Every page URL claimed so far: each operation's own, plus each shared page as it is created, so a
		// collapsed group never takes a URL that belongs to an operation outside it or to an earlier group.
		var takenMonikers = ops.Select(o => ApiUrlBuilder.OperationMoniker(o.Operation.Value.OperationId, o.Path.Key)).ToHashSet(
			StringComparer.Ordinal
		);

		var distinctTagNames = ops.Select(o => o.Tag ?? "unknown").Distinct().ToList();
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
					tagMeta = new OpenApiTagMetadata(tagName.CapitalizeFirstLetter(), string.Empty, null);
				if (!tagNameToUrlSegment.TryGetValue(tagName, out var urlSegment))
					throw new InvalidOperationException($"Internal error: no URL segment for OpenAPI tag '{tagName}'.");

				var tag = new ApiTag(tagName, tagMeta.DisplayName, tagMeta.Description, tagMeta.ExternalDocs, urlSegment, apis);
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

				CreateTagNavigationItems(
					apiUrlSuffix,
					classification,
					classificationNavigationItem,
					classificationNavigationItem,
					takenMonikers,
					tagNavigationItems
				);
				topLevelNavigationItems.Add(classificationNavigationItem);
				// if there is only a single tag item will be added directly to the classificationNavigationItem, otherwise they will be added to the tagNavigationItems
				if (classificationNavigationItem.NavigationItems.Count == 0)
					classificationNavigationItem.NavigationItems = tagNavigationItems;
			}
			else
				CreateTagNavigationItems(
					apiUrlSuffix,
					classification,
					rootNavigation,
					rootNavigation,
					takenMonikers,
					topLevelNavigationItems
				);
		}
		CreateSchemaNavigationItems(apiUrlSuffix, openApiDocument, rootNavigation, topLevelNavigationItems);

		// Topic pages sit under a Topics heading below the landing page and before the generated OpenAPI groups. The docset's
		// children: pages come first, in declared order. The spec's x-topics follow, except those a child page already covers.
		var finalNavigationItems = new List<INavigationItem>();
		var topics = OpenApiExtensionReader.ParseXTopics(openApiDocument);
		var children = ChildFiles(apiConfig, versionMajor);
		var coveredTopics = CoveredTopics(apiUrlSuffix, topics, children);
		var remainingTopics = topics.Where(topic => !coveredTopics.Contains(topic)).ToArray();
		var topicsNavigation = new TopicsNavigationItem(rootNavigation);
		topicsNavigation.NavigationItems =
		[
			.. CreateChildNavigationItems(apiUrlSuffix, children, topicsNavigation),
			.. TopicsNavigationItem.Order(CreateTopicNavigationItems(apiUrlSuffix, remainingTopics, topicsNavigation))
		];
		if (topicsNavigation.NavigationItems.Count > 0)
			finalNavigationItems.Add(topicsNavigation);

		// A spec topic titled Authentication replaces the generated Authentication page; the topic page still lists the schemes.
		var hasAuthenticationTopic = remainingTopics.Any(ApiTopicPage.IsAuthentication);
		finalNavigationItems.AddRange(
			StructuralNavigationItem.Create(context.UrlPathPrefix, apiUrlSuffix, rootNavigation, openApiDocument).Where(
				item => !(hasAuthenticationTopic && item.Model.Kind == ApiStructuralKind.Authentication)
			)
		);

		IReadOnlyList<INavigationItem> endpointItems = topLevelNavigationItems.Count > 0
			? topLevelNavigationItems
			: rootNavigation.NavigationItems.Count > 0 ? rootNavigation.NavigationItems.ToArray() : [];
		if (finalNavigationItems.Count > 0 && endpointItems.Count > 0)
			finalNavigationItems.Add(new SidebarSeparatorNavigationItem(rootNavigation, rootNavigation));
		if (endpointItems.Count > 0)
			finalNavigationItems.AddRange(endpointItems);

		rootNavigation.NavigationItems = finalNavigationItems;

		return rootNavigation;
	}

	private static IReadOnlyList<IFileInfo> ChildFiles(ResolvedApiConfiguration? apiConfig, int? versionMajor) =>
		apiConfig is null
			? []
			: [
				.. apiConfig.Children.Where(
					file => !ApiSupplementalName.TryParseVersionSuffix(file.Name, out _, out var fileMajor) || fileMajor == versionMajor
				)
			];

	private List<INavigationItem> CreateTopicNavigationItems(
		string apiUrlSuffix,
		IReadOnlyList<OpenApiTopic> topics,
		TopicsNavigationItem topicsNavigation
	)
	{
		var items = new List<INavigationItem>(topics.Count);
		foreach (var topic in topics)
		{
			if (ApiTopicPage.IsAuthentication(topic))
			{
				var authentication = StructuralNavigationItem.ForAuthenticationTopic(
					context.UrlPathPrefix,
					apiUrlSuffix,
					topicsNavigation.Landing,
					topic
				);
				authentication.Parent = topicsNavigation;
				items.Add(authentication);
				continue;
			}

			items.Add(
				new TopicNavigationItem(
					TopicUrl(apiUrlSuffix, topic),
					new ApiTopicPage(topic.Title, topic.Content),
					topicsNavigation.Landing,
					topicsNavigation
				)
			);
		}

		return items;
	}

	private List<INavigationItem> CreateChildNavigationItems(
		string apiUrlSuffix,
		IReadOnlyList<IFileInfo> children,
		TopicsNavigationItem topicsNavigation
	)
	{
		var markdownSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		return [
			.. children.Select(
				child => CreateMarkdownNavigationItem(apiUrlSuffix, child, topicsNavigation.Landing, topicsNavigation, markdownSlugs)
			)
		];
	}

	/// <summary>
	/// The spec topics that a <c>children:</c> page already covers, because the child's first heading matches the topic title
	/// or its text matches the topic content. The topic is not built; elastic/docs-infra redirects its bump.sh URL to the child.
	/// </summary>
	private HashSet<OpenApiTopic> CoveredTopics(string apiUrlSuffix, IReadOnlyList<OpenApiTopic> topics, IReadOnlyList<IFileInfo> children)
	{
		var covered = new HashSet<OpenApiTopic>();
		if (topics.Count == 0 || children.Count == 0)
			return covered;

		var childPages = children
			.Select(child => (File: child, Page: MarkdownNavigationTitleReader.ReadHeadingAndBody(context.ReadFileSystem, child)))
			.Where(child => child.Page.Heading is not null)
			.ToArray();
		foreach (var topic in topics)
		{
			var match = childPages.FirstOrDefault(child => Covers(child.Page, topic)).File;
			if (match is null)
				continue;

			_logger.LogInformation(
				"API '{ApiKey}' builds no page for x-topics entry '{Title}', because the children: page '{File}' covers it.",
				apiUrlSuffix,
				topic.Title,
				match.Name
			);
			_ = covered.Add(topic);
		}

		return covered;
	}

	private static bool Covers((string? Heading, string? Body) child, OpenApiTopic topic) =>
		string.Equals(child.Heading, topic.Title, StringComparison.OrdinalIgnoreCase)
			|| (!string.IsNullOrWhiteSpace(topic.Content) && CollapseWhitespace(child.Body) == CollapseWhitespace(topic.Content));

	private static string CollapseWhitespace(string? text) =>
		string.Join(' ', (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

	private string TopicUrl(string apiUrlSuffix, OpenApiTopic topic) =>
		ApiUrlBuilder.TopicUrl(context.UrlPathPrefix, apiUrlSuffix, ApiUrlBuilder.TopicMoniker(topic.Title));

	private string ChildUrl(string apiUrlSuffix, string slug) =>
		$"{ApiUrlBuilder.ProductRoot(context.UrlPathPrefix, apiUrlSuffix)}/{slug}/";

	private SimpleMarkdownNavigationItem CreateMarkdownNavigationItem(
		string apiUrlSuffix,
		IFileInfo markdownFile,
		LandingNavigationItem rootNavigation,
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		HashSet<string> markdownSlugs
	)
	{
		var slug = SimpleMarkdownNavigationItem.CreateSlugFromFile(markdownFile);

		// Check for duplicate markdown slugs
		if (!markdownSlugs.Add(slug))
		{
			throw new InvalidOperationException(
				$"Duplicate markdown slug '{slug}' found in API product '{apiUrlSuffix}'. " + $"File: {markdownFile.FullName}"
			);
		}

		SimpleMarkdownNavigationItem.ValidateSlugForCollisions(slug, apiUrlSuffix, markdownFile.FullName);

		var url = ChildUrl(apiUrlSuffix, slug);
		var title = MarkdownNavigationTitleReader.GetNavigationTitle(context.ReadFileSystem, markdownFile);

		// Create simple navigation item - will be handled by regular documentation system
		var navItem = new SimpleMarkdownNavigationItem(url, title, markdownFile, rootNavigation) { Parent = parent };

		return navItem;
	}

	private void CreateTagNavigationItems(
		string apiUrlSuffix,
		ApiClassification classification,
		IRootNavigationItem<IApiGroupingModel, INavigationItem> rootNavigation,
		IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parent,
		HashSet<string> takenMonikers,
		List<IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem>> parentNavigationItems
	)
	{
		foreach (var tag in classification.Tags)
		{
			var endpointNavigationItems = new List<IEndpointOrOperationNavigationItem>();
			var tagNavigationItem = new TagNavigationItem(tag, context.UrlPathPrefix, apiUrlSuffix, rootNavigation, parent);
			CreateEndpointNavigationItems(apiUrlSuffix, rootNavigation, tag, tagNavigationItem, takenMonikers, endpointNavigationItems);
			parentNavigationItems.Add(tagNavigationItem);
			tagNavigationItem.NavigationItems = endpointNavigationItems;
			tagNavigationItem.ApplyIntroHeadings(ApiMarkdown.ReadIntroHeadings(
				markdownRenderer,
				tag.Description,
				rootNavigation.Url,
				postSections: []
			));
		}
	}

	private void CreateEndpointNavigationItems(
		string apiUrlSuffix,
		IRootNavigationItem<IApiGroupingModel, INavigationItem> rootNavigation,
		ApiTag tag,
		IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parentNavigationItem,
		HashSet<string> takenMonikers,
		List<IEndpointOrOperationNavigationItem> endpointNavigationItems
	)
	{
		foreach (var endpoint in tag.Endpoints)
		{
			// One page can only stand in for several operations when they are the same call; a sibling with
			// its own parameters or responses keeps its own page rather than redirecting to a page that lacks them.
			var differences = endpoint.Operations.Count > 1 ? OperationEndpoint.Differences(endpoint.Operations) : [];
			if (endpoint.Operations.Count > 1 && differences.Count == 0)
			{
				endpointNavigationItems.Add(
					CreateCollapsedOperationNavigationItem(apiUrlSuffix, rootNavigation, endpoint, parentNavigationItem, takenMonikers)
				);
			}
			else
			{
				// Siblings of one API are meant to be the same call; a difference is worth a look at the spec.
				if (differences.Count > 0)
				{
					_logger.LogWarning(
						"Operations {Operations} of API '{Api}' differ in {Facets}; each keeps its own page instead of sharing one.",
						string.Join(", ", endpoint.Operations.Select(static o => $"{o.OperationType.Method.ToUpperInvariant()} {o.Route}")),
						endpoint.Name,
						string.Join(", ", differences)
					);
				}
				foreach (var operation in endpoint.Operations)
				{
					var operationNavigationItem = new OperationNavigationItem(
						context.UrlPathPrefix,
						apiUrlSuffix,
						operation,
						rootNavigation,
						parentNavigationItem
					);
					endpointNavigationItems.Add(operationNavigationItem);
				}
			}
		}
	}

	/// <summary>
	/// Every method and route of one API renders as a single page built around the primary operation.
	/// The other operations' former URLs become redirects.
	/// </summary>
	private OperationNavigationItem CreateCollapsedOperationNavigationItem(
		string apiUrlSuffix,
		IRootNavigationItem<IApiGroupingModel, INavigationItem> rootNavigation,
		ApiEndpoint endpoint,
		IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parentNavigationItem,
		HashSet<string> takenMonikers
	)
	{
		var primary = OperationEndpoint.SelectPrimary(endpoint.Operations);
		var moniker = ApiUrlBuilder.CanonicalOperationMoniker(endpoint.Operations, primary, takenMonikers);
		var preferred = ApiUrlBuilder.CanonicalOperationMoniker(endpoint.Operations, primary);
		if (moniker != preferred)
		{
			_logger.LogWarning(
				"The shared page for API '{Api}' would be '{Preferred}', but another page already owns that URL; it is '{Moniker}' instead.",
				endpoint.Name,
				preferred,
				moniker
			);
		}
		_ = takenMonikers.Add(moniker);
		var url = ApiUrlBuilder.OperationUrl(context.UrlPathPrefix, apiUrlSuffix, moniker);
		var aliases = endpoint
			.Operations
			.Select(
				o => ApiUrlBuilder.OperationUrl(
					context.UrlPathPrefix,
					apiUrlSuffix,
					ApiUrlBuilder.OperationMoniker(o.Operation.OperationId, o.Route)
				)
			)
			.Where(alias => alias != url)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		return new OperationNavigationItem(context.UrlPathPrefix, apiUrlSuffix, primary, rootNavigation, parentNavigationItem, moniker)
		{
			Siblings = [.. endpoint.Operations.Where(o => !ReferenceEquals(o, primary))],
			AliasUrls = aliases
		};
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

		// One category per group of type pages (Query DSL, Aggregations, Ingest), in the order TypePages lists them.
		foreach (var category in TypePages.All.GroupBy(static p => p.Category))
		{
			var present = category.Where(p => schemas.ContainsKey(p.SchemaId)).ToArray();
			if (present.Length == 0)
				continue;

			var categoryNav = new SchemaCategoryNavigationItem(
				new SchemaCategory(category.Key.Title, category.Key.Description),
				rootNavigation,
				typesCategoryNav
			);
			categoryNav.NavigationItems =
			[
				.. present.Select(
					page => new SchemaNavigationItem(
						context.UrlPathPrefix,
						apiUrlSuffix,
						new ApiSchema(page.SchemaId, page.Title, category.Key.Slug, schemas[page.SchemaId]),
						rootNavigation,
						categoryNav
					)
				)
			];
			categoryNavigationItems.Add(categoryNav);
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
				UnknownTagGroupName
			);
		}

		return UnknownTagGroupName;
	}

	// The raw operationId stays the grouping key: humanized titles of distinct ids (get-script, get_script) can collide.
	private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

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
						"Rename one of the tag names in the spec."
				);
			}

			segmentToTagName[segment] = name;
			toSegment[name] = segment;
		}

		return toSegment;
	}
}
