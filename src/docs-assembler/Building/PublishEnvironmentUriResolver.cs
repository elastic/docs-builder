// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Documentation.Assembler.Configuration;
using Documentation.Assembler.Navigation;
using Elastic.Markdown.CrossLinks;

namespace Documentation.Assembler.Building;

public class PublishEnvironmentUriResolver : IUriEnvironmentResolver
{
	private readonly GlobalNavigationPathProvider _globalNavigationPathProvider;
	private Uri BaseUri { get; }

	private PublishEnvironment PublishEnvironment { get; }

	private IsolatedBuildEnvironmentUriResolver IsolatedBuildResolver { get; }

	public PublishEnvironmentUriResolver(GlobalNavigationPathProvider globalNavigationPathProvider, PublishEnvironment environment)
	{
		_globalNavigationPathProvider = globalNavigationPathProvider;
		if (!Uri.TryCreate(environment.Uri, UriKind.Absolute, out var uri))
			throw new Exception($"Could not parse uri {environment.Uri} in environment {environment}");

		BaseUri = uri;
		PublishEnvironment = environment;
		IsolatedBuildResolver = new IsolatedBuildEnvironmentUriResolver();
	}

	public Uri Resolve(Uri crossLinkUri, string path)
	{
		// TODO Maybe not needed
		if (PublishEnvironment.Name == "preview")
			return IsolatedBuildResolver.Resolve(crossLinkUri, path);

		var subPath = _globalNavigationPathProvider.GetSubPath(crossLinkUri, ref path);

		var fullPath = (PublishEnvironment.PathPrefix, subPath) switch
		{
			(null or "", null or "") => path,
			(null or "", var p) => $"{p}/{path.TrimStart('/')}",
			(var p, null or "") => $"{p}/{path.TrimStart('/')}",
			var (p, pp) => $"{p}/{pp}/{path.TrimStart('/')}"
		};

		return new Uri(BaseUri, fullPath);
	}
}
