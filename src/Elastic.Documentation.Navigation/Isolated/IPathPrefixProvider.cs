// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation.Isolated;

public interface IPathPrefixProvider
{
	string PathPrefix { get; }
}

public interface INavigationPathPrefixProvider
{
	IPathPrefixProvider PathPrefixProvider { get; set; }
}

public class PathPrefixProvider(string pathPrefix) : IPathPrefixProvider
{
	/// <inheritdoc />
	public string PathPrefix { get; } = pathPrefix;
}

