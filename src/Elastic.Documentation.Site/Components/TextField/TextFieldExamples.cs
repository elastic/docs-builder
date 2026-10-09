// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.TextField;

public static class TextFieldExamples
{
	public static ComponentSection Fields { get; } = new(
		"Text field",
		"A label, a native input and optional hint and error text. The error sets aria-invalid and is announced to screen readers.",
		"input",
		[
			ComponentExample.Of(
				"Default",
				_TextField.Create,
				new TextFieldModel { Id = "ex-name", Name = "name", Label = "Name", Placeholder = "Ada Lovelace" }
			),
			ComponentExample.Of(
				"With hint",
				_TextField.Create,
				new TextFieldModel { Id = "ex-email", Name = "email", Label = "Email", Type = "email", Hint = "We only use this to reply." }
			),
			ComponentExample.Of(
				"Error",
				_TextField.Create,
				new TextFieldModel
				{
					Id = "ex-key",
					Name = "key",
					Label = "API key",
					Value = "abc",
					Error = "Keys are at least 20 characters."
				}
			),
			ComponentExample.Of(
				"Warning",
				_TextField.Create,
				new TextFieldModel
				{
					Id = "ex-index",
					Name = "name",
					Label = "Index name",
					Value = "Logs",
					Warning = "Uppercase letters are not allowed in index names."
				}
			),
			ComponentExample.Of(
				"Success",
				_TextField.Create,
				new TextFieldModel { Id = "ex-slug", Name = "slug", Label = "Slug", Value = "my-logs", Success = "Available." }
			),
			ComponentExample.Of(
				"Disabled",
				_TextField.Create,
				new TextFieldModel { Id = "ex-region", Name = "region", Label = "Region", Value = "eu-west-1", Disabled = true }
			)
		]
	);
}
