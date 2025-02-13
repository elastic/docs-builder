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
	bool TryResolve(Uri crosslinkUri, [NotNullWhen(true)]out Uri? resolvedUri);
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

	public bool TryResolve(Uri crosslinkUri, [NotNullWhen(true)]out Uri? resolvedUri) =>
		TryResolve(_linkReferences, crosslinkUri, out resolvedUri);

	public static bool TryResolve(IDictionary<string, LinkReference> lookup, Uri crosslinkUri, [NotNullWhen(true)] out Uri? resolvedUri)
	{
		resolvedUri = null;
		if (!lookup.TryGetValue(crosslinkUri.Scheme, out var linkReference))
		{
			//TODO emit error
			return false;
		}
		var lookupPath = crosslinkUri.AbsolutePath.TrimStart('/');

		if (!linkReference.Links.TryGetValue(lookupPath, out var link))
		{
			//TODO emit error
			return false;
		}

		//https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/cloud-account/change-your-password
		var path = lookupPath.Replace(".md", "");
		var baseUri = new Uri("https://docs-v3-preview.elastic.dev");
		resolvedUri = new Uri(baseUri, $"elastic/{crosslinkUri.Scheme}/tree/main/{path}");
		return true;
	}
}
