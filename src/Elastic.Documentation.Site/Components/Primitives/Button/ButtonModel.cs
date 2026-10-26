// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.Primitives.Icon;
using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.Primitives.Button;

public enum ButtonVariant
{
	Primary,
	Secondary,
	Tertiary,
	Danger,

	/// <summary>The teal call to action, for heroes and bands only.</summary>
	Cta
}

public enum ButtonSize
{
	Medium,
	Small,
	Large
}

public sealed record ButtonModel
{
	/// <summary>Visible text. When <see cref="IconOnly"/> is set it becomes the accessible name instead.</summary>
	public required string Label { get; init; }

	/// <summary>Renders an anchor when set and a button otherwise.</summary>
	public string? Href { get; init; }

	public ButtonVariant Variant { get; init; } = ButtonVariant.Primary;
	public ButtonSize Size { get; init; } = ButtonSize.Medium;
	public string? Icon { get; init; }
	public string? TrailingIcon { get; init; }
	public bool IconOnly { get; init; }
	public bool Disabled { get; init; }

	/// <summary>Offers the target as a download. Only an anchor can, so it is ignored without <see cref="Href"/>.</summary>
	public bool Download { get; init; }

	/// <summary>Hooks for scripts, written as <c>data-{key}</c>. Scripts bind to these, never to the styling classes.</summary>
	public IReadOnlyDictionary<string, string>? Data { get; init; }

	public HtmlString DataAttributes => ComponentData.Attributes(Data);

	public string CssClass =>
		string.Join(
			' ',
			new[]
			{
				"btn",
				$"btn--{Variant.ToString().ToLowerInvariant()}",
				Size switch
				{
					ButtonSize.Small => "btn--sm",
					ButtonSize.Large => "btn--lg",
					_ => null
				},
				IconOnly ? "btn--icon" : null
			}.Where(c => c is not null)
		);

	public IconSize IconSize => Size == ButtonSize.Small ? IconSize.Small : IconSize.Medium;
}
