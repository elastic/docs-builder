// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.LinkIndex;

public interface ILinkIndexReader
{
	/// <summary>The link index, checked by <c>LinkLoader</c>: text that does not parse is an error diagnostic and no value; findings ride in the result.</summary>
	Task<Result<LinkRegistry>> GetRegistry(Cancel cancellationToken = default);

	/// <summary>The <c>links.json</c> at <paramref name="key"/>, checked by <c>LinkLoader</c> the same way.</summary>
	Task<Result<LinkManifest>> GetRepositoryLinks(string key, Cancel cancellationToken = default);
	string RegistryUrl { get; }
}
