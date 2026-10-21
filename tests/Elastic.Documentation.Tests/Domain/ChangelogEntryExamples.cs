// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using CsCheck;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

internal static class ChangelogEntryExamples
{
	/// <summary>
	/// A generated entry as the loader hands it over: a title and type, at least one product with an id, a file named for the PR it declares,
	/// a subtype only on a breaking change, and not a marker (a marker is the other shape, and has its own counter-examples).
	/// </summary>
	public static ChangelogEntry Valid(ChangelogEntry e)
	{
		var title = e.Title.Length > 80 ? e.Title[..80] : e.Title;
		var type = e.Type == ChangelogEntryType.Invalid ? ChangelogEntryType.Feature : e.Type;
		var pr = (title.Length % 90) + 1;
		return e with
		{
			Link = null,
			SourceRedirect = false,
			Title = string.IsNullOrWhiteSpace(title) ? "A title" : title,
			Type = type,
			Subtype = type == ChangelogEntryType.BreakingChange ? e.Subtype : null,
			Description = e.Description is { Length: > 600 } d ? d[..600] : e.Description,
			Products =
			[
				.. (e.Products is { Count: > 0 } ? e.Products : [ProductReference.Of("elasticsearch")]).Select(
					p =>
						p with
						{
							TargetRaw = null,
							ProductId = string.IsNullOrWhiteSpace(p.ProductId) ? "elasticsearch" : p.ProductId.Replace('_', '-')
						}
				)
			],
			FileName = $"{pr}-change.yaml",
			Pr = pr.ToString(CultureInfo.InvariantCulture)
		};
	}

	public static Gen<T> Generated<T>(Action<AutoOptions>? configure = null) =>
		GenAuto.Of<T>(AutoMode.Plausible, o =>
		{
			o.Normalize<ChangelogEntry>(Valid);
			configure?.Invoke(o);
		});
}
