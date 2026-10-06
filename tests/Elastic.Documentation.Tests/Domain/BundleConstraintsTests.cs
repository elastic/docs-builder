// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ReleaseNotes;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

internal static class BundleExamples
{
	public static BundledEntry Entry(string file, string checksum = "abc", string title = "Add a thing") =>
		new()
		{
			File = new BundledFile { Name = file, Checksum = checksum },
			Type = ChangelogEntryType.Feature,
			Title = title,
			Products = [new ProductReference { ProductId = "elasticsearch" }]
		};

	public static Bundle Bundle(params BundledEntry[] entries) =>
		new() { Products = [new BundledProduct("elasticsearch", "9.0.0")], Entries = entries };

	public static LoadedBundle Loaded(Bundle data, string version = "9.0.0", string? path = null) =>
		new(version, "elasticsearch", "elastic", data, path ?? $"/bundles/elasticsearch-elasticsearch-{version}.yaml", []);
}

public class BundleConstraintsTests
{
	[Test]
	public void Laws() => BundleConstraints.All.Laws(Valid(), CounterExamples);

	private static Bundle Valid() => BundleExamples.Bundle(BundleExamples.Entry("1.yaml"), BundleExamples.Entry("2.yaml"));

	private static IReadOnlyDictionary<string, Func<Bundle, Bundle>> CounterExamples =>
		new Dictionary<string, Func<Bundle, Bundle>>
		{
			["bundle-product-single-target"] =
				b => b with { Products = [new BundledProduct("elasticsearch", "9.0.0"), new BundledProduct("elasticsearch", "9.1.0")] },
			["bundle-release-date-parses"] = b => b with { ReleaseDateRaw = "2025-13-45" },
			["bundle-entries-inlined"] =
				b => b with { Entries = [.. b.Entries, new BundledEntry { File = new BundledFile { Name = "3.yaml", Checksum = "c" } }] },
			["bundle-entry-files-unique"] = b => b with { Entries = [.. b.Entries, BundleExamples.Entry("1.yaml")] }
		};
}

public class BundleAmendConstraintsTests
{
	[Test]
	public void Laws() => BundleAmendConstraints.All.Laws(Valid(), CounterExamples);

	private static BundleAmendOfParent Valid() =>
		new(
			BundleExamples.Bundle(BundleExamples.Entry("1.yaml", "abc"), BundleExamples.Entry("2.yaml", "def")),
			new Bundle
			{
				ExcludeEntries = [new BundledEntry { File = new BundledFile { Name = "1.yaml", Checksum = "abc" } }],
				Entries = [BundleExamples.Entry("3.yaml")]
			}
		);

	private static BundleAmendOfParent WithExclusion(BundleAmendOfParent m, string name, string checksum) =>
		m with
		{
			Amend = m.Amend with { ExcludeEntries = [new BundledEntry { File = new BundledFile { Name = name, Checksum = checksum } }] }
		};

	private static IReadOnlyDictionary<string, Func<BundleAmendOfParent, BundleAmendOfParent>> CounterExamples =>
		new Dictionary<string, Func<BundleAmendOfParent, BundleAmendOfParent>>
		{
			["amend-removals-exist"] = m => WithExclusion(m, "9.yaml", ""),
			["amend-removal-checksums-match"] = m => WithExclusion(m, "1.yaml", "different"),
			["amend-added-entries-new"] = m => m with { Amend = m.Amend with { Entries = [BundleExamples.Entry("2.yaml", "def")] } }
		};
}

public class LoadedBundleConstraintsTests
{
	[Test]
	public void Laws() => LoadedBundleConstraints.All.Laws(Valid(), CounterExamples);

	private static LoadedBundle Valid() => BundleExamples.Loaded(BundleExamples.Bundle());

	private static IReadOnlyDictionary<string, Func<LoadedBundle, LoadedBundle>> CounterExamples =>
		new Dictionary<string, Func<LoadedBundle, LoadedBundle>>
		{
			["bundle-filename-convention"] = b => b with { FilePath = "/bundles/changelog-bundle.yaml" },
			// no target: the file name is the version, so the convention cannot be checked and only this rule reports
			["bundle-target-present"] = b => b with { Data = b.Data with { Products = [new BundledProduct("elasticsearch")] } }
		};
}

public class LoadedBundlesConstraintsTests
{
	[Test]
	public void Laws() => LoadedBundlesConstraints.All.Laws(Valid(), CounterExamples);

	private static LoadedBundles Valid() =>
		new([
			BundleExamples.Loaded(BundleExamples.Bundle(BundleExamples.Entry("1.yaml")), "9.0.0"),
			BundleExamples.Loaded(BundleExamples.Bundle(BundleExamples.Entry("2.yaml")), "9.1.0")
		]);

	private static IReadOnlyDictionary<string, Func<LoadedBundles, LoadedBundles>> CounterExamples =>
		new Dictionary<string, Func<LoadedBundles, LoadedBundles>>
		{
			["bundles-target-unique"] =
				m =>
					m with
					{
						Bundles =
						[
							m.Bundles[0],
							BundleExamples.Loaded(
								BundleExamples.Bundle(BundleExamples.Entry("2.yaml")),
								"9.0.0",
								"/bundles/other-9.0.0.yaml"
							)
						]
					},
			["bundles-entry-files-unique"] =
				m =>
					m with
					{
						Bundles = [m.Bundles[0], BundleExamples.Loaded(BundleExamples.Bundle(BundleExamples.Entry("1.yaml")), "9.1.0")]
					}
		};
}
