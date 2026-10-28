// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Navigation;
using Elastic.ApiExplorer.Structural;
using Elastic.ApiExplorer.Supplemental;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Topics;

/// <summary>
/// Builds the Topics heading of one API product, below the landing page and before the generated OpenAPI groups: the docset's
/// <c>children:</c> pages first, in declared order, then the spec's <c>x-topics</c> that no child covers.
/// </summary>
public class ApiTopicsNavigationBuilder(ILogger logger, BuildContext context)
{
	/// <summary>The docset's <c>children:</c> files for one version: unsuffixed files, and <c>.vN.md</c> files of that major.</summary>
	public static IReadOnlyList<IFileInfo> ChildFiles(ResolvedApiConfiguration? apiConfig, int? versionMajor) =>
		apiConfig is null
			? []
			: [
				.. apiConfig.Children.Where(
					file => !ApiSupplementalName.TryParseVersionSuffix(file.Name, out _, out var fileMajor) || fileMajor == versionMajor
				)
			];

	/// <summary>The Topics heading, and whether a spec topic titled Authentication takes over the generated Authentication page.</summary>
	public (TopicsNavigationItem Topics, bool HasAuthenticationTopic) Create(
		string apiUrlSuffix,
		OpenApiDocument openApiDocument,
		IReadOnlyList<IFileInfo> children,
		LandingNavigationItem rootNavigation
	)
	{
		var topics = OpenApiExtensionReader.ParseXTopics(openApiDocument);
		var hasSchemes = StructuralViewModel.HasSchemes(openApiDocument);
		var coveredTopics = CoveredTopics(apiUrlSuffix, topics, children, hasSchemes: hasSchemes);
		var remainingTopics = topics.Where(topic => !coveredTopics.ContainsKey(topic)).ToArray();
		var topicsNavigation = new TopicsNavigationItem(rootNavigation);
		topicsNavigation.NavigationItems =
		[
			.. CreateChildNavigationItems(
				apiUrlSuffix,
				children,
				ReplacedTopicNames(apiUrlSuffix, coveredTopics, hasSchemes: hasSchemes),
				topicsNavigation
			),
			.. TopicsNavigationItem.Order(CreateTopicNavigationItems(apiUrlSuffix, remainingTopics, topicsNavigation))
		];
		return (topicsNavigation, remainingTopics.Any(static topic => topic.IsAuthentication));
	}

	private List<INavigationItem> CreateTopicNavigationItems(
		string apiUrlSuffix,
		IReadOnlyList<ApiTopicPage> topics,
		TopicsNavigationItem topicsNavigation
	)
	{
		var items = new List<INavigationItem>(topics.Count);
		var urls = new HashSet<string>(StringComparer.Ordinal);
		foreach (var topic in topics)
		{
			var url = topic.IsAuthentication
				? ApiUrlBuilder.AuthenticationUrl(context.UrlPathPrefix, apiUrlSuffix)
				: ApiUrlBuilder.TopicUrl(context.UrlPathPrefix, apiUrlSuffix, topic.Title);
			if (!urls.Add(url))
			{
				logger.LogWarning(
					"API '{ApiKey}' skips x-topics entry '{Title}', because an earlier entry already builds the page {Url}. Give each topic a distinct title.",
					apiUrlSuffix,
					topic.Title,
					url
				);
				continue;
			}

			if (topic.IsAuthentication)
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

			items.Add(new TopicNavigationItem(url, topic, topicsNavigation.Landing, topicsNavigation));
		}

		return items;
	}

	private List<INavigationItem> CreateChildNavigationItems(
		string apiUrlSuffix,
		IReadOnlyList<IFileInfo> children,
		IReadOnlyDictionary<IFileInfo, IReadOnlyList<string>> replacedTopicNames,
		TopicsNavigationItem topicsNavigation
	)
	{
		var markdownSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var items = new List<INavigationItem>(children.Count);
		foreach (var child in children)
		{
			var item = CreateChildNavigationItem(apiUrlSuffix, child, topicsNavigation, markdownSlugs);
			item.ReplacedTopicUrls = replacedTopicNames.TryGetValue(child, out var names) ? names : [];
			items.Add(item);
		}

		return items;
	}

	private SimpleMarkdownNavigationItem CreateChildNavigationItem(
		string apiUrlSuffix,
		IFileInfo markdownFile,
		TopicsNavigationItem topicsNavigation,
		HashSet<string> markdownSlugs
	)
	{
		var slug = SimpleMarkdownNavigationItem.CreateSlugFromFile(markdownFile);
		if (!markdownSlugs.Add(slug))
		{
			throw new InvalidOperationException(
				$"Duplicate markdown slug '{slug}' found in API product '{apiUrlSuffix}'. " + $"File: {markdownFile.FullName}"
			);
		}

		SimpleMarkdownNavigationItem.ValidateSlugForCollisions(slug, apiUrlSuffix, markdownFile.FullName);

		var url = ApiUrlBuilder.ChildUrl(context.UrlPathPrefix, apiUrlSuffix, slug);
		var title = MarkdownNavigationTitleReader.GetNavigationTitle(context.ReadFileSystem, markdownFile);
		return new SimpleMarkdownNavigationItem(url, title, markdownFile, topicsNavigation.Landing) { Parent = topicsNavigation };
	}

	/// <summary>The URLs each child takes over from the topics it replaces, for the version switcher.</summary>
	private Dictionary<IFileInfo, IReadOnlyList<string>> ReplacedTopicNames(
		string apiUrlSuffix,
		IReadOnlyDictionary<ApiTopicPage, IFileInfo> coveredTopics,
		bool hasSchemes
	) =>
		coveredTopics.GroupBy(static pair => pair.Value).ToDictionary(
			group => group.Key,
			group => (IReadOnlyList<string>)[.. group.SelectMany(pair => ReplacedNames(apiUrlSuffix, pair.Key, hasSchemes: hasSchemes))]
		);

	/// <summary>
	/// The topic page, and for an Authentication topic the <c>/authentication</c> page another version builds from it. That page
	/// name stays with the generated Authentication page when this version's spec declares security schemes.
	/// </summary>
	private IEnumerable<string> ReplacedNames(string apiUrlSuffix, ApiTopicPage topic, bool hasSchemes)
	{
		yield return ApiUrlBuilder.TopicUrl(context.UrlPathPrefix, apiUrlSuffix, topic.Title);
		if (topic.IsAuthentication && !hasSchemes)
			yield return ApiUrlBuilder.AuthenticationUrl(context.UrlPathPrefix, apiUrlSuffix);
	}

	/// <summary>
	/// The spec topics that a <c>children:</c> page already covers, with the child that covers each. The topic is not built;
	/// elastic/docs-infra redirects its bump.sh URL to the child.
	/// </summary>
	private Dictionary<ApiTopicPage, IFileInfo> CoveredTopics(
		string apiUrlSuffix,
		IReadOnlyList<ApiTopicPage> topics,
		IReadOnlyList<IFileInfo> children,
		bool hasSchemes
	)
	{
		var covered = new Dictionary<ApiTopicPage, IFileInfo>();
		if (topics.Count == 0 || children.Count == 0)
			return covered;

		var childPages = children
			.Select(child => (File: child, Page: MarkdownNavigationTitleReader.ReadHeadingAndBody(context.ReadFileSystem, child)))
			.Where(child => child.Page.Heading is not null)
			.ToArray();
		foreach (var topic in topics)
		{
			var match = childPages.FirstOrDefault(child => topic.IsCoveredBy(child.Page.Heading, child.Page.Body)).File;
			if (match is null)
				continue;

			logger.LogInformation(
				"API '{ApiKey}' builds no page for x-topics entry '{Title}', because the children: page '{File}' covers it.",
				apiUrlSuffix,
				topic.Title,
				match.Name
			);
			covered[topic] = match;
			if (topic.IsAuthentication && !hasSchemes)
			{
				logger.LogWarning(
					"API '{ApiKey}' builds no /authentication page: the children: page '{File}' replaces the Authentication topic and the spec declares no security schemes. Redirect /authentication to the child page in elastic/docs-infra.",
					apiUrlSuffix,
					match.Name
				);
			}
		}

		return covered;
	}
}
