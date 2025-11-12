// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;

namespace Elastic.Documentation.Api.Core.Search;

public interface ISearchGateway
{
	Task<(int TotalHits, List<SearchResultItem> Results)> SearchAsync(
		Activity? parentActivity,
		string query,
		int pageNumber,
		int pageSize,
		Cancel ctx = default
	);
}
