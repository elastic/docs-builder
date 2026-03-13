// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>Controls how changelog files created by 'changelog add' are named.</summary>
[EnumExtensions]
public enum FilenameStrategy
{
	/// <summary>Use the PR number as filename (e.g., 12345.yaml).</summary>
	[Display(Name = "pr")]
	Pr,

	/// <summary>Use the issue number as filename (e.g., 67890.yaml).</summary>
	[Display(Name = "issue")]
	Issue,

	/// <summary>Use a Unix timestamp with a title slug (e.g., 1735689600-fix-search.yaml).</summary>
	[Display(Name = "timestamp")]
	Timestamp
}
