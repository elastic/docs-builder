// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Creation;
using Elastic.Documentation.Configuration.Changelog;

namespace Elastic.Changelog;

/// <summary>
/// Resolves the areas of an entry from GitHub labels and applies <c>pivot.required</c> for areas,
/// the same way in every command.
/// </summary>
public static class AreaResolution
{
	/// <summary>The areas that the labels map to (<c>pivot.areas</c>). Empty when no label maps to an area.</summary>
	public static List<string> FromLabels(string[] labels, ChangelogConfiguration config) =>
		config.LabelToAreas is { Count: > 0 } mapping ? PrInfoProcessor.MapLabelsToAreas(labels, mapping) : [];

	/// <summary>True when <c>pivot.required</c> lists <c>areas</c>: a pull request or issue without an area label is an error.</summary>
	public static bool RequiresAreaLabel(ChangelogConfiguration config) => config.RequiredPivots.HasFlag(RequiredPivot.Areas);

	/// <summary>The error for <paramref name="subject"/> without an area label. It lists the labels of <c>pivot.areas</c>.</summary>
	public static string MissingAreaLabelMessage(string subject, ChangelogConfiguration config) =>
		$"No matching area label found on {subject}. Add one of the following labels: {string.Join(", ", config.LabelToAreas?.Keys ?? [])}.";
}
