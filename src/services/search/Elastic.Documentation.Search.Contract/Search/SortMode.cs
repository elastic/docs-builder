// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>How <see cref="SearchRequest"/> results are ordered.</summary>
public enum SortMode
{
	/// <summary>Order by <c>_score</c> descending.</summary>
	Relevance,
	/// <summary>Order by <c>last_updated</c> descending.</summary>
	Recent,
	/// <summary>Order by <c>title.keyword</c> ascending.</summary>
	Alpha
}
