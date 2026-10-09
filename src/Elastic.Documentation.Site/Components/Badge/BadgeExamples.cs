// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Badge;

public static class BadgeExamples
{
	public static ComponentSection Badges { get; } = new(
		"Badge",
		"One badge for every status, tag and label. The tone carries the meaning; the shape carries the weight.",
		"badge",
		[
			ComponentExample.Of("Neutral", _Badge.Create, new BadgeModel { Text = "Draft" }),
			ComponentExample.Of("Info", _Badge.Create, new BadgeModel { Text = "Beta", Tone = Tone.Info }),
			ComponentExample.Of("Success", _Badge.Create, new BadgeModel { Text = "Stable", Tone = Tone.Success, Icon = "check" }),
			ComponentExample.Of("Warning", _Badge.Create, new BadgeModel { Text = "Preview", Tone = Tone.Warning }),
			ComponentExample.Of("Danger", _Badge.Create, new BadgeModel { Text = "Deprecated", Tone = Tone.Danger }),
			ComponentExample.Of("Important", _Badge.Create, new BadgeModel { Text = "Breaking", Tone = Tone.Important }),
			ComponentExample.Of(
				"Pill",
				_Badge.Create,
				new BadgeModel { Text = "Elasticsearch", Tone = Tone.Info, Shape = BadgeShape.Pill }
			),
			ComponentExample.Of("Tag", _Badge.Create, new BadgeModel { Text = "New", Shape = BadgeShape.Tag }),
			ComponentExample.Of("Monospace status", _Badge.Create, new BadgeModel { Text = "200 OK", Tone = Tone.Success, Mono = true })
		]
	);
}
