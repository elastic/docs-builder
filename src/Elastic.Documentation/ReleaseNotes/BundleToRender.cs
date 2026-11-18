// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// A <see cref="Bundle"/> handed to <c>changelog render</c>, with its amend files already merged in: the stage that is checked for what a render needs
/// (every entry inlined, once). The <see cref="Bundle"/> a loader reads for a site build is checked by the loader, which skips an entry it cannot
/// render and says so; this stage refuses it.
/// </summary>
public sealed record BundleToRender(Bundle Data) : IHaveConstraints<BundleToRender>
{
	public static ConstraintSet<BundleToRender> Constraints => BundleToRenderConstraints.All;
}

/// <summary>Every rule a <see cref="BundleToRender"/> must satisfy, in the order they are reported.</summary>
[Constraints<BundleToRender>]
public static partial class BundleToRenderConstraints
{
	/// <summary>
	/// One pass over the entries: for each, a file named twice is a warning, then an entry without inline content or without products is an error.
	/// The two are one rule because the render command has always reported them entry by entry.
	/// </summary>
	public static readonly Constraint<BundleToRender> BundleEntriesRenderable = Constraints<BundleToRender>.Check(
		b => Problems(b.Data)
	).Describe("Every entry of a bundle is inlined (a title, a type and products) and a changelog file appears once in it.");

	private static List<Violation> Problems(Bundle bundle)
	{
		var violations = new List<Violation>();
		var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var entry in bundle.Entries)
		{
			if (!string.IsNullOrWhiteSpace(entry.File?.Name) && !fileNames.Add(entry.File.Name))
				violations.Add(Violation.Warning($"Changelog file '{entry.File.Name}' appears multiple times in the same bundle"));

			if (!entry.HasInlineContent)
			{
				violations.Add(
					Violation.Error(
						$"Entry '{entry.DisplayName}' in bundle has no inline content: title and type are required. " +
							"Re-create the bundle with 'changelog bundle'."
					)
				);
			}
			else if (!entry.HasProducts)
				violations.Add(Violation.Error($"Entry '{entry.Title}' in bundle is missing required field: products"));
		}
		return violations;
	}
}
