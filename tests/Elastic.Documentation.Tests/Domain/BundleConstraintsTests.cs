// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using CsCheck;
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

	/// <summary>
	/// Generated bundles that satisfy what the model promises: a changelog file named once, a release date written the way it parses, a loaded bundle
	/// named after its repo, first product and target, a version and a file once across the loaded bundles, and an amend that removes what its parent has and adds what it does not.
	/// </summary>
	public static Gen<T> Generated<T>() =>
		GenAuto.Of<T>(
			AutoMode.Plausible,
			o => o
				.Identity<BundledFile, string>(f => f.Name)
				.Normalize<Bundle>(b => b with { ReleaseDateRaw = b.ReleaseDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) })
				.Normalize<LoadedBundle>(
					b => b.Data.Products is [{ Target: { } target } product, ..]
						? b with { FilePath = $"/bundles/{b.Repo}-{product.ProductId}-{target}.yaml" }
						: b
				)
				.Normalize<LoadedBundles>(
					m =>
						m with
						{
							Bundles =
							[
								.. m.Bundles.Select(
									(b, i) =>
										b with
										{
											Version = $"9.{i}.0",
											FilePath = b.FilePath.Replace(".yaml", $"-{i}.yaml", StringComparison.Ordinal),
											Data = b.Data with
											{
												Entries =
												[
													.. b
														.Data
														.Entries
														.Select(
															(e, j) =>
																e with
																{
																	File = new BundledFile
																	{
																		Name = $"b{i}-{j}.yaml",
																		Checksum = e.File?.Checksum ?? ""
																	}
																}
														)
												]
											}
										}
								)
							]
						}
				)
				.Normalize<BundleAmendOfParent>(
					m =>
						m with
						{
							Amend = m.Amend with
							{
								ExcludeEntries =
								[
									.. m.Parent.Entries.Take(m.Amend.ExcludeEntries.Count).Select(e => new BundledEntry { File = e.File })
								],
								Entries =
								[
									.. m
										.Amend
										.Entries
										.Select((e, i) => e with { File = new BundledFile { Name = $"added-{i}.yaml", Checksum = "x" } })
								]
							}
						}
				)
		);
}

public class BundleConstraintsTests
{
	[Test]
	public void Laws() => BundleConstraints.All.Laws(BundleExamples.Generated<Bundle>(), CounterExamples);

	private static IReadOnlyDictionary<string, Func<Bundle, Bundle>> CounterExamples =>
		new Dictionary<string, Func<Bundle, Bundle>>
		{
			["bundle-product-single-target"] =
				b => b with { Products = [new BundledProduct("elasticsearch", "9.0.0"), new BundledProduct("elasticsearch", "9.1.0")] },
			["bundle-release-date-parses"] = b => b with { ReleaseDateRaw = "2025-13-45" },
			["bundle-entries-inlined"] =
				b => b with { Entries = [.. b.Entries, new BundledEntry { File = new BundledFile { Name = "3.yaml", Checksum = "c" } }] },
			["bundle-entry-files-unique"] =
				b => b with { Entries = [.. b.Entries, BundleExamples.Entry("dup.yaml"), BundleExamples.Entry("dup.yaml")] }
		};
}

public class BundleAmendConstraintsTests
{
	[Test]
	public void Laws() => BundleAmendConstraints.All.Laws(BundleExamples.Generated<BundleAmendOfParent>(), CounterExamples);

	private static BundledEntry Removal(string name, string checksum) =>
		new() { File = new BundledFile { Name = name, Checksum = checksum } };

	// the parent gets the entry the amend then removes or adds, so each case stands on its own whatever the generated parent holds
	private static BundleAmendOfParent WithParentEntry(BundleAmendOfParent m, string name, string checksum) =>
		m with { Parent = m.Parent with { Entries = [.. m.Parent.Entries, BundleExamples.Entry(name, checksum)] } };

	private static IReadOnlyDictionary<string, Func<BundleAmendOfParent, BundleAmendOfParent>> CounterExamples =>
		new Dictionary<string, Func<BundleAmendOfParent, BundleAmendOfParent>>
		{
			["amend-removals-exist"] = m => m with { Amend = m.Amend with { ExcludeEntries = [Removal("9.yaml", "")] } },
			["amend-removal-checksums-match"] = m =>
			{
				var c = WithParentEntry(m, "1.yaml", "abc");
				return c with { Amend = c.Amend with { ExcludeEntries = [Removal("1.yaml", "different")] } };
			},
			["amend-added-entries-new"] = m =>
			{
				var c = WithParentEntry(m, "2.yaml", "def");
				return c with { Amend = c.Amend with { Entries = [BundleExamples.Entry("2.yaml", "def")] } };
			}
		};
}

public class LoadedBundleConstraintsTests
{
	[Test]
	public void Laws() => LoadedBundleConstraints.All.Laws(BundleExamples.Generated<LoadedBundle>(), CounterExamples);

	private static IReadOnlyDictionary<string, Func<LoadedBundle, LoadedBundle>> CounterExamples =>
		new Dictionary<string, Func<LoadedBundle, LoadedBundle>>
		{
			["bundle-filename-convention"] =
				b =>
					b with
					{
						Data = b.Data with { Products = [new BundledProduct("elasticsearch", "9.0.0")] },
						FilePath = "/bundles/changelog-bundle.yaml"
					},
			// no target: the file name is the version, so the convention cannot be checked and only this rule reports
			["bundle-target-present"] = b => b with { Data = b.Data with { Products = [new BundledProduct("elasticsearch")] } }
		};
}

public class LoadedBundlesConstraintsTests
{
	[Test]
	public void Laws() => LoadedBundlesConstraints.All.Laws(BundleExamples.Generated<LoadedBundles>(), CounterExamples);

	private static IReadOnlyDictionary<string, Func<LoadedBundles, LoadedBundles>> CounterExamples =>
		new Dictionary<string, Func<LoadedBundles, LoadedBundles>>
		{
			["bundles-target-unique"] =
				m =>
					m with
					{
						Bundles =
						[
							.. m.Bundles,
							BundleExamples.Loaded(
								BundleExamples.Bundle(BundleExamples.Entry("x1.yaml")),
								"99.0.0",
								"/bundles/a-99.0.0.yaml"
							),
							BundleExamples.Loaded(
								BundleExamples.Bundle(BundleExamples.Entry("x2.yaml")),
								"99.0.0",
								"/bundles/b-99.0.0.yaml"
							)
						]
					},
			["bundles-entry-files-unique"] =
				m =>
					m with
					{
						Bundles =
						[
							.. m.Bundles,
							BundleExamples.Loaded(BundleExamples.Bundle(BundleExamples.Entry("shared.yaml")), "98.0.0"),
							BundleExamples.Loaded(
								BundleExamples.Bundle(BundleExamples.Entry("shared.yaml")),
								"99.0.0",
								"/bundles/other-99.0.0.yaml"
							)
						]
					}
		};
}
