// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Documentation.Configuration.Changelog;

namespace Elastic.Changelog.Tests.Creation;

public class FilenameStrategyTests
{
	private static CreateChangelogArguments DefaultInput() => new() { Products = [] };

	[Fact]
	public void ApplyConfigDefaults_AlwaysSetsPrNumberTrue()
	{
		var config = ChangelogConfiguration.Default;
		var input = DefaultInput();

		var result = ChangelogCreationService.ApplyConfigDefaults(input, config);

		result.UsePrNumber.Should().BeTrue("filename strategy is always Pr");
	}

	[Fact]
	public void ApplyConfigDefaults_DefaultConfig_UsesPr()
	{
		var config = ChangelogConfiguration.Default;
		var input = DefaultInput();

		var result = ChangelogCreationService.ApplyConfigDefaults(input, config);

		result.UsePrNumber.Should().BeTrue("default FilenameStrategy is Pr");
	}

	[Fact]
	public void ApplyConfigDefaults_CLIUsePrNumber_RemainsTrue()
	{
		var config = ChangelogConfiguration.Default;
		var input = DefaultInput() with { UsePrNumber = true };

		var result = ChangelogCreationService.ApplyConfigDefaults(input, config);

		result.UsePrNumber.Should().BeTrue();
	}
}
