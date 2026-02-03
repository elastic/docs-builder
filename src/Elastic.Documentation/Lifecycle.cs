// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation;

/// <summary>
/// Enum representing changelog entry lifecycle stages
/// </summary>
[EnumExtensions]
public enum Lifecycle
{
	/// <summary>A technical preview of a feature or enhancement.</summary>
	[Display(Name = "preview")]
	Preview,

	/// <summary>A beta release of a feature or enhancement.</summary>
	[Display(Name = "beta")]
	Beta,

	/// <summary>A generally available release of a feature or enhancement.</summary>
	[Display(Name = "ga")]
	Ga
}
