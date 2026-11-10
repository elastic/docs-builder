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
}
