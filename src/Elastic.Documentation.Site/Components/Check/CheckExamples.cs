// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Check;

public static class CheckExamples
{
	public static ComponentSection Checks { get; } = new(
		"Checkbox and radio",
		"Native inputs with a styled box. Selection, keyboard use and form submission are the browser's; the checked state is pure CSS.",
		"check",
		[
			ComponentExample.Of(
				"Checkbox",
				_Check.Create,
				new CheckModel { Id = "ex-c1", Name = "newsletter", Label = "Email me release notes" }
			),
			ComponentExample.Of(
				"Checked",
				_Check.Create,
				new CheckModel { Id = "ex-c2", Name = "terms", Label = "I agree", Checked = true }
			),
			ComponentExample.Of(
				"Radio",
				_Check.Create,
				new CheckModel { Id = "ex-r1", Name = "plan", Value = "cloud", Label = "Cloud", Radio = true, Checked = true }
			),
			ComponentExample.Of(
				"Disabled",
				_Check.Create,
				new CheckModel { Id = "ex-c3", Name = "beta", Label = "Beta features", Disabled = true }
			)
		]
	);
}
