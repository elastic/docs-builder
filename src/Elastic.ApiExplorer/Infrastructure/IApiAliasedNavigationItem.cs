// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>A page that other URLs forward to. The generator writes a redirect page at each alias.</summary>
public interface IApiAliasedNavigationItem : INavigationItem
{
	IReadOnlyList<string> AliasUrls { get; }
}
