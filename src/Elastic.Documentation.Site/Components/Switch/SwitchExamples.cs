// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Switch;

public static class SwitchExamples
{
	public static ComponentSection Switches { get; } = new(
		"Switch",
		"A checkbox with role=switch for settings that take effect immediately.",
		"switch",
		[
			ComponentExample.Of("Off", _Switch.Create, new SwitchModel { Id = "ex-s1", Name = "dark", Label = "Dark mode" }),
			ComponentExample.Of("On", _Switch.Create, new SwitchModel { Id = "ex-s2", Name = "alerts", Label = "Alerts", On = true }),
			ComponentExample.Of(
				"Disabled",
				_Switch.Create,
				new SwitchModel { Id = "ex-s3", Name = "sync", Label = "Sync", Disabled = true }
			)
		]
	);
}
