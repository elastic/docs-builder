// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Primitives.Button;

public static class ButtonExamples
{
	public static ComponentSection Buttons { get; } = new(
		"Button",
		"Four variants, three sizes. A button with an href renders as a link; without one it renders as a button. Hover, focus and press show real states.",
		"btn",
		[
			ComponentExample.Of("Primary", _Button.Create, new ButtonModel { Label = "Get started", Href = "#" }),
			ComponentExample.Of(
				"Secondary",
				_Button.Create,
				new ButtonModel { Label = "View docs", Href = "#", Variant = ButtonVariant.Secondary }
			),
			ComponentExample.Of("Tertiary", _Button.Create, new ButtonModel { Label = "Learn more", Variant = ButtonVariant.Tertiary }),
			ComponentExample.Of("Danger", _Button.Create, new ButtonModel { Label = "Delete", Variant = ButtonVariant.Danger }),
			ComponentExample.Of("Small", _Button.Create, new ButtonModel { Label = "Small", Size = ButtonSize.Small }),
			ComponentExample.Of("Large", _Button.Create, new ButtonModel { Label = "Large", Size = ButtonSize.Large }),
			ComponentExample.Of(
				"With icons",
				_Button.Create,
				new ButtonModel { Label = "Copy", Icon = "copy_clipboard", TrailingIcon = "arrow_right", Variant = ButtonVariant.Secondary }
			),
			ComponentExample.Of(
				"Icon only",
				_Button.Create,
				new ButtonModel { Label = "Copy to clipboard", Icon = "copy_clipboard", IconOnly = true, Variant = ButtonVariant.Secondary }
			),
			ComponentExample.Of("Disabled", _Button.Create, new ButtonModel { Label = "Unavailable", Disabled = true })
		]
	);
}
