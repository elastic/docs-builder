// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.Badge;

namespace Elastic.Documentation.Site.Components.Method;

public enum HttpMethod
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
	public required HttpMethod Method { get; init; }

	/// <summary>The compact form used in navigation trees.</summary>
	public bool Small { get; init; }

	public string Text => Method == HttpMethod.Delete && Small ? "DEL" : Method.ToString().ToUpperInvariant();

	public Tone Tone => Method switch
	{
		HttpMethod.Get => Tone.Info,
		HttpMethod.Post => Tone.Success,
		HttpMethod.Put => Tone.Warning,
		HttpMethod.Patch => Tone.Important,
		HttpMethod.Delete => Tone.Danger,
		_ => Tone.Neutral
	};

	public string CssClass =>
		string.Join(
			' ',
			new[] { new BadgeModel { Text = Text, Tone = Tone }.CssClass, "method", Small ? "method--sm" : null }.Where(c => c is not null)
		);
}
