// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation.Configuration;

/// <summary>
/// Registry type for cross-link resolution. Maps to the link index source:
/// <c>Public</c> uses the S3-based public link index; other values use the codex-link-index for that environment.
/// </summary>
[EnumExtensions]
public enum DocSetRegistry
{
	/// <summary>Public documentation; uses S3-based link index.</summary>
	[Display(Name = "public")]
	Public,

	/// <summary>Internal codex environment; uses codex-link-index/internal/.</summary>
	[Display(Name = "internal")]
	Internal,
}
