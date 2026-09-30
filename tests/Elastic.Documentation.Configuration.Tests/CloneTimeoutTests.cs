// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using YamlDotNet.Core;

namespace Elastic.Documentation.Configuration.Tests;

public class CloneTimeoutTests
{
	private static AssemblyConfiguration Deserialize(string refsYaml) =>
		AssemblyConfiguration.Deserialize($"narrative:\nreferences:\n{refsYaml}");

	[Test]
	[Arguments("30s", 30)]
	[Arguments("2m", 120)]
	[Arguments("15m", 900)]
	[Arguments("1s", 1)]
	public void CloneTimeout_ValidDuration_Deserializes(string input, int expectedSeconds)
	{
		var config = Deserialize($"  my-repo:\n    clone_timeout: {input}");

		var timeout = config.ReferenceRepositories["my-repo"].CloneTimeout;
		timeout.Should().NotBeNull();
		timeout.Value.TotalSeconds.Should().Be(expectedSeconds);
	}

	[Test]
	public void CloneTimeout_Absent_IsNull()
	{
		var config = Deserialize("  my-repo:");

		config.ReferenceRepositories["my-repo"].CloneTimeout.Should().BeNull();
	}

	[Test]
	public void CloneTimeout_OnNarrative_Deserializes()
	{
		var config = AssemblyConfiguration.Deserialize("narrative:\n  clone_timeout: 15m\nreferences:\n  some-repo:");

		config.Narrative.CloneTimeout.Should().Be(TimeSpan.FromMinutes(15));
	}

	[Test]
	[Arguments("1h")]
	[Arguments("90")]
	[Arguments("0m")]
	[Arguments("0s")]
	[Arguments("-5m")]
	public void CloneTimeout_InvalidDuration_ThrowsYamlException(string input)
	{
		var act = () => Deserialize($"  my-repo:\n    clone_timeout: {input}");
		act.Should().Throw<YamlException>();
	}
}
