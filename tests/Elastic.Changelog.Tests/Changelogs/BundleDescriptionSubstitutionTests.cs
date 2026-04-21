// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;

namespace Elastic.Changelog.Tests.Changelogs;

public class BundleDescriptionSubstitutionTests
{
	[Fact]
	public void SubstitutePlaceholders_AllPlaceholdersResolved_ReturnsSubstitutedString()
	{
		var result = BundleDescriptionSubstitution.SubstitutePlaceholders(
			"Release {version} ({lifecycle}) from {owner}/{repo}",
			"9.2.0", "ga", "elastic", "elasticsearch");

		result.Should().Be("Release 9.2.0 (ga) from elastic/elasticsearch");
	}

	[Fact]
	public void SubstitutePlaceholders_NullValues_ReplacedWithEmptyString()
	{
		var result = BundleDescriptionSubstitution.SubstitutePlaceholders(
			"Version {version} by {owner}", null, null, null, null);

		result.Should().Be("Version  by ");
	}

	[Fact]
	public void SubstitutePlaceholders_EmptyDescription_ReturnsEmpty()
	{
		var result = BundleDescriptionSubstitution.SubstitutePlaceholders(
			"", "9.2.0", "ga", "elastic", "elasticsearch");

		result.Should().BeEmpty();
	}

	[Fact]
	public void SubstitutePlaceholders_NoPlaceholders_ReturnsOriginal()
	{
		var result = BundleDescriptionSubstitution.SubstitutePlaceholders(
			"Just a plain description.", "9.2.0", "ga", "elastic", "elasticsearch");

		result.Should().Be("Just a plain description.");
	}

	[Fact]
	public void SubstitutePlaceholders_PartialPlaceholders_OnlySubstitutesPresent()
	{
		var result = BundleDescriptionSubstitution.SubstitutePlaceholders(
			"Download: https://github.com/{owner}/{repo}/releases",
			null, null, "elastic", "elasticsearch");

		result.Should().Be("Download: https://github.com/elastic/elasticsearch/releases");
	}

	[Fact]
	public void SubstitutePlaceholders_ValidateResolvable_ThrowsWhenVersionMissing()
	{
		var act = () => BundleDescriptionSubstitution.SubstitutePlaceholders(
			"Release {version}", null, null, null, null, validateResolvable: true);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*version*");
	}

	[Fact]
	public void SubstitutePlaceholders_ValidateResolvable_ThrowsWhenMultipleMissing()
	{
		var act = () => BundleDescriptionSubstitution.SubstitutePlaceholders(
			"v{version} ({lifecycle}) from {owner}/{repo}",
			null, null, null, null, validateResolvable: true);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*version*lifecycle*owner*repo*");
	}

	[Fact]
	public void SubstitutePlaceholders_ValidateResolvable_SucceedsWhenAllProvided()
	{
		var result = BundleDescriptionSubstitution.SubstitutePlaceholders(
			"v{version} from {owner}/{repo}",
			"9.2.0", "ga", "elastic", "elasticsearch", validateResolvable: true);

		result.Should().Be("v9.2.0 from elastic/elasticsearch");
	}

	[Fact]
	public void SubstitutePlaceholders_ValidateResolvable_IgnoresUnusedNullValues()
	{
		var result = BundleDescriptionSubstitution.SubstitutePlaceholders(
			"Download from {owner}/{repo}",
			null, null, "elastic", "elasticsearch", validateResolvable: true);

		result.Should().Be("Download from elastic/elasticsearch");
	}

	[Fact]
	public void SubstitutePlaceholders_NullDescription_ReturnsNull()
	{
		var result = BundleDescriptionSubstitution.SubstitutePlaceholders(
			null!, "9.2.0", "ga", "elastic", "elasticsearch");

		result.Should().BeNull();
	}
}
