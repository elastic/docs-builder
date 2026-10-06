// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;
using Nullean.Entities;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>Every rule a <see cref="LoadedBundle"/> must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class LoadedBundleConstraints
{
	public static Constraint<LoadedBundle> BundleFilenameConvention { get; } = Constraints<LoadedBundle>
		.Each(
			m => m.Data.Products.Take(1),
			(m, product) => !string.IsNullOrWhiteSpace(product.Target) &&
				!string.Equals(
					FileName(m.FilePath),
					$"{m.Repo}-{product.ProductId}-{product.Target}.yaml",
					StringComparison.OrdinalIgnoreCase
				)
				? $"Bundle file '{FileName(m.FilePath)}' does not follow the naming convention '{{repo}}-{{product}}-{{version}}.yaml' " +
					$"('{m.Repo}-{product.ProductId}-{product.Target}.yaml'); the fallbacks are '{{product}}-{{version}}.yaml' and 'changelog-bundle.yaml'."
				: null
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"R66: A bundle file is named {repo}-{product}-{version}.yaml; the generator falls back to {product}-{version}.yaml and then changelog-bundle.yaml, with a warning."
		);

	public static Constraint<LoadedBundle> BundleTargetPresent { get; } = Constraints<LoadedBundle>
		.Each(
			m => m.Data.Products,
			(m, p) => string.IsNullOrWhiteSpace(p.Target)
				? $"Product '{p.ProductId}' of bundle '{FileName(m.FilePath)}' has no target; the file name is used as its version."
				: null
		)
		.WithSeverity(Severity.Warning)
		.Describe("R87: Every product of a bundle has a target; without one the file name is the version.");

	internal static string FileName(string path)
	{
		var normalized = path.Replace('\\', '/');
		var slash = normalized.LastIndexOf('/');
		return slash >= 0 ? normalized[(slash + 1)..] : normalized;
	}
}

/// <summary>The bundles loaded from one folder (or one CDN listing), which the rules across bundles hold to account.</summary>
[Equatable]
public sealed partial record LoadedBundles([property: OrderedEquality] IReadOnlyList<LoadedBundle> Bundles);

/// <summary>Every rule across the <see cref="LoadedBundle"/>s of a <see cref="LoadedBundles"/>, in the order they are reported.</summary>
[Constraints]
public static partial class LoadedBundlesConstraints
{
	public static Constraint<LoadedBundles> BundlesTargetUnique { get; } = Constraints<LoadedBundles>
		.Unique(
			m => m.Bundles,
			b => b.Version,
			StringComparer.Ordinal,
			message: "Several bundles share a target version; the loader merges them (descriptions joined, first release date wins) and the render CLI warns and drops all but one."
		)
		.WithSeverity(Severity.Warning)
		.Describe("R88: Each target version has one bundle (stage dependent: the loader merges silently, the render CLI warns and drops).");

	public static Constraint<LoadedBundles> BundlesEntryFilesUnique { get; } = Constraints<LoadedBundles>
		.Consistent(
			m => m.Bundles.SelectMany(
				b => b
					.Data
					.Entries
					.Where(e => !string.IsNullOrWhiteSpace(e.File?.Name))
					.Select(e => (File: e.File!.Name, Bundle: b.FilePath))
			),
			x => x.File,
			x => x.Bundle,
			StringComparer.OrdinalIgnoreCase,
			StringComparer.Ordinal,
			describe: (file, bundles) => $"Changelog file '{file}' appears in multiple bundles: {string.Join(", ", bundles)}"
		)
		.WithSeverity(Severity.Warning)
		.Describe("R89: A changelog file appears in one bundle, not several (render CLI only).");
}
