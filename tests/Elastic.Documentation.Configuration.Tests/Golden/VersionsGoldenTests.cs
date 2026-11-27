// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>What <c>versions.yml</c> loads into today. See <c>Golden/Baseline/README.md</c> for where the input comes from.</summary>
public class VersionsGoldenTests
{
	private static string Load(string yaml) => GoldenConfig.Outcome(GoldenConfig.Versions, yaml, p => p.CreateVersionConfiguration());

	[Test]
	public void Baseline_Loaded_MatchesSnapshot() => ModelDump.ToText(GoldenConfig.LoadVersions()).Should().MatchSnapshot();

	[Test]
	public void EmptyFile_Loaded_Outcome() =>
		Load("")
			.Should()
			.MatchInlineSnapshot(
				"""
		throws
		InvalidOperationException: Failed to deserialize YAML into 'Elastic.Documentation.Configuration.Versions.VersionsConfigDto'.
		"""
			);

	[Test]
	public void EmptyMap_Loaded_Outcome() =>
		Load("versioning_systems: {}\n")
			.Should()
			.MatchInlineSnapshot("""
		model <VersionsConfiguration>:
		  VersioningSystems: {}
		""");

	[Test]
	public void UnknownTopLevelKey_Loaded_Outcome() =>
		Load("bogus: 1\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/versions.yml: (Lin: 0, Col: 0, Chr: 0) - (Lin: 0, Col: 7, Chr: 7): (1,1) Unknown key 'bogus'. Valid keys: versioning_systems.
				"""
			);

	[Test]
	public void UnknownSystemKey_Loaded_Outcome() =>
		Load("versioning_systems:\n  stack:\n    base: 9.0\n    current: 9.1\n    extra: x\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/versions.yml: (Lin: 4, Col: 4, Chr: 64) - (Lin: 4, Col: 11, Chr: 71): (5,5) Unknown key 'extra' in $.versioning_systems.stack. Valid keys: base, current.
				"""
			);

	[Test]
	public void UnknownVersioningSystemId_Loaded_Outcome() =>
		Load("versioning_systems:\n  not-a-system:\n    base: 1.0\n    current: 1.1\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			throws
			InvalidOperationException: Could not parse versioning system id not-a-system
			"""
			);

	[Test]
	public void MissingBaseAndCurrent_Loaded_Outcome() =>
		Load("versioning_systems:\n  stack: {}\n")
			.Should()
			.MatchInlineSnapshot("""
		throws
		InvalidOperationException: Could not parse version
		""");

	[Test]
	public void BlankVersions_Loaded_Outcome() =>
		Load("versioning_systems:\n  stack:\n    base: ''\n    current: '  '\n")
			.Should()
			.MatchInlineSnapshot("""
			throws
			InvalidOperationException: Could not parse version
			""");

	[Test]
	public void NonSemVersion_Loaded_Outcome() =>
		Load("versioning_systems:\n  stack:\n    base: banana\n    current: 9.1\n")
			.Should()
			.MatchInlineSnapshot("""
			throws
			InvalidOperationException: Could not parse version banana
			""");

	[Test]
	public void WrongTypeForSystemMap_Loaded_Outcome() =>
		Load("versioning_systems: [a, b]\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/versions.yml: (Lin: 0, Col: 20, Chr: 20) - (Lin: 0, Col: 21, Chr: 21): Expected a StartMapping token but found 'StartSequence'.
				"""
			);

	[Test]
	public void WrongTypeForSystem_Loaded_Outcome() =>
		Load("versioning_systems:\n  stack: 9.1\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/versions.yml: (Lin: 1, Col: 9, Chr: 29) - (Lin: 1, Col: 12, Chr: 32): Expected a StartMapping token but found 'Scalar'.
				"""
			);

	[Test]
	public void ShortVersionStrings_Loaded_PaddedWithZeros() =>
		Load("versioning_systems:\n  stack:\n    base: 9\n    current: 9.1\n  ece:\n    base: 3.0.0-beta.1\n    current: 4.2.3+build\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				model <VersionsConfiguration>:
				  VersioningSystems:
				    Stack <VersioningSystem>:
				      Id: Stack
				      Base: 9.0.0
				      Current: 9.1.0
				      IsVersionless: False
				    Ece <VersioningSystem>:
				      Id: Ece
				      Base: 3.0.0-beta.1
				      Current: 4.2.3+build
				      IsVersionless: False
				"""
			);

	[Test]
	public void DuplicateSystemKey_Loaded_Outcome() =>
		Load("versioning_systems:\n  stack:\n    base: 9.0\n    current: 9.1\n  stack:\n    base: 8.0\n    current: 8.1\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				throws
				YamlException: {config-runtime}/versions.yml: (Lin: 5, Col: 4, Chr: 73) - (Lin: 5, Col: 4, Chr: 73): Duplicate mapping key 'stack'.
				"""
			);

	[Test]
	public void CaseDifferentSystemId_Loaded_Outcome() =>
		Load("versioning_systems:\n  Stack:\n    base: 9.0\n    current: 9.1\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			model <VersionsConfiguration>:
			  VersioningSystems:
			    Stack <VersioningSystem>:
			      Id: Stack
			      Base: 9.0.0
			      Current: 9.1.0
			      IsVersionless: False
			"""
			);
}
