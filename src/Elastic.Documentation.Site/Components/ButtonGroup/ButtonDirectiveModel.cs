// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.ButtonGroup;

public enum ButtonDirectiveStyle
{
	Primary,
	Secondary,
	Neutral
}

public sealed record ButtonDirectiveModel
{
	/// <summary>The link the Markdown renderer emits for the button text.</summary>
	public required IHtmlContent Link { get; init; }

	public ButtonDirectiveStyle Style { get; init; } = ButtonDirectiveStyle.Primary;

	/// <summary>Alignment of a standalone button: <c>left</c>, <c>center</c> or <c>right</c>.</summary>
	public string? Align { get; init; }

	/// <summary>True when the button sits inside a button group rather than on its own line.</summary>
	public bool InGroup { get; init; }

	public string CssClass =>
		string.Join(
			' ',
			new[]
			{
				InGroup ? "button-directive__item" : "button-directive",
				$"button-directive--{Style.ToString().ToLowerInvariant()}",
				InGroup || string.IsNullOrEmpty(Align) ? null : $"button-directive--{Align}"
			}.Where(c => c is not null)
		);
}
