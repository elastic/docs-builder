// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.State;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.CrossLinks;

public interface ICrossLinkResolver
{
	Task FetchLinks();
	bool TryResolve(Action<string> errorEmitter, Uri crossLinkUri, [NotNullWhen(true)]out Uri? resolvedUri);
}

public class CrossLinkResolver(ConfigurationFile configuration, ILoggerFactory logger) : ICrossLinkResolver
{
	private readonly string[] _links = configuration.CrossLinkRepositories;
	private FrozenDictionary<string, LinkReference> _linkReferences = new Dictionary<string, LinkReference>().ToFrozenDictionary();
	private readonly ILogger _logger = logger.CreateLogger(nameof(CrossLinkResolver));

	public static LinkReference Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkReference)!;

	public async Task FetchLinks()
	{
		using var client = new HttpClient();
		var dictionary = new Dictionary<string, LinkReference>();
		foreach (var link in _links)
		{
			var url = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/{link}/main/links.json";
			_logger.LogInformation($"Fetching {url}");
			var json = await client.GetStringAsync(url);
			var linkReference = Deserialize(json);
			dictionary.Add(link, linkReference);
		}
		_linkReferences = dictionary.ToFrozenDictionary();
	}

	public bool TryResolve(Action<string> errorEmitter, Uri crossLinkUri, [NotNullWhen(true)]out Uri? resolvedUri) =>
		TryResolve(errorEmitter, _linkReferences, crossLinkUri, out resolvedUri);

	private static Uri BaseUri { get; } = new Uri("https://docs-v3-preview.elastic.dev");

	public static bool TryResolve(Action<string> errorEmitter, IDictionary<string, LinkReference> lookup, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri)
	{
		resolvedUri = null;
		if (!lookup.TryGetValue(crossLinkUri.Scheme, out var linkReference))
		{
			errorEmitter($"'{crossLinkUri.Scheme}' is not declared as valid cross link repository in docset.yml under cross_links");
			return false;
		}
		var lookupPath = crossLinkUri.AbsolutePath.TrimStart('/');
		if (string.IsNullOrEmpty(lookupPath) && crossLinkUri.Host.EndsWith(".md"))
			lookupPath = crossLinkUri.Host;

		if (!linkReference.Links.TryGetValue(lookupPath, out var link))
		{
			errorEmitter($"'{lookupPath}' is not a valid link in the '{crossLinkUri.Scheme}' cross link repository.");
			return false;
		}

		//https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/cloud-account/change-your-password
		var path = lookupPath.Replace(".md", "");
		if (path.EndsWith("/index"))
			path = path.Substring(0, path.Length - 6);
		if (path == "index")
			path = string.Empty;

		if (!string.IsNullOrEmpty(crossLinkUri.Fragment))
		{
			if (link.Anchors is null)
			{
				errorEmitter($"'{lookupPath}' does not have any anchors so linking to '{crossLinkUri.Fragment}' is impossible.");
				return false;
			}

			if (!link.Anchors.Contains(crossLinkUri.Fragment.TrimStart('#')))
			{
				errorEmitter($"'{lookupPath}' has no anchor named: '{crossLinkUri.Fragment}'.");
				return false;
			}
			path += crossLinkUri.Fragment;
		}

		resolvedUri = new Uri(BaseUri, $"elastic/{crossLinkUri.Scheme}/tree/main/{path}");
		return true;
	}
}
