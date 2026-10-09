// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.Badge;

namespace Elastic.Documentation.Site.Components.Method;

public enum HttpVerb
{
	Get,
	Post,
	Put,
	Patch,
	Delete,
	Head
}

public sealed record MethodBadgeModel
{
	public required HttpVerb Method { get; init; }

	/// <summary>The compact form used in navigation trees.</summary>
	public bool Small { get; init; }

	public string Text => Method == HttpVerb.Delete && Small ? "DEL" : Method.ToString().ToUpperInvariant();

	public Tone Tone => Method switch
	{
		HttpVerb.Get => Tone.Info,
		HttpVerb.Post => Tone.Success,
		HttpVerb.Put => Tone.Warning,
		HttpVerb.Patch => Tone.Important,
		HttpVerb.Delete => Tone.Danger,
		_ => Tone.Neutral
	};

	public string CssClass =>
		string.Join(
			' ',
			new[] { new BadgeModel { Text = Text, Tone = Tone }.CssClass, "method", Small ? "method--sm" : null }.Where(c => c is not null)
		);
}
