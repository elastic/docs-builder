// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Documentation.Assembler.Configuration;
using Elastic.Markdown.CrossLinks;

namespace Documentation.Assembler.Building;

public class PublishEnvironmentUriResolver : IUriEnvironmentResolver
{
	private Uri BaseUri { get; }

	private PublishEnvironment PublishEnvironment { get; }

	private PreviewEnvironmentUriResolver PreviewResolver { get; }

	private FrozenDictionary<string, Repository> AllRepositories { get; }

	public PublishEnvironmentUriResolver(AssemblyConfiguration configuration, PublishEnvironment environment)
	{
		if (!Uri.TryCreate(environment.Uri, UriKind.Absolute, out var uri))
			throw new Exception($"Could not parse uri {environment.Uri} in environment {environment}");

		BaseUri = uri;
		PublishEnvironment = environment;
		PreviewResolver = new PreviewEnvironmentUriResolver();
		AllRepositories = configuration.ReferenceRepositories.Values.Concat<Repository>([configuration.Narrative])
			.ToFrozenDictionary(e => e.Name, e => e);
		RepositoryLookup = AllRepositories.GetAlternateLookup<ReadOnlySpan<char>>();
	}

	private FrozenDictionary<string, Repository>.AlternateLookup<ReadOnlySpan<char>> RepositoryLookup { get; }

	public Uri Resolve(Uri crossLinkUri, string path)
	{
		if (PublishEnvironment.Name == "preview")
			return PreviewResolver.Resolve(crossLinkUri, path);

		var repositoryPath = crossLinkUri.Scheme;
		if (RepositoryLookup.TryGetValue(crossLinkUri.Scheme, out var repository))
			repositoryPath = repository.PathPrefix;

		var fullPath = (PublishEnvironment.PathPrefix, repositoryPath) switch
		{
			(null or "", null or "") => path,
			(null or "", var p) => $"{p}/{path}",
			(var p, null or "") => $"{p}/{path}",
			var (p, pp) => $"{p}/{pp}/{path}"
		};

		return new Uri(BaseUri, fullPath);
	}
}
