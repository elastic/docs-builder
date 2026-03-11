// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Creation;
using Elastic.Documentation.Configuration.Changelog;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Creation;

public class FilenameStrategyTests
{
	private static CreateChangelogArguments DefaultInput() =>
		new() { Products = [] };

	[Fact]
	public void ApplyConfigDefaults_FilenamePr_SetsUsePrNumber()
	{
		var config = ChangelogConfiguration.Default with { Filename = FilenameStrategy.Pr };
		var input = DefaultInput();

		var result = ChangelogCreationService.ApplyConfigDefaults(input, config);

		result.UsePrNumber.Should().BeTrue();
		result.UseIssueNumber.Should().BeFalse();
	}

	[Fact]
	public void ApplyConfigDefaults_FilenameIssue_SetsUseIssueNumber()
	{
		var config = ChangelogConfiguration.Default with { Filename = FilenameStrategy.Issue };
		var input = DefaultInput();

		var result = ChangelogCreationService.ApplyConfigDefaults(input, config);

		result.UsePrNumber.Should().BeFalse();
		result.UseIssueNumber.Should().BeTrue();
	}

	[Fact]
	public void ApplyConfigDefaults_FilenameTimestamp_NeitherFlagSet()
	{
		var config = ChangelogConfiguration.Default with { Filename = FilenameStrategy.Timestamp };
		var input = DefaultInput();

		var result = ChangelogCreationService.ApplyConfigDefaults(input, config);

		result.UsePrNumber.Should().BeFalse();
		result.UseIssueNumber.Should().BeFalse();
	}

	[Fact]
	public void ApplyConfigDefaults_CLIUsePrNumber_OverridesConfigIssue()
	{
		var config = ChangelogConfiguration.Default with { Filename = FilenameStrategy.Issue };
		var input = DefaultInput() with { UsePrNumber = true };

		var result = ChangelogCreationService.ApplyConfigDefaults(input, config);

		result.UsePrNumber.Should().BeTrue();
		result.UseIssueNumber.Should().BeFalse();
	}

	[Fact]
	public void ApplyConfigDefaults_CLIUseIssueNumber_OverridesConfigPr()
	{
		var config = ChangelogConfiguration.Default with { Filename = FilenameStrategy.Pr };
		var input = DefaultInput() with { UseIssueNumber = true };

		var result = ChangelogCreationService.ApplyConfigDefaults(input, config);

		result.UsePrNumber.Should().BeFalse();
		result.UseIssueNumber.Should().BeTrue();
	}

	[Fact]
	public void ApplyConfigDefaults_DefaultConfig_UsePrNumber()
	{
		var config = ChangelogConfiguration.Default;
		var input = DefaultInput();

		var result = ChangelogCreationService.ApplyConfigDefaults(input, config);

		result.UsePrNumber.Should().BeTrue("default FilenameStrategy is Pr");
		result.UseIssueNumber.Should().BeFalse();
	}
}
