// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.Navigation.LinkMenu;

namespace Elastic.Documentation.Site.Components.Forms.SplitButton;

public static class SplitButtonExamples
{
	public static ComponentSection SplitButtons { get; } = new(
		"Split button",
		"A primary action with related actions in a menu beside it.",
		"split-button",
		[
			ComponentExample.Of(
				"Default",
				_SplitButton.Create,
				new SplitButtonModel
				{
					Label = "Deploy",
					Href = "#deploy",
					MenuLabel = "More deploy options",
					Items = [new("Deploy to staging", "#staging"), new("Schedule deploy", "#schedule")]
				}
			)
		]
	);
}
