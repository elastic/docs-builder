// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Primitives.Kbd;

public static class KbdExamples
{
	public static ComponentSection Keys { get; } = new(
		"Keyboard key",
		"A single key or a chord. Use it for shortcuts in prose and in controls such as the search field.",
		"kbd",
		[
			ComponentExample.Of("Single key", _Kbd.Create, new KbdModel { Keys = ["Esc"] }),
			ComponentExample.Of("Chord", _Kbd.Create, new KbdModel { Keys = ["Ctrl", "K"] })
		]
	);
}
