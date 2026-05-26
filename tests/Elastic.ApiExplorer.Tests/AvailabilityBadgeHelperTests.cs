// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Versions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class AvailabilityBadgeHelperTests
{
	[Theory]
	[InlineData("Experimental; added in 9.5.0", "experimental 9.5.0")]
	[InlineData("Experimental", "experimental")]
	[InlineData("Technical Preview; added in 9.4.0", "preview 9.4.0")]
	[InlineData("Generally available; added in 9.1.0", "ga 9.1.0")]
	[InlineData("Added in 7.7.0", "ga 7.7.0")]
	public void ProjectToLifecycleFormat_MapsXStateToLifecycleString(string xState, string expected)
	{
		AvailabilityBadgeHelper.ProjectToLifecycleFormat(xState).Should().Be(expected);
	}

	[Fact]
	public void FromOperation_ExperimentalXState_ProducesExperimentalBadge()
	{
		var operation = new OpenApiOperation
		{
			Extensions = new Dictionary<string, IOpenApiExtension>
			{
				["x-state"] = new JsonNodeExtension(JsonValue.Create("Experimental; added in 9.0.0"))
			}
		};

		var versionsConfiguration = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Stack,
					new VersioningSystem
					{
						Id = VersioningSystemId.Stack,
						Base = new SemVersion(8, 0, 0),
						Current = new SemVersion(9, 2, 0)
					}
				}
			}
		};

		var badgeData = AvailabilityBadgeHelper.FromOperation(operation, versionsConfiguration);

		badgeData.Should().NotBeNull();
		badgeData.LifecycleName.Should().Be("Experimental");
		badgeData.LifecycleClass.Should().Be("experimental");
		badgeData.ShowLifecycleName.Should().BeTrue();
		badgeData.BadgeVersion.Should().Be("9.0+");
	}
}
