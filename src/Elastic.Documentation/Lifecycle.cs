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
	/// <summary>Tech Previews are time-boxed evaluations to help us gather early feedback from customers on key upcoming features.</summary>
	[Display(Name = "preview")]
	Preview,

	/// <summary>A beta release of a feature or enhancement.</summary>
	[Display(Name = "beta")]
	Beta,

	/// <summary>Generally available features are stable, scalable, and production-ready.</summary>
	[Display(Name = "ga")]
	Ga,

	/// <summary>The experimental phase exists to enable rapid iteration on new features.</summary>
	[Display(Name = "experimental")]
	Experimental
}
