// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Entities;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class BundleConstraintsTests
{
	[Test]
	public void Laws() => BundleConstraints.All.Laws(BundleExamples.Generated<Bundle>(), CounterExamples);

	private static IReadOnlyDictionary<string, Func<Bundle, Bundle>> CounterExamples =>
		new Dictionary<string, Func<Bundle, Bundle>>
		{
			["bundle-product-single-target"] =
				b => b with { Products = [new BundledProduct("elasticsearch", "9.0.0"), new BundledProduct("elasticsearch", "9.1.0")] },
			["bundle-has-one-product"] =
				b => b with { Products = [new BundledProduct("elasticsearch", "9.0.0"), new BundledProduct("kibana", "9.0.0")] },
			["bundle-release-date-parses"] = b => b with { ReleaseDateAuthored = Authored.Of<DateOnly?>("2025-13-45", null) },
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

	[Test]
	public void FilenameConvention_ChecksEveryProductAndNamesTheOneThatMismatches() =>
		BundleExamples.Generated<LoadedBundle>().Sample(
			b =>
			{
				var bundle = b with
				{
					Data = b.Data with { Products = [new BundledProduct("elasticsearch", "9.0.0"), new BundledProduct("kibana", "9.0.0")] },
					FilePath = $"/bundles/{b.Repo}-elasticsearch-9.0.0.yaml"
				};

				var violations = LoadedBundleConstraints.All.Report(bundle).Of(LoadedBundleConstraints.BundleFilenameConvention);

				var violation = violations.Should().ContainSingle().Subject;
				violation.Message.Should().Contain("product 'kibana'").And.Contain($"{b.Repo}-kibana-9.0.0.yaml");
			},
			iter: 20
		);

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
