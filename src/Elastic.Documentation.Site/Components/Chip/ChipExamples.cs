// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Chip;

public static class ChipExamples
{
	public static ComponentSection Chips { get; } = new(
		"Chip",
		"A toggle for filters. It is a checkbox underneath, so a group of chips submits as a multi-select.",
		"chip",
		[
			ComponentExample.Of("Off", _Chip.Create, new ChipModel { Id = "ex-ch1", Name = "type", Value = "guide", Label = "Guides" }),
			ComponentExample.Of(
				"Selected",
				_Chip.Create,
				new ChipModel { Id = "ex-ch2", Name = "type", Value = "ref", Label = "Reference", Selected = true }
			)
		]
	);
}
