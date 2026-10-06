// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>What the applies_to reader drops is kept on the model (see Constraints-Conventions.md), so the constraints can name it.</summary>
public class ApplicableToAuthoringTests
{
	private static ApplicableTo Read(string yaml) =>
		new DeserializerBuilder().WithTypeConverter(new ApplicableToYamlConverter([])).Build().Deserialize<ApplicableTo>(yaml);

	[Test]
	public void NestedKeysNoReaderMaps_AreKept()
	{
		var applicable = Read(
			"""
			deployment:
			  ece: ga
			  bogus: ga
			product:
			  apm-agent-java: ga
			"""
		);

		applicable.UnknownKeys.Should().Equal("deployment.bogus", "product.apm-agent-java");
		ApplicableToConstraints.All.Validate(applicable).Select(v => v.ConstraintId).Should().Contain("applies-to-keys-read");
	}

	[Test]
	public void ANonTextValue_IsKept()
	{
		var applicable = Read(
			"""
			stack: ga
			serverless:
			  elasticsearch:
			    - ga
			"""
		);

		applicable.IgnoredValues.Should().Equal("serverless.elasticsearch");
		ApplicableToConstraints.All.Validate(applicable).Select(v => v.ConstraintId).Should().Contain("applies-to-values-are-text");
	}

	[Test]
	public void AnUnparsableVersionAndExtraTokens_AreReported()
	{
		var applicable = Read("""
			stack: ga 9.x, beta 9.1 extra
			""");

		ApplicableToConstraints
			.All
			.Validate(applicable)
			.Where(v => v.ConstraintId == "applicability-version-readable")
			.Should()
			.HaveCount(2);
	}

	[Test]
	public void AWellFormedBlock_KeepsNothing()
	{
		var applicable = Read("""
			stack: ga 9.0
			deployment:
			  ess: ga
			""");

		applicable.UnknownKeys.Should().BeEmpty();
		applicable.IgnoredValues.Should().BeEmpty();
	}
}
