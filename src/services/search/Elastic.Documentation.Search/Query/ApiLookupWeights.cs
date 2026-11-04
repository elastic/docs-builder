// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#pragma warning disable IDE0130 // 'Query' subfolder would shadow the ES client's Query type
namespace Elastic.Documentation.Search;

/// <summary>
/// The weight of each role a word of an API lookup can play, measured on the dev index. The order between them matters
/// more than the values, and these invariants hold:
/// <list type="bullet">
/// <item><see cref="EndpointKey"/> + <see cref="FieldName"/> beats <see cref="WholeTitle"/> + <see cref="OperationWord"/> + <see cref="TitleWord"/>,
/// so <c>search knn</c> keeps <c>search</c> above <c>knn-eval</c> (8.07 against 6.57).</item>
/// <item><see cref="OperationWord"/> beats <see cref="GroupWord"/>, so <c>cluster-get-settings</c> precedes <c>nodes-info</c>, whose group is Cluster.</item>
/// <item><see cref="OperationWord"/> beats <see cref="FieldName"/>, so <c>cluster health</c> is <c>cluster-health</c>, not <c>cat-health</c> through its <c>cluster</c> field.</item>
/// <item><see cref="WholeTitle"/> lets a natural-language title beat a word that is also an endpoint, as in <c>get cluster settings</c>.</item>
/// </list>
/// </summary>
public static class ApiLookupWeights
{
	public const float EndpointKey = 6.0f;
	public const float OperationWord = 2.0f;
	public const float GroupWord = 1.0f;
	public const float TitleWord = 1.0f;
	public const float FieldName = 1.0f;
	public const float FieldNamePrefix = 0.6f;
	public const float ChildOption = 0.4f;
	public const float Method = 0.5f;
	public const float BodyWord = 0.1f;
	public const float WholeTitle = 3.0f;
	public const float ListsChildren = 0.5f;
	public const float Specificity = 0.5f;
	public const float NotDeprecated = 0.2f;
}
