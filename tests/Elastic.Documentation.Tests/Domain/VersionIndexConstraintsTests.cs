// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.OpenApi;
using Nullean.Entities;

namespace Elastic.Documentation.Tests.Domain;

public class VersionIndexConstraintsTests
{
	private static VersionIndex Index(params (string Key, string Version)[] monikers) =>
		new([
			new VersionIndexRepository(
				"elastic/api",
				[new VersionIndexSpec("openapi.json", [.. monikers.Select(m => new VersionMoniker(m.Key, m.Version))])]
			)
		]);

	private static IReadOnlyList<(string? Id, Severity Severity)> Violations(VersionIndex index) =>
		[.. VersionIndex.Constraints.Report(index).Violations.Select(v => (v.ConstraintId, v.Severity))];

	[Test]
	public void AMainAndMajorsThatMatchTheirVersions_BreakNothing() =>
		Violations(Index(("main", "main"), ("9", "9.5"), ("8", "8.19"))).Should().BeEmpty();

	[Test]
	public void MasterIsAVersionOfMain() => Violations(Index(("main", "master"))).Should().BeEmpty();

	[Test]
	public void AMonikerThatIsNeitherMainNorAMajor_IsAWarning() =>
		Violations(Index(("latest", "9.1"))).Should().ContainSingle().Which.Severity.Should().Be(Severity.Warning);

	[Test]
	[Arguments("main", "9.1")]
	[Arguments("8", "9.1")]
	[Arguments("8", "8")]
	[Arguments("8", "8.x")]
	public void AKeyThatDoesNotMatchItsVersion_IsAnError(string key, string version) =>
		Violations(Index((key, version))).Should().ContainSingle().Which.Severity.Should().Be(Severity.Error);

	/// <summary>
	/// Law: an index whose monikers are main or majors, each resolving to a release of its own major, breaks no rule, whatever its repositories and spec files are called.
	/// Consequence if broken: the index the lambda builds would be flagged by the model that reads it.
	/// </summary>
	[Test]
	public void AnIndexOfMainAndMajorsThatMatch_BreaksNothing() =>
		Gen.Select(
			Gen.String.AlphaNumeric.Where(n => n.Length > 0),
			Gen.Int[0, 30].Array[0, 5],
			Gen.Bool,
			(name, minors, hasMain) => new VersionIndex([
				new VersionIndexRepository(
					$"org/{name}",
					[
						new VersionIndexSpec(
							$"{name}.json",
							[
								.. hasMain ? [new VersionMoniker("main", "main")] : Array.Empty<VersionMoniker>(),
								.. minors.Select(
									(minor, i) => new VersionMoniker((i + 1).ToString(CultureInfo.InvariantCulture), $"{i + 1}.{minor}")
								)
							]
						)
					]
				)
			])
		).Sample(index => VersionIndex.Constraints.Report(index).Violations.Count == 0);

	[Test]
	public void IndexesWithTheSameContent_AreEqual() =>
		Index(("main", "main"), ("9", "9.5")).Should().Be(Index(("main", "main"), ("9", "9.5")));
}
