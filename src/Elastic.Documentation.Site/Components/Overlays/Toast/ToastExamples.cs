// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Overlays.Toast;

public static class ToastExamples
{
	public static ComponentSection Toasts { get; } = new(
		"Toast",
		"A short message with a coloured edge. Warnings and errors use role alert so they interrupt; the rest use role status. The dismiss button needs script and is hidden without it.",
		"toast",
		[
			ComponentExample.Of("Info", _Toast.Create, new ToastModel { Message = "Your draft was saved." }),
			ComponentExample.Of(
				"Success",
				_Toast.Create,
				new ToastModel { Message = "Copied to the clipboard.", Kind = ToastKind.Success }
			),
			ComponentExample.Of(
				"Danger",
				_Toast.Create,
				new ToastModel { Message = "The build failed. See the console.", Kind = ToastKind.Danger }
			)
		]
	);
}
