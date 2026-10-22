// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Primitives.Badge;

public enum Tone
{
	Neutral,
	Info,
	Success,
	Warning,
	Danger,
	Important
}

public enum BadgeShape
{
	/// <summary>A small rounded label.</summary>
	Label,

	/// <summary>A fully rounded label.</summary>
	Pill,

	/// <summary>A solid uppercase tag in the emphasis colour.</summary>
	Tag
}

public sealed record BadgeModel
{
	public required string Text { get; init; }
	public Tone Tone { get; init; } = Tone.Neutral;
	public BadgeShape Shape { get; init; } = BadgeShape.Label;

	/// <summary>Uses the monospace font, for status codes and versions.</summary>
	public bool Mono { get; init; }

	public string? Icon { get; init; }

	public string CssClass =>
		string.Join(
			' ',
			new[]
			{
				"badge",
				Tone == Tone.Neutral ? null : $"badge--{Tone.ToString().ToLowerInvariant()}",
				Shape == BadgeShape.Label ? null : $"badge--{Shape.ToString().ToLowerInvariant()}",
				Mono ? "badge--mono" : null
			}.Where(c => c is not null)
		);
}
