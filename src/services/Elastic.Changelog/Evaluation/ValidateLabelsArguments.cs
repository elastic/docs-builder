// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Evaluation;

/// <summary>Arguments for the changelog validate-labels command.</summary>
public record ValidateLabelsArguments
{
	public required string Config { get; init; }
	public required string[] PrLabels { get; init; }
}
