// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.State;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.CrossLinks;


public record LinkIndex
{
	[JsonPropertyName("repositories")]
	public required Dictionary<string, Dictionary<string, LinkIndexEntry>> Repositories { get; init; }

	public static LinkIndex Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkIndex)!;

	public static string Serialize(LinkIndex index) =>
		JsonSerializer.Serialize(index, SourceGenerationContext.Default.LinkIndex);
}

public record LinkIndexEntry
{
	[JsonPropertyName("repository")]
	public required string Repository { get; init; }

	[JsonPropertyName("path")]
	public required string Path { get; init; }

	[JsonPropertyName("branch")]
	public required string Branch { get; init; }

	[JsonPropertyName("etag")]
	public required string ETag { get; init; }
}

public interface ICrossLinkResolver
{
	Task FetchLinks();
	bool TryResolve(Action<string> errorEmitter, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri);
}

public class CrossLinkResolver(ConfigurationFile configuration, ILoggerFactory logger) : ICrossLinkResolver
{
	private readonly string[] _links = configuration.CrossLinkRepositories;
	private FrozenDictionary<string, LinkReference> _linkReferences = new Dictionary<string, LinkReference>().ToFrozenDictionary();
	private readonly ILogger _logger = logger.CreateLogger(nameof(CrossLinkResolver));
	private readonly HashSet<string> _declaredRepositories = [];

	public static LinkReference Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkReference)!;

	public async Task FetchLinks()
	{
		using var client = new HttpClient();
		var dictionary = new Dictionary<string, LinkReference>();
		foreach (var link in _links)
		{
			_ = _declaredRepositories.Add(link);
			try
			{
				var url = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/{link}/main/links.json";
				_logger.LogInformation("Fetching {Url}", url);
				var json = await client.GetStringAsync(url);
				var linkReference = Deserialize(json);
				dictionary.Add(link, linkReference);
			}
			catch when (link == "docs-content")
			{
				throw;
			}
			catch when (link != "docs-content")
			{
				// TODO: ignored for now while we wait for all links.json files to populate
			}
		}
		_linkReferences = dictionary.ToFrozenDictionary();
	}

	public bool TryResolve(Action<string> errorEmitter, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri) =>
		TryResolve(errorEmitter, _declaredRepositories, _linkReferences, crossLinkUri, out resolvedUri);

	private static Uri BaseUri { get; } = new Uri("https://docs-v3-preview.elastic.dev");

	public static bool TryResolve(Action<string> errorEmitter, HashSet<string> declaredRepositories, IDictionary<string, LinkReference> lookup, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri)
	{
		resolvedUri = null;
		if (crossLinkUri.Scheme == "docs-content")
		{
			if (!lookup.TryGetValue(crossLinkUri.Scheme, out var linkReference))
			{
				errorEmitter($"'{crossLinkUri.Scheme}' is not declared as valid cross link repository in docset.yml under cross_links");
				return false;
			}
			return TryFullyValidate(errorEmitter, linkReference, crossLinkUri, out resolvedUri);
		}

		// TODO this is temporary while we wait for all links.json files to be published
		if (!declaredRepositories.Contains(crossLinkUri.Scheme))
		{
			errorEmitter($"'{crossLinkUri.Scheme}' is not declared as valid cross link repository in docset.yml under cross_links");
			return false;
		}

		var lookupPath = (crossLinkUri.Host + '/' + crossLinkUri.AbsolutePath.TrimStart('/')).Trim('/');
		var path = ToTargetUrlPath(lookupPath);
		if (!string.IsNullOrEmpty(crossLinkUri.Fragment))
			path += crossLinkUri.Fragment;

		var branch = GetBranch(crossLinkUri);
		resolvedUri = new Uri(BaseUri, $"elastic/{crossLinkUri.Scheme}/tree/{branch}/{path}");
		return true;
	}

	private static bool TryFullyValidate(Action<string> errorEmitter, LinkReference linkReference, Uri crossLinkUri, [NotNullWhen(true)] out Uri? resolvedUri)
	{
		resolvedUri = null;
		var lookupPath = (crossLinkUri.Host + '/' + crossLinkUri.AbsolutePath.TrimStart('/')).Trim('/');
		if (string.IsNullOrEmpty(lookupPath) && crossLinkUri.Host.EndsWith(".md"))
			lookupPath = crossLinkUri.Host;

		if (!linkReference.Links.TryGetValue(lookupPath, out var link))
		{
			errorEmitter($"'{lookupPath}' is not a valid link in the '{crossLinkUri.Scheme}' cross link repository.");
			return false;
		}

		var path = ToTargetUrlPath(lookupPath);

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

		var branch = GetBranch(crossLinkUri);
		resolvedUri = new Uri(BaseUri, $"elastic/{crossLinkUri.Scheme}/tree/{branch}/{path}");
		return true;
	}

	/// Hardcoding these for now, we'll have an index.json pointing to all links.json files
	/// at some point from which we can query the branch soon.
	private static string GetBranch(Uri crossLinkUri)
	{
		var branch = crossLinkUri.Scheme switch
		{
			"docs-content" => "main",
			_ => "main"
		};
		return branch;
	}


	private static string ToTargetUrlPath(string lookupPath)
	{
		//https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/cloud-account/change-your-password
		var path = lookupPath.Replace(".md", "");
		if (path.EndsWith("/index"))
			path = path[..^6];
		if (path == "index")
			path = string.Empty;
		return path;
	}
}
