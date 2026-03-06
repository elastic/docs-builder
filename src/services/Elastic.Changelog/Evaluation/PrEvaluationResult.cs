// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Evaluation;

/// <summary>Result of PR evaluation for changelog generation.</summary>
public enum PrEvaluationResult
{
	/// <summary>PR is eligible for changelog generation.</summary>
	Success,

	/// <summary>No label matching a changelog type was found on the PR.</summary>
	NoLabel,

	/// <summary>PR title is empty after processing.</summary>
	NoTitle,

	/// <summary>PR was skipped (body-only edit, bot commit, or all products blocked by labels).</summary>
	Skipped,

	/// <summary>Changelog file was manually edited by a human.</summary>
	ManuallyEdited
}
