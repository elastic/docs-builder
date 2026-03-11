// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Elastic.Changelog.Evaluation;

/// <summary>Result of PR evaluation for changelog generation.</summary>
[EnumExtensions]
public enum PrEvaluationResult
{
	/// <summary>PR is eligible for changelog generation.</summary>
	[Display(Name = "success")]
	Success,

	/// <summary>No label matching a changelog type was found on the PR.</summary>
	[Display(Name = "no-label")]
	NoLabel,

	/// <summary>PR title is empty after processing.</summary>
	[Display(Name = "no-title")]
	NoTitle,

	/// <summary>PR was skipped (body-only edit, bot commit, or all products blocked by labels).</summary>
	[Display(Name = "skipped")]
	Skipped,

	/// <summary>Changelog file was manually edited by a human.</summary>
	[Display(Name = "manually-edited")]
	ManuallyEdited,

	/// <summary>An error occurred during artifact preparation (e.g., generate step failed or YAML missing).</summary>
	[Display(Name = "error")]
	Error
}
