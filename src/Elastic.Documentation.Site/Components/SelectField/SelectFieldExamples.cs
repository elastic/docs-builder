// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.SelectField;

public static class SelectFieldExamples
{
	public static ComponentSection Selects { get; } = new(
		"Select",
		"A native select, so the option list, keyboard handling and mobile pickers come from the browser. Only the closed control is styled.",
		"select",
		[
			ComponentExample.Of(
				"Version",
				_SelectField.Create,
				new SelectFieldModel
				{
					Id = "ex-version",
					Name = "version",
					Label = "Version",
					Options = [new("9.0", "9.0+ (Current)", true), new("8.19", "8.19"), new("7.17", "7.17")]
				}
			),
			ComponentExample.Of(
				"Disabled",
				_SelectField.Create,
				new SelectFieldModel
				{
					Id = "ex-lang",
					Name = "lang",
					Label = "Language",
					Disabled = true,
					Options = [new("en", "English", true)]
				}
			)
		]
	);
}
