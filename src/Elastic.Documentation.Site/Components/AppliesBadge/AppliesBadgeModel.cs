// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.AppliesBadge;

public enum Lifecycle
{
	Ga,
	Preview,
	Deprecated,
	Unavailable
}

public sealed record AppliesBadgeModel
{
	public required string Product { get; init; }

	/// <summary>Lifecycle and version, for example "GA 9.0+".</summary>
	public required string Status { get; init; }

	public Lifecycle Lifecycle { get; init; } = Lifecycle.Ga;

	/// <summary>The outlined, rounded form used under page titles.</summary>
	public bool Outlined { get; init; }

	public string CssClass =>
		string.Join(
			' ',
			new[]
			{
				Outlined ? "applies-pill" : "applies-badge",
				$"{(Outlined ? "applies-pill" : "applies-badge")}--{Lifecycle.ToString().ToLowerInvariant()}"
			}
		);

	public string Part => Outlined ? "applies-pill" : "applies-badge";
}
