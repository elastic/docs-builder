// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.Icon;

namespace Elastic.Documentation.Site.Components.Button;

public enum ButtonVariant
{
	Primary,
	Secondary,
	Tertiary,
	Danger
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
