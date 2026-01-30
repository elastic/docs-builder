// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation;

/// <summary>
/// Enum representing changelog entry types
/// </summary>
[EnumExtensions]
public enum ChangelogEntryType
{
	/// <summary>Invalid or unrecognized type - used for validation errors.</summary>
	[Display(Name = "invalid")]
	Invalid = 0,

	/// <summary>A new feature or enhancement.</summary>
	[Display(Name = "feature")]
	Feature,

	/// <summary>An improvement to an existing feature.</summary>
	[Display(Name = "enhancement")]
	Enhancement,

	/// <summary>An advisory about a potential security vulnerability.</summary>
	[Display(Name = "security")]
	Security,

	/// <summary>A bug fix.</summary>
	[Display(Name = "bug-fix")]
	BugFix,

	/// <summary>A breaking change to the documented behavior of the product.</summary>
	[Display(Name = "breaking-change")]
	BreakingChange,

	/// <summary>Functionality that is deprecated and will be removed in a future release.</summary>
	[Display(Name = "deprecation")]
	Deprecation,

	/// <summary>A problem that is known to exist in the product.</summary>
	[Display(Name = "known-issue")]
	KnownIssue,

	/// <summary>Major documentation changes or reorganizations.</summary>
	[Display(Name = "docs")]
	Docs,

	/// <summary>Functionality that no longer works or behaves incorrectly.</summary>
	[Display(Name = "regression")]
	Regression,

	/// <summary>Changes that do not fit into any of the other categories.</summary>
	[Display(Name = "other")]
	Other
}
