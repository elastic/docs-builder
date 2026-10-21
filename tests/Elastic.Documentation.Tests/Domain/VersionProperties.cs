// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Versions;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// Service-algorithm laws of versions: <see cref="SemVersion"/> (parse and print, order), <see cref="VersionOrDate"/>, <see cref="VersionSpec"/> and the relation
/// of a <see cref="VersioningSystem"/>'s base and current. A fixed seed, 200 iterations.
/// </summary>
public class VersionProperties
{
	private static Gen<SemVersion> Versions { get; } = Gen.Select(
		Gen.Int[0, 12],
		Gen.Int[0, 12],
		Gen.Int[0, 12],
		Gen.OneOfConst("", "", "alpha", "alpha.1", "alpha.beta", "beta.2", "beta.11", "rc.1", "1", "2", "x-y"),
		Gen.OneOfConst("", "", "", "build.1", "sha.abc"),
		(major, minor, patch, prerelease, metadata) => new SemVersion(major, minor, patch, prerelease, metadata)
	);

	/// <summary>Versions without build metadata, which semver ignores for precedence.</summary>
	private static Gen<SemVersion> PlainVersions { get; } = Versions.Select(v => new SemVersion(v.Major, v.Minor, v.Patch, v.Prerelease));

	private static int Sign(int comparison) => Math.Sign(comparison);

	/// <summary>
	/// Law: a version printed and parsed again is the version that was printed, and a valid version text printed again is the text.
	/// Consequence if broken: a version written to a file is read back as another, and a release is filed under the wrong number.
	/// </summary>
	[Test]
	public void SemVersion_ParseAndPrint_RoundTrip() =>
		Versions.Check(version =>
		{
			var text = version.ToString();

			SemVersion.TryParse(text, out var parsed).Should().BeTrue(text);

			parsed.Should().Be(version);
			parsed!.ToString().Should().Be(text);
			((SemVersion)text).Should().Be(version);
		});

	/// <summary>
	/// Law: the order of versions is a total order: <c>CompareTo</c> is reflexive, antisymmetric (the sign flips with the arguments) and transitive,
	/// and the comparison operators agree with it.
	/// Consequence if broken: a sort of versions depends on the sort algorithm, and the latest version of a list is not always the same one.
	/// </summary>
	[Test]
	public void SemVersion_Order_IsTotal() =>
		Gen.Select(Versions, Versions, Versions).Check(x =>
		{
			var (a, b, c) = x;

			Sign(a.CompareTo(a)).Should().Be(0);
			Sign(a.CompareTo(b)).Should().Be(-Sign(b.CompareTo(a)), $"{a} against {b}");
			if (a <= b && b <= c)
				(a <= c).Should().BeTrue($"{a} <= {b} <= {c}");
			(a < b).Should().Be(Sign(a.CompareTo(b)) < 0);
			(a > b).Should().Be(Sign(a.CompareTo(b)) > 0);
			(a >= b).Should().Be(Sign(a.CompareTo(b)) >= 0);
			(a <= b).Should().Be(Sign(a.CompareTo(b)) <= 0);
		});

	/// <summary>
	/// Law: the order is consistent with equality: two versions compare as 0 exactly when they are equal, and equal versions have one hash code.
	/// Consequence if broken: a sorted set or a dictionary keyed by a version keeps or drops entries that a list of the same versions does not.
	/// </summary>
	[Test]
	public void SemVersion_Order_IsConsistentWithEquality() =>
		Gen.Select(Versions, Versions).Check(x =>
		{
			var (a, b) = x;

			(a.CompareTo(b) == 0).Should().Be(a.Equals(b), $"{a} against {b}");
			if (a.Equals(b))
				a.GetHashCode().Should().Be(b.GetHashCode());
		});

	/// <summary>
	/// The law that does hold without build metadata: the order is consistent with equality, and a prerelease precedes its release.
	/// Consequence if broken: even the versions of a release line sort wrongly.
	/// </summary>
	[Test]
	public void SemVersion_WithoutMetadata_OrdersByPrecedence() =>
		Gen.Select(PlainVersions, PlainVersions).Check(x =>
		{
			var (a, b) = x;

			(a.CompareTo(b) == 0).Should().Be(a.Equals(b), $"{a} against {b}");
			if (a.Prerelease.Length > 0)
				(a < new SemVersion(a.Major, a.Minor, a.Patch)).Should().BeTrue($"{a} precedes its release");
			if ((a.Major, a.Minor, a.Patch) != (b.Major, b.Minor, b.Patch))
				Sign(a.CompareTo(b)).Should().Be(Sign((a.Major, a.Minor, a.Patch).CompareTo((b.Major, b.Minor, b.Patch))));
		});

	private static Gen<string> VersionOrDateTexts { get; } = Gen.OneOfConst(
		"9.3.0",
		"9.10.1",
		"9.3.0-beta.1",
		"10.0.0",
		"2025-08-05",
		"2025-08",
		"2024-12-31",
		"nightly",
		"latest",
		"v1",
		""
	);

	/// <summary>
	/// Law: <see cref="VersionOrDate"/> orders mixed semver, date and free-text versions totally: antisymmetric and transitive.
	/// Consequence if broken: a product that moved from semver to dates lists its releases in an order that depends on where the sort began.
	/// </summary>
	[Test]
	public void VersionOrDate_Order_IsTotal() =>
		Gen.Select(VersionOrDateTexts, VersionOrDateTexts, VersionOrDateTexts).Check(x =>
		{
			var (a, b, c) = (VersionOrDate.Parse(x.Item1), VersionOrDate.Parse(x.Item2), VersionOrDate.Parse(x.Item3));

			Sign(a.CompareTo(b)).Should().Be(-Sign(b.CompareTo(a)), $"{x.Item1} against {x.Item2}");
			if (a.CompareTo(b) <= 0 && b.CompareTo(c) <= 0)
				a.CompareTo(c).Should().BeLessThanOrEqualTo(0, $"{x.Item1} <= {x.Item2} <= {x.Item3}");
		});

	private static Gen<VersionSpec> Specs
	{
		get;
	} = Gen.Select(Gen.Int[0, 3], Gen.Int[0, 12], Gen.Int[0, 12], Gen.Int[0, 12], Gen.Int[0, 12], Gen.Bool, Gen.Bool, (
		kind,
		major,
		minor,
		patch,
		max,
		showMin,
		showMax
	) =>
	{
		// the patch is printed only when it is shown, so a version whose patch is hidden has patch 0
		SemVersion Min() => new(major, minor, showMin ? patch : 0);

		SemVersion Max() => new(major + max % 3, minor + max, showMax ? patch : 0);

		return kind switch
		{
			0 => VersionSpec.GreaterThanOrEqual(Min(), showMin),
			1 => VersionSpec.Exact(Min(), showMin),
			_ => VersionSpec.Range(Min(), Max(), showMin, showMax)
		};
	});

	/// <summary>
	/// Law: a version spec printed and parsed again is the spec that was printed (kind, bounds and which patch is shown), and printing the parse of a printed
	/// spec gives the same text.
	/// Consequence if broken: an applies_to badge shows another version range than the one the author wrote.
	/// </summary>
	[Test]
	public void VersionSpec_ParseAndPrint_RoundTrip() =>
		Specs.Check(spec =>
		{
			var text = spec.ToString();

			VersionSpec.TryParse(text, out var parsed).Should().BeTrue(text);

			parsed.Should().Be(spec, text);
			parsed!.ToString().Should().Be(text);
		});

	private static Gen<string> SpecTexts { get; } = Gen.OneOfConst(
		"9.0",
		"9.0+",
		"9.0.4",
		"9.0.4+",
		"9.0.4!",
		"9.0.4!+",
		"=9.1",
		"=9.1.2!",
		"9.0-9.1",
		"9.0-9.1.2!",
		"9.0.1!-9.1.2!",
		"9.0.0-alpha",
		"9.0.0-beta.1+",
		"=9.0.0-rc.1",
		"9.0.0-alpha-9.1.0",
		"all",
		"x",
		"9"
	);

	/// <summary>
	/// Law: the printed form of a parsed spec is a fixed point: parsing it and printing again gives the same text, and what it means (kind, bounds) is kept.
	/// Consequence if broken: saving a docset's version twice keeps rewriting it.
	/// </summary>
	[Test]
	public void VersionSpec_PrintOfParse_IsAFixedPoint() =>
		SpecTexts.Check(text =>
		{
			if (!VersionSpec.TryParse(text, out var spec))
				return;

			var printed = spec.ToString();
			VersionSpec.TryParse(printed, out var again).Should().BeTrue($"{text} printed as {printed}");

			again!.ToString().Should().Be(printed);
			again.Kind.Should().Be(spec.Kind, $"{text} printed as {printed}");
			(again.Min.Major, again.Min.Minor).Should().Be((spec.Min.Major, spec.Min.Minor), $"{text} printed as {printed}");
		});

	/// <summary>
	/// Law: the order of specs (by the highest version each names) is a total preorder: antisymmetric and transitive, and equal specs compare as 0.
	/// Consequence if broken: the versions of a badge list sort differently on each build.
	/// </summary>
	[Test]
	public void VersionSpec_Order_IsATotalPreorder() =>
		Gen.Select(Specs, Specs, Specs).Check(x =>
		{
			var (a, b, c) = x;

			Sign(a.CompareTo(b)).Should().Be(-Sign(b.CompareTo(a)), $"{a} against {b}");
			if (a.CompareTo(b) <= 0 && b.CompareTo(c) <= 0)
				a.CompareTo(c).Should().BeLessThanOrEqualTo(0, $"{a} <= {b} <= {c}");
			if (a.Equals(b))
				a.CompareTo(b).Should().Be(0);
		});

	/// <summary>
	/// Law: the versioning system rule <c>BaseNotAfterCurrent</c> fires for a system exactly when its base is after its current version, and a system
	/// is versionless exactly when its current major is at or above the sentinel.
	/// Consequence if broken: a docs set can state a first version later than the latest release, and no rule says so.
	/// </summary>
	[Test]
	public void VersioningSystem_BaseAndCurrent_Relations() =>
		Gen.Select(PlainVersions, PlainVersions).Check(x =>
		{
			var (baseVersion, current) = x;
			var system = DomainGen.DefaultVersioning(VersioningSystemId.Stack) with { Base = baseVersion, Current = current };
			var catalog = new VersioningSystemCatalog
			{
				VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem> { [VersioningSystemId.Stack] = system }
			};

			var fires = VersioningSystemConstraints
				.All
				.Validate(catalog)
				.Any(v => v.ConstraintId == VersioningSystemConstraints.BaseNotAfterCurrent.Name);

			fires.Should().Be(baseVersion.CompareTo(current) > 0);
			system.IsVersionless.Should().Be(current.Major >= VersioningSystem.VersionlessSentinel);
		});
}
