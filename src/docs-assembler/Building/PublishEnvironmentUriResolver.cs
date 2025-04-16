// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Documentation.Assembler.Extensions;
using Elastic.Documentation.Model.Configuration.Assembler;
using Elastic.Markdown.Links.CrossLinks;

namespace Documentation.Assembler.Building;

public class PublishEnvironmentUriResolver : IUriEnvironmentResolver
{
	private readonly FrozenDictionary<Uri, TocTopLevelMapping> _topLevelMappings;
	private Uri BaseUri { get; }

	private PublishEnvironment PublishEnvironment { get; }

	private IReadOnlyList<string> TableOfContentsPrefixes { get; }

	public PublishEnvironmentUriResolver(FrozenDictionary<Uri, TocTopLevelMapping> topLevelMappings, PublishEnvironment environment)
	{
		_topLevelMappings = topLevelMappings;
		PublishEnvironment = environment;

		TableOfContentsPrefixes = [..topLevelMappings
			.Values
			.Select(p =>
			{
				var source = p.Source.ToString();
				return source.EndsWith(":///") ? source[..^1] : source;
			})
			.OrderByDescending(v => v.Length)
		];

		if (!Uri.TryCreate(environment.Uri, UriKind.Absolute, out var uri))
			throw new Exception($"Could not parse uri {environment.Uri} in environment {environment}");

		BaseUri = uri;
	}

	public Uri Resolve(Uri crossLinkUri, string path)
	{
		if (crossLinkUri.Scheme == "detection-rules")
		{

		}

		var subPath = GetSubPathPrefix(crossLinkUri, ref path);

		var fullPath = (PublishEnvironment.PathPrefix, subPath) switch
		{
			(null or "", null or "") => path,
			(null or "", var p) => $"{p}/{path.TrimStart('/')}",
			(var p, null or "") => $"{p}/{path.TrimStart('/')}",
			var (p, pp) => $"{p}/{pp}/{path.TrimStart('/')}"
		};

		return new Uri(BaseUri, fullPath);
	}

	public static string MarkdownPathToUrlPath(string path)
	{
		if (path.EndsWith("/index.md"))
			path = path[..^8];
		if (path.EndsWith(".md"))
			path = path[..^3];
		return path;

	}

	public string[] ResolveToSubPaths(Uri crossLinkUri, string path)
	{
		var lookup = crossLinkUri.ToString().TrimEnd('/').AsSpan();
		if (lookup.EndsWith("index.md", StringComparison.Ordinal))
			lookup = lookup[..^8];
		if (lookup.EndsWith(".md", StringComparison.Ordinal))
			lookup = lookup[..^3];

		Uri? match = null;
		foreach (var prefix in TableOfContentsPrefixes)
		{
			if (!lookup.StartsWith(prefix, StringComparison.Ordinal))
				continue;
			match = new Uri(prefix);
			break;
		}

		if (match is null || !_topLevelMappings.TryGetValue(match, out var toc))
		{
			var fallBack = new Uri(lookup.ToString());
			return [$"{fallBack.Host}/{fallBack.AbsolutePath.Trim('/')}"];
		}
		path = MarkdownPathToUrlPath(path);

		var originalPath = Path.Combine(match.Host, match.AbsolutePath.Trim('/')).TrimStart('/');
		var relativePathSpan = path.AsSpan();
		var newRelativePath = relativePathSpan.StartsWith(originalPath, StringComparison.Ordinal)
			? relativePathSpan.Slice(originalPath.Length).TrimStart('/').ToString()
			: relativePathSpan.TrimStart(originalPath).TrimStart('/').ToString();

		var tokens = newRelativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var paths = new List<string>();
		var p = "";
		for (var index = 0; index < tokens.Length; index++)
		{
			p += tokens[index] + '/';
			paths.Add(p);
		}

		return paths
			.Select(i => $"{toc.SourcePathPrefix}/{i.TrimStart('/')}")
			.Concat([$"{toc.SourcePathPrefix}/"])
			.ToArray();
	}

	private string GetSubPathPrefix(Uri crossLinkUri, ref string path)
	{
		var lookup = crossLinkUri.ToString().AsSpan();
		if (lookup.EndsWith(".md", StringComparison.Ordinal))
			lookup = lookup[..^3];

		// temporary fix only spotted two instances of this:
		// Error: Unable to find defined toc for url: docs-content:///manage-data/ingest/transform-enrich/set-up-an-enrich-processor.md
		// Error: Unable to find defined toc for url: kibana:///reference/configuration-reference.md
		if (lookup.IndexOf(":///") >= 0)
			lookup = lookup.ToString().Replace(":///", "://").AsSpan();

		Uri? match = null;
		foreach (var prefix in TableOfContentsPrefixes)
		{
			if (!lookup.StartsWith(prefix, StringComparison.Ordinal))
				continue;
			match = new Uri(prefix);
			break;
		}

		if (match is null || !_topLevelMappings.TryGetValue(match, out var toc))
			return string.Empty;


		var originalPath = Path.Combine(match.Host, match.AbsolutePath.Trim('/'));
		if (originalPath == toc.SourcePathPrefix)
			return string.Empty;

		var newRelativePath = path.AsSpan().GetTrimmedRelativePath(originalPath);
		path = Path.Combine(toc.SourcePathPrefix, newRelativePath);

		return string.Empty;
	}
}
