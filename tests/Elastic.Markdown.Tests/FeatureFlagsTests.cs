// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Builder;
using FluentAssertions;
using Xunit;

namespace Elastic.Markdown.Tests;

public class FeatureFlagsTests
{
	[Fact]
	public void StagingElasticNavEnabled_ShouldReturnFalse_WhenNotSet()
	{
		// Arrange
		var featureFlags = new FeatureFlags(new Dictionary<string, bool>());

		// Act & Assert
		featureFlags.StagingElasticNavEnabled.Should().BeFalse();
	}

	[Fact]
	public void StagingElasticNavEnabled_ShouldReturnTrue_WhenSetToTrue()
	{
		// Arrange
		var featureFlags = new FeatureFlags(new Dictionary<string, bool>());

		// Act
		featureFlags.StagingElasticNavEnabled = true;

		// Assert
		featureFlags.StagingElasticNavEnabled.Should().BeTrue();
	}

	[Fact]
	public void StagingElasticNavEnabled_ShouldWork_WhenSetViaSetMethod()
	{
		// Arrange
		var featureFlags = new FeatureFlags(new Dictionary<string, bool>());

		// Act
		featureFlags.Set("STAGING_ELASTIC_NAV", true);

		// Assert
		featureFlags.StagingElasticNavEnabled.Should().BeTrue();
	}

	[Fact]
	public void StagingElasticNavEnabled_ShouldWork_WithNormalizedKey()
	{
		// Arrange
		var featureFlags = new FeatureFlags(new Dictionary<string, bool>());

		// Act
		featureFlags.Set("staging-elastic-nav", true);

		// Assert
		featureFlags.StagingElasticNavEnabled.Should().BeTrue();
	}
}