// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Components.Admonition;

public enum AdmonitionKind
{
	Note,
	Tip,
	Important,
	Warning,
	Caution
}

public sealed record AdmonitionModel
{
	public required AdmonitionKind Kind { get; init; }
	public required string Title { get; init; }

	/// <summary>The rendered nested content of the directive.</summary>
	public required IHtmlContent Body { get; init; }

	public string? Id { get; init; }

	/// <summary>Extra author-supplied classes from the directive options.</summary>
	public string? Classes { get; init; }

	/// <summary>A pre-rendered applies-to badge shown in the header, when the directive declares one.</summary>
	public IHtmlContent? AppliesTo { get; init; }

	public string CssClass =>
		string.Join(
			' ',
			new[]
			{
				"admonition",
				$"admonition--{Kind.ToString().ToLowerInvariant()}",
				string.IsNullOrWhiteSpace(Classes) ? null : Classes
			}.Where(c => c is not null)
		);

	public string Icon => Kind switch
	{
		AdmonitionKind.Tip => "sparkles",
		AdmonitionKind.Important => "flag",
		AdmonitionKind.Warning => "error",
		AdmonitionKind.Caution => "cross_circle",
		_ => "info"
	};

	/// <summary>Maps a directive name such as <c>warning</c> to a kind; unknown names, including the generic <c>admonition</c>, read as a note.</summary>
	public static AdmonitionKind KindFor(string directive) => directive.ToLowerInvariant() switch
	{
		"tip" => AdmonitionKind.Tip,
		"important" => AdmonitionKind.Important,
		"warning" => AdmonitionKind.Warning,
		"caution" => AdmonitionKind.Caution,
		_ => AdmonitionKind.Note
	};
}
