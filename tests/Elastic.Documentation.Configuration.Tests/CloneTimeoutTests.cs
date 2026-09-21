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

	[Theory]
	[InlineData("30s", 30)]
	[InlineData("2m", 120)]
	[InlineData("15m", 900)]
	[InlineData("1s", 1)]
	public void CloneTimeout_ValidDuration_Deserializes(string input, int expectedSeconds)
	{
		var config = Deserialize($"  my-repo:\n    clone_timeout: {input}");

		var timeout = config.ReferenceRepositories["my-repo"].CloneTimeout;
		timeout.Should().NotBeNull();
		timeout.Value.TotalSeconds.Should().Be(expectedSeconds);
	}

	[Fact]
	public void CloneTimeout_Absent_IsNull()
	{
		var config = Deserialize("  my-repo:");

		config.ReferenceRepositories["my-repo"].CloneTimeout.Should().BeNull();
	}

	[Fact]
	public void CloneTimeout_OnNarrative_Deserializes()
	{
		var config = AssemblyConfiguration.Deserialize("narrative:\n  clone_timeout: 15m\nreferences:\n  some-repo:");

		config.Narrative.CloneTimeout.Should().Be(TimeSpan.FromMinutes(15));
	}

	[Theory]
	[InlineData("1h")]
	[InlineData("90")]
	[InlineData("0m")]
	[InlineData("0s")]
	[InlineData("-5m")]
	public void CloneTimeout_InvalidDuration_ThrowsYamlException(string input)
	{
		var act = () => Deserialize($"  my-repo:\n    clone_timeout: {input}");
		act.Should().Throw<YamlException>();
	}
}
