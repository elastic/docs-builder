// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.AppliesSwitch;

public class AppliesSwitchViewModel : DirectiveViewModel
{
	public required bool IsDropdown { get; init; }

	/// Item view models for the selector labels; only populated for the dropdown
	/// appearance, where the switch view renders all inputs and labels itself so
	/// they can be grouped into a single overlay menu.
	public required IReadOnlyList<AppliesItemViewModel> Items { get; init; }
}
