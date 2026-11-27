// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract;

namespace Elastic.ApiExplorer.Export;

/// <summary>
/// The plain words of an operation id and its literal path segments, e.g. <c>ml-flush-job</c> and
/// <c>/_ml/anomaly_detectors/{job_id}/_flush</c> give <c>ml flush job anomaly detectors</c>.
/// The search analyzer keeps <c>_ - .</c> inside a token, so without these words <c>ml flush</c> never meets <c>_flush</c>.
/// </summary>
internal static class ApiSearchWords
{
	internal static string[] Of(string operationId, string path) =>
		[
			.. path
				.Split('/', StringSplitOptions.RemoveEmptyEntries)
				.Where(segment => !segment.StartsWith('{'))
				.Prepend(operationId)
				.SelectMany(ApiPropertyPath.IdentifierWords)
				.Distinct(StringComparer.Ordinal)
		];

	internal static string[] OfOperationId(string operationId) => Of(operationId, string.Empty);

	/// <summary>
	/// <see cref="Of"/> plus the two verbs of a title that opens "X or Y", because ids say <c>put</c> where readers say
	/// <c>create</c>: <c>Create or update users</c> adds <c>create</c> and <c>update</c> to <c>security put user</c>.
	/// Any other title opening is left out, as a plain <c>Get</c> would lift every operation that starts with it.
	/// </summary>
	internal static string[] OfOperation(string operationId, string path, string title) =>
		[.. Of(operationId, path).Concat(EitherOrVerbs(title)).Distinct(StringComparer.Ordinal)];

	private static IEnumerable<string> EitherOrVerbs(string title) =>
		title
			.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
			.Select(w => w.ToLowerInvariant())
			.ToArray() is [var first, "or", var second, ..]
			&& first.All(char.IsLetter)
			&& second.All(char.IsLetter) ? [first, second] : [];
}
