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
/// so <c>search knn</c> keeps <c>search</c> above <c>knn-eval</c> (8.47 against 6.90).</item>
/// <item><see cref="OperationWord"/> beats <see cref="GroupWord"/>, so <c>cluster-get-settings</c> precedes <c>nodes-info</c>, whose group is Cluster.</item>
/// <item><see cref="OperationWord"/> beats <see cref="FieldName"/>, so <c>cluster health</c> is <c>cluster-health</c>, not <c>cat-health</c> through its <c>cluster</c> field.</item>
/// <item><see cref="EndpointKey"/> beats <see cref="NamespaceKey"/>, so a bare <c>search</c> keeps the <c>_search</c> operation above <c>_search/scroll</c> (9.97 against 9.47).</item>
/// <item><see cref="NamespaceKey"/> + <see cref="OperationWord"/> beats <see cref="EndpointKey"/> + <see cref="GroupWord"/>, so <c>snapshot restore</c> is <c>snapshot-restore</c>
/// (11.90) above <c>snapshot-create</c> (11.40), whose group is "Snapshot and restore". It also widens <c>cluster health</c> to 11.40 against 6.90.</item>
/// <item>A typed route takes the same two keys and beats the routes under it, so <c>/_search</c> keeps <c>search</c> above
/// <c>scroll</c> (9.58 against 9.08) and <c>/api/fleet</c> lists the 326 Fleet routes.</item>
/// <item><see cref="NamespaceKey"/> beats <see cref="EndpointCompletion"/>, so a complete typed route keeps its list (<c>/_search</c>: <c>search</c>
/// 9.97, <c>scroll</c> 9.47) and <c>_search_shards</c> joins after <c>_search/scroll</c>.</item>
/// <item><see cref="EndpointCompletion"/> beats <see cref="NamespaceCompletion"/>, so <c>/_sear</c> keeps <c>search</c> (8.97) above <c>scroll</c> (8.47).
/// Both sit below <see cref="EndpointKey"/>: a key typed in full beats one typed in part.</item>
/// <item>The bare keys score <c>/search</c> the same as <c>/_search</c>, and <c>/cluster health</c> finds <c>cluster-health</c> (11.07).</item>
/// <item><see cref="FieldWord"/> sits between <see cref="FieldNamePrefix"/> and <see cref="ChildOption"/>, so a typed identifier beats its words
/// and a row named by a word beats a listed child. With <see cref="FieldSeveralWords"/> it adds up to <see cref="FieldName"/>, so
/// <c>unrated docs</c> finds <c>unrated_docs</c> as surely as the identifier does.</item>
/// <item><see cref="WholeTitle"/> lets a natural-language title beat a word that is also an endpoint, as in <c>get cluster settings</c>.</item>
/// <item>A method typed before a path lifts by <see cref="Method"/> only and is no word of a title, so <c>GET /_search</c> keeps the
/// <c>POST</c> operation <c>search</c> first (10.20) above <c>scroll</c> (9.70), where "Get the search shards" led it (9.13).</item>
/// <item><see cref="OperationWordPrefix"/> sits just below <see cref="OperationWord"/>, so a word typed whole beats one that only starts another word.</item>
/// <item><see cref="OperationWordTypo"/> sits between <see cref="OperationWord"/> and the one-point roles, so an exact operation word beats a
/// typo of it, and a typo of an operation word beats a word that only a title, a group or a field name carries.</item>
/// <item><see cref="TitleNamedTypo"/> sits below <see cref="TitleNamed"/>, so a title typed exactly beats one typed with a typo, and below every
/// endpoint key, so it only breaks ties. A word one edit from another title lifts that title too, as <c>stats</c> does for <c>state</c>.</item>
/// <item><see cref="NotDeprecated"/> sits above every tie-break (<see cref="Method"/>, <see cref="Specificity"/>, <see cref="ListsChildren"/>,
/// <see cref="SameApi"/>, 0.5 each) and below every role, so it only separates operations that explain the typed words equally well, and
/// a tie-break never lifts a deprecated operation above a current one: <c>create user</c> lists the current <c>Create or update users</c>
/// (8.08 + 0.4 against 8.34) above the deprecated <c>Create a new monitored user</c>, which a <c>user</c> object in its body had lifted.
/// <c>template</c> lists the current <c>HEAD /_template/{name}</c> (10.08) above the three
/// deprecated legacy template operations (9.88), and <c>legacy index template</c> still puts those first (11.88 against 8.18), since a
/// better match beats the demotion. It lifts every operation whose flag is not true, whether the index omits the flag or writes
/// <c>false</c>.</item>
/// </list>
/// </summary>
public static class ApiLookupWeights
{
	public const float EndpointKey = 6.0f;
	public const float NamespaceKey = 5.5f;
	public const float EndpointCompletion = 5.0f;
	public const float NamespaceCompletion = 4.5f;
	public const float OperationWord = 2.0f;
	public const float OperationWordPrefix = 1.8f;
	public const float OperationWordTypo = 1.5f;
	public const float GroupWord = 1.0f;
	public const float TitleWord = 1.0f;
	public const float FieldName = 1.0f;
	public const float FieldNamePrefix = 0.6f;
	public const float FieldWord = 0.5f;
	public const float FieldSeveralWords = FieldName - FieldWord;
	public const float ChildOption = 0.4f;
	public const float Method = 0.5f;
	public const float BodyWord = 0.1f;
	public const float WholeTitle = 3.0f;

	/// <summary>
	/// The typed words are the whole title of an operation once stopwords and plurals are ignored. It adds to
	/// <see cref="WholeTitle"/>, which every operation that mentions the words earns.
	/// </summary>
	public const float TitleNamed = 6.0f;

	/// <summary>The typed words are the whole title of an operation within one edit.</summary>
	public const float TitleNamedTypo = 2.0f;
	public const float ListsChildren = 0.5f;
	public const float Specificity = 0.5f;
	public const float NotDeprecated = 0.6f;

	/// <summary>
	/// Pins an API's landing page above every operation when the reader typed only words of its name, as in <c>kibana api</c>.
	/// Far above the best an operation reaches (an endpoint key plus a whole title is about 12), so the landing page leads
	/// whatever the words also mean to an operation.
	/// </summary>
	public const float LandingName = 50.0f;

	/// <summary>
	/// Lifts the hits of the API the reader is on, which breaks ties between identical operations of sibling APIs. It sits
	/// below <see cref="WholeTitle"/> and <see cref="EndpointKey"/>, so a better match from another API still wins.
	/// </summary>
	public const float SameApi = 0.5f;
}
