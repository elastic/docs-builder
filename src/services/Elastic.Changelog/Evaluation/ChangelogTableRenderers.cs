// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Evaluation;

/// <summary>
/// Shared GFM table renderers used by <see cref="ChangelogPrEvaluationService"/>,
/// <see cref="ChangelogLabelValidationService"/>, and <see cref="ChangelogCommentRenderer"/>.
/// </summary>
internal static class ChangelogTableRenderers
{
	internal static string BuildLabelTable(IReadOnlyDictionary<string, string>? labelToType) =>
		BuildMappingTable(labelToType, "Label", "Type");

	/// <summary>
	/// Returns a comma-separated list of label keys for inline rendering in PR comments.
	/// </summary>
	internal static string BuildLabelKeys(IReadOnlyDictionary<string, string>? labelToType) =>
		labelToType is { Count: > 0 } ? string.Join(",", labelToType.Keys) : "";

	internal static string BuildProductLabelTable(IReadOnlyDictionary<string, string>? labelToProducts) =>
		BuildMappingTable(labelToProducts, "Label", "Product");

	internal static string BuildMappingTable(IReadOnlyDictionary<string, string>? mapping, string keyHeader, string valueHeader)
	{
		if (mapping is not { Count: > 0 })
			return "";

		var lines = new List<string> { $"| {keyHeader} | {valueHeader} |", "| --- | --- |" };
		foreach (var (key, value) in mapping)
			lines.Add($"| `{EscapePipe(key)}` | {EscapePipe(value)} |");

		return string.Join("\n", lines);
	}

	/// <summary>Escapes pipe characters inside GFM table cells.</summary>
	private static string EscapePipe(string value) => value.Replace("|", "\\|");
}
