// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Changelog;

namespace Elastic.Documentation.Configuration.Tests;

public class ChangelogDtoProjectionCoverageTests
{
	[Test]
	public void ChangelogConfigurationDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogConfigurationDto>()
			.Maps(
				nameof(ChangelogConfigurationDto.Filename),
				nameof(ChangelogConfigurationDto.Pivot),
				nameof(ChangelogConfigurationDto.Lifecycles),
				nameof(ChangelogConfigurationDto.Products),
				nameof(ChangelogConfigurationDto.Rules),
				nameof(ChangelogConfigurationDto.Extract),
				nameof(ChangelogConfigurationDto.Bundle)
			)
			.Ignores(
				ProjectionCoverage.Ignore(
					nameof(ChangelogConfigurationDto.Block),
					"The removed key is accepted only so the loader can report its migration error."
				)
			)
			.ShouldBeComplete();

	[Test]
	public void ChangelogRulesDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogRulesDto>()
			.Maps(
				nameof(ChangelogRulesDto.Match),
				nameof(ChangelogRulesDto.Create),
				nameof(ChangelogRulesDto.Bundle),
				nameof(ChangelogRulesDto.Publish)
			)
			.ShouldBeComplete();

	[Test]
	public void ChangelogBundleRulesDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogBundleRulesDto>()
			.Maps(
				nameof(ChangelogBundleRulesDto.ExcludeProducts),
				nameof(ChangelogBundleRulesDto.IncludeProducts),
				nameof(ChangelogBundleRulesDto.MatchProducts),
				nameof(ChangelogBundleRulesDto.MatchAreas),
				nameof(ChangelogBundleRulesDto.ExcludeTypes),
				nameof(ChangelogBundleRulesDto.IncludeTypes),
				nameof(ChangelogBundleRulesDto.ExcludeAreas),
				nameof(ChangelogBundleRulesDto.IncludeAreas),
				nameof(ChangelogBundleRulesDto.Products)
			)
			.ShouldBeComplete();

	[Test]
	public void ChangelogCreateRulesDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogCreateRulesDto>()
			.Maps(
				nameof(ChangelogCreateRulesDto.Exclude),
				nameof(ChangelogCreateRulesDto.Include),
				nameof(ChangelogCreateRulesDto.Match),
				nameof(ChangelogCreateRulesDto.Products)
			)
			.ShouldBeComplete();

	[Test]
	public void ChangelogPublishRulesDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogPublishRulesDto>()
			.Maps(
				nameof(ChangelogPublishRulesDto.MatchAreas),
				nameof(ChangelogPublishRulesDto.ExcludeTypes),
				nameof(ChangelogPublishRulesDto.IncludeTypes),
				nameof(ChangelogPublishRulesDto.ExcludeAreas),
				nameof(ChangelogPublishRulesDto.IncludeAreas),
				nameof(ChangelogPublishRulesDto.Products)
			)
			.ShouldBeComplete();

	[Test]
	public void ChangelogPivotDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogPivotDto>()
			.Maps(
				nameof(ChangelogPivotDto.Types),
				nameof(ChangelogPivotDto.Subtypes),
				nameof(ChangelogPivotDto.Areas),
				nameof(ChangelogPivotDto.Products),
				nameof(ChangelogPivotDto.Features),
				nameof(ChangelogPivotDto.Highlight)
			)
			.ShouldBeComplete();

	[Test]
	public void ChangelogProductsDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogProductsDto>()
			.Maps(nameof(ChangelogProductsDto.Available), nameof(ChangelogProductsDto.Default))
			.ShouldBeComplete();

	[Test]
	public void ChangelogDefaultProductDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogDefaultProductDto>()
			.Maps(nameof(ChangelogDefaultProductDto.Product), nameof(ChangelogDefaultProductDto.Lifecycle))
			.ShouldBeComplete();

	[Test]
	public void ChangelogBundleDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogBundleDto>()
			.Maps(
				nameof(ChangelogBundleDto.Directory),
				nameof(ChangelogBundleDto.OutputDirectory),
				nameof(ChangelogBundleDto.UseLocalChangelogs),
				nameof(ChangelogBundleDto.Description),
				nameof(ChangelogBundleDto.Repo),
				nameof(ChangelogBundleDto.Owner),
				nameof(ChangelogBundleDto.Branch),
				nameof(ChangelogBundleDto.ReleaseDates),
				nameof(ChangelogBundleDto.LinkAllowRepos),
				nameof(ChangelogBundleDto.Profiles),
				nameof(ChangelogBundleDto.Releases)
			)
			.Ignores(
				ProjectionCoverage.Ignore(
#pragma warning disable CS0618 // The compatibility-only key must remain named until it is removed from the DTO.
					nameof(ChangelogBundleDto.Resolve),
#pragma warning restore CS0618
					"The deprecated key is accepted only so the loader can emit its warning."
				)
			)
			.ShouldBeComplete();

	[Test]
	public void ChangelogBundleProfileDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogBundleProfileDto>()
			.Maps(
				nameof(ChangelogBundleProfileDto.Product),
				nameof(ChangelogBundleProfileDto.Products),
				nameof(ChangelogBundleProfileDto.Output),
#pragma warning disable CS0618 // Deprecated fields are deliberately carried into the domain for compatibility.
				nameof(ChangelogBundleProfileDto.OutputDirectory),
				nameof(ChangelogBundleProfileDto.OutputProducts),
#pragma warning restore CS0618
				nameof(ChangelogBundleProfileDto.Description),
				nameof(ChangelogBundleProfileDto.Repo),
				nameof(ChangelogBundleProfileDto.Owner),
				nameof(ChangelogBundleProfileDto.Branch),
				nameof(ChangelogBundleProfileDto.HideFeatures),
				nameof(ChangelogBundleProfileDto.ReleaseDates),
#pragma warning disable CS0618 // Deprecated fields are deliberately carried into the domain for compatibility.
				nameof(ChangelogBundleProfileDto.Source)
#pragma warning restore CS0618
			)
			.ShouldBeComplete();

	[Test]
	public void ChangelogBundleReleasesDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogBundleReleasesDto>()
			.Maps(nameof(ChangelogBundleReleasesDto.Github), nameof(ChangelogBundleReleasesDto.Products))
			.ShouldBeComplete();

	[Test]
	public void ChangelogExtractDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogExtractDto>()
			.Maps(
				nameof(ChangelogExtractDto.ReleaseNotes),
				nameof(ChangelogExtractDto.Issues),
				nameof(ChangelogExtractDto.StripTitlePrefix)
			)
			.ShouldBeComplete();

	[Test]
	public void ChangelogTypeEntryDto_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage
			.For<ChangelogTypeEntryDto>()
			.Maps(nameof(ChangelogTypeEntryDto.Labels), nameof(ChangelogTypeEntryDto.Subtypes))
			.ShouldBeComplete();

	[Test]
	public void YamlLenientList_ToDomain_AccountsForEverySourceMember() =>
		ProjectionCoverage.For<YamlLenientList>().Maps(nameof(YamlLenientList.Values)).ShouldBeComplete();
}
