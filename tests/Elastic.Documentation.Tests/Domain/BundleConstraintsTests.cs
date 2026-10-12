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
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest.For(BundleConstraints.All, BundleExamples.Generated<Bundle>()).CounterExamples(CounterExamples).Axes();

	private static IReadOnlyDictionary<string, Func<Bundle, Bundle>> CounterExamples => new Dictionary<string, Func<Bundle, Bundle>> { };
}

public class BundleAmendConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest
			.For(BundleAmendConstraints.All, BundleExamples.Generated<BundleAmendOfParent>())
			.CounterExamples(CounterExamples)
			.Axes();

	private static BundledEntry Removal(string name, string checksum) =>
		new() { File = new BundledFile { Name = name, Checksum = checksum } };

	// the parent gets the entry the amend then removes or adds, so each case stands on its own whatever the generated parent holds
	private static BundleAmendOfParent WithParentEntry(BundleAmendOfParent m, string name, string checksum) =>
		m with { Parent = m.Parent with { Entries = [.. m.Parent.Entries, BundleExamples.Entry(name, checksum)] } };

	private static IReadOnlyDictionary<string, Func<BundleAmendOfParent, BundleAmendOfParent>> CounterExamples =>
		new Dictionary<string, Func<BundleAmendOfParent, BundleAmendOfParent>> { };
}

public class LoadedBundleConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest.For(LoadedBundleConstraints.All, BundleExamples.Generated<LoadedBundle>()).CounterExamples(CounterExamples).Axes();

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
		new Dictionary<string, Func<LoadedBundle, LoadedBundle>> { };
}

public class LoadedBundlesConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest.For(LoadedBundlesConstraints.All, BundleExamples.Generated<LoadedBundles>()).CounterExamples(CounterExamples).Axes();

	private static IReadOnlyDictionary<string, Func<LoadedBundles, LoadedBundles>> CounterExamples =>
		new Dictionary<string, Func<LoadedBundles, LoadedBundles>>
		{
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
