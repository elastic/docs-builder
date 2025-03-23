// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Collections.Immutable;
using Documentation.Assembler.Configuration;
using Documentation.Assembler.Navigation;
using Documentation.Assembler.Sourcing;
using Elastic.Markdown.CrossLinks;

namespace Documentation.Assembler.Building;

public class PublishEnvironmentUriResolver : IUriEnvironmentResolver
{
	private Uri BaseUri { get; }

	private PublishEnvironment PublishEnvironment { get; }

	private IsolatedBuildEnvironmentUriResolver IsolatedBuildResolver { get; }

	private ImmutableSortedSet<string> TableOfContentsPrefixes { get; }

	public PublishEnvironmentUriResolver(FrozenDictionary<Uri, TocTopLevelMapping> configuredSources, PublishEnvironment environment)
	{
		PublishEnvironment = environment;
		IsolatedBuildResolver = new IsolatedBuildEnvironmentUriResolver();

		TableOfContentsPrefixes = configuredSources.Values
			.Select(kv => kv.SourcePathPrefix)
			.OrderByDescending(k => k.Length)
			.ToImmutableSortedSet();

		if (!Uri.TryCreate(environment.Uri, UriKind.Absolute, out var uri))
			throw new Exception($"Could not parse uri {environment.Uri} in environment {environment}");

		BaseUri = uri;
	}

	public Uri Resolve(Uri crossLinkUri, string path)
	{
		// TODO Maybe not needed
		if (PublishEnvironment.Name == "preview")
			return IsolatedBuildResolver.Resolve(crossLinkUri, path);

		var subPath = GetSubPath(crossLinkUri, ref path);

		var fullPath = (PublishEnvironment.PathPrefix, subPath) switch
		{
			(null or "", null or "") => path,
			(null or "", var p) => $"{p}/{path.TrimStart('/')}",
			(var p, null or "") => $"{p}/{path.TrimStart('/')}",
			var (p, pp) => $"{p}/{pp}/{path.TrimStart('/')}"
		};

		return new Uri(BaseUri, fullPath);
	}

	public string GetSubPath(Uri crossLinkUri, ref string path)
	{
		var lookup = crossLinkUri.ToString().AsSpan();
		if (lookup.EndsWith(".md", StringComparison.Ordinal))
			lookup = lookup[..^3];

		// temporary fix only spotted two instances of this:
		// Error: Unable to find defined toc for url: docs-content:///manage-data/ingest/transform-enrich/set-up-an-enrich-processor.md
		// Error: Unable to find defined toc for url: kibana:///reference/configuration-reference.md
		if (lookup.IndexOf(":///") >= 0)
			lookup = lookup.ToString().Replace(":///", "://").AsSpan();

		string? match = null;
		foreach (var prefix in TableOfContentsPrefixes)
		{
			if (!lookup.StartsWith(prefix, StringComparison.Ordinal))
				continue;
			match = prefix;
			break;
		}

		/*
		if (match is null || !_tocLookup.TryGetValue(match, out var toc))
		{
			//TODO remove
			if (crossLinkUri.Scheme != "asciidocalypse")
				_context.Collector.EmitError(_context.NavigationPath, $"Unable to find defined toc for url: {crossLinkUri}");
			return $"reference/{crossLinkUri.Scheme}";
		}*/

		//path = path.AsSpan().TrimStart(toc.SourcePrefix).ToString();

		return path;
	}

}
