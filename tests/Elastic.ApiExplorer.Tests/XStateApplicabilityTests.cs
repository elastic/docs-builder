// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Versions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class XStateApplicabilityTests
{
	private static readonly SemVersion CurrentStack = new(9, 2, 0);

	[Test]
	[Arguments("Experimental; added in 9.5.0", "experimental 9.5.0")]
	[Arguments("Experimental", "experimental")]
	[Arguments("Technical Preview; added in 9.4.0", "preview 9.4.0")]
	[Arguments("Technical preview", "preview")]
	[Arguments("Beta; Added in 8.12.0", "beta 8.12.0")]
	[Arguments("Generally available; added in 9.1.0", "ga 9.1.0")]
	[Arguments("Generally available", "ga")]
	[Arguments("Added in 7.7.0", "ga 7.7.0")]
	[Arguments("Deprecated; Deprecated in 7.0.0", "deprecated 7.0.0")]
	[Arguments("Generally available; Deprecated in 9.2.0", "deprecated 9.2.0")]
	[Arguments("   ", null)]
	[Arguments("Something else", null)]
	public void ToAppliesToSyntax_XStateLabel_MapsToAppliesToSyntax(string xState, string? expected) =>
		XStateApplicability.ToAppliesToSyntax(xState).Should().Be(expected);

	[Test]
	public void Parse_GaWithVersion_ProducesStackApplicability()
	{
		var applies = XStateApplicability.Parse("Generally available; Added in 7.7.0");

		applies.Should().NotBeNull();
		var stack = applies.Stack!.Single();
		stack.Lifecycle.Should().Be(ProductLifecycle.GenerallyAvailable);
		stack.Version!.Min.Should().Be(new SemVersion(7, 7, 0));
	}

	[Test]
	[Arguments("Generally available; Added in 9.5.0", true)]
	[Arguments("Experimental; Added in 9.5.0", true)]
	[Arguments("Generally available; Added in 9.2.0", false)]
	[Arguments("Deprecated; Deprecated in 9.5.0", false)]
	[Arguments("Technical Preview", false)]
	public void IsUnreleased_XStateLabel_OnlyFlagsFutureIntroductions(string xState, bool expected) =>
		XStateApplicability.IsUnreleased(XStateApplicability.Parse(xState), CurrentStack).Should().Be(expected);

	[Test]
	[Arguments("Generally available", false)]
	[Arguments("Generally available; Added in 7.7.0", true)]
	[Arguments("Technical Preview", true)]
	[Arguments("Deprecated; Deprecated in 7.0.0", true)]
	public void IsWorthABadge_XStateLabel_SkipsPlainGa(string xState, bool expected) =>
		XStateApplicability.IsWorthABadge(XStateApplicability.Parse(xState)!).Should().Be(expected);

	[Test]
	[Arguments("Experimental; added in 9.0.0", "Experimental", "9.0+", "", "Experimental since 9.0")]
	[Arguments("Generally available; Added in 7.7.0", "GA", "7.7+", "", "Generally available since 7.7")]
	[Arguments("Generally available; Added in 9.5.0", "GA", "", "Planned", "Planned")]
	[Arguments("Deprecated; Deprecated in 7.0.0", "Deprecated", "7.0+", "", "Deprecated since 7.0")]
	public void FromOperation_XState_RendersLikeAppliesTo(
		string xState,
		string lifecycleName,
		string version,
		string lifecycleText,
		string popoverText
	)
	{
		var badge = AvailabilityBadge.FromOperation(OperationWithState(xState), StackVersions());

		badge.Should().NotBeNull();
		badge.Inline.Should().BeTrue();
		var item = badge.GetApplicabilityItems().Single();
		item.Key.Should().Be("Stack");
		item.RenderData.LifecycleName.Should().Be(lifecycleName);
		item.RenderData.Version.Should().Be(version);
		item.RenderData.BadgeLifecycleText.Should().Be(lifecycleText);
		item.RenderData.PopoverData!.AvailabilityItems.Select(a => a.Text).Should().Equal(popoverText);
	}

	[Test]
	[Arguments("Generally available")]
	[Arguments("   ")]
	public void FromOperation_NothingWorthShowing_ReturnsNull(string xState) =>
		AvailabilityBadge.FromOperation(OperationWithState(xState), StackVersions()).Should().BeNull();

	private static OpenApiOperation OperationWithState(string xState) =>
		new() { Extensions = new Dictionary<string, IOpenApiExtension> { ["x-state"] = new JsonNodeExtension(JsonValue.Create(xState)) } };

	private static VersionsConfiguration StackVersions() =>
		new()
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Stack,
					new VersioningSystem { Id = VersioningSystemId.Stack, Base = new SemVersion(8, 0, 0), Current = CurrentStack }
				}
			}
		};
}
