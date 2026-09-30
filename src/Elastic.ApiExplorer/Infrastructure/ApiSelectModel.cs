// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Infrastructure;

public sealed record ApiSelectOption(string Label, string Value, bool Selected);

/// <summary>Custom details dropdown used for language and example pickers.</summary>
public sealed record ApiSelectModel
{
	public required string AriaLabel { get; init; }
	public required IReadOnlyList<ApiSelectOption> Options { get; init; }
	public string ExtraClass { get; init; } = "";
	public string? Id { get; init; }
	public string? SyncGroup { get; init; }

	public ApiSelectOption Current => Options.FirstOrDefault(static o => o.Selected) ?? Options[0];
}
