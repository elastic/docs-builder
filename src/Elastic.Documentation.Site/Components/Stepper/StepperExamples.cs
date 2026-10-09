// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.Stepper;

public static class StepperExamples
{
	private static async Task<HtmlString> StepsAsync()
	{
		var steps = new[]
		{
			new StepModel
			{
				Title = "Install",
				Anchor = "ex-install",
				Body = new HtmlString("<p>Download the package for your platform.</p>")
			},
			new StepModel
			{
				Title = "Configure",
				Anchor = "ex-configure",
				Body = new HtmlString("<p>Set the cluster name and the network host.</p>")
			},
			new StepModel
			{
				Title = "Start",
				Anchor = "ex-start",
				Body = new HtmlString("<p>Run the service and check the health endpoint.</p>")
			}
		};
		var html = new System.Text.StringBuilder();
		foreach (var step in steps)
		{
			using var writer = new StringWriter();
			await _Step.Create(step).RenderAsync(writer);
			_ = html.Append(writer);
		}
		return new HtmlString(html.ToString());
	}

	public static ComponentSection Steppers { get; } = new(
		"Stepper",
		"Numbered steps with a connector. The numbers come from a CSS counter, so reordering steps needs no edits.",
		"stepper",
		[ComponentExample.Of("Three steps", _Stepper.Create, new StepperModel { Steps = StepsAsync().GetAwaiter().GetResult() })]
	);
}
