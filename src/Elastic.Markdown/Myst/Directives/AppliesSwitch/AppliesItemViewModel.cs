// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration;

namespace Elastic.Markdown.Myst.Directives.AppliesSwitch;

public class AppliesItemViewModel : DirectiveViewModel
{
	public required int Index { get; init; }
	public required int AppliesSwitchIndex { get; init; }
	public required string AppliesToDefinition { get; init; }
	public required ApplicableTo? AppliesTo { get; init; }
	public required string? SyncKey { get; init; }
	public required string? AppliesSwitchGroupKey { get; init; }
	public required BuildContext BuildContext { get; init; }
}
