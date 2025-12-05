// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using FluentAssertions;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Tests.AppliesTo;

public class ProductApplicabilityToStringTests
{
	[Fact]
	public void ProductApplicabilityToStringIncludesAllProperties()
	{
		// Create a ProductApplicability with all properties set
		var productApplicability = new ProductApplicability();
		var productType = typeof(ProductApplicability);
		var properties = productType.GetProperties()
			.Where(p => p.GetCustomAttribute<YamlMemberAttribute>() != null)
			.ToList();

		// Set all properties to a test value
		var testValue = AppliesCollection.GenerallyAvailable;
		foreach (var property in properties)
		{
			property.SetValue(productApplicability, testValue);
		}

		// Get the ToString output
		var result = productApplicability.ToString();

		// Verify that each property's YAML alias appears in the output
		foreach (var property in properties)
		{
			var yamlAlias = property.GetCustomAttribute<YamlMemberAttribute>()!.Alias;
			result.Should().Contain($"{yamlAlias}=",
				$"ToString should include the property {property.Name} with alias '{yamlAlias}'");
		}

		// Verify we have the expected number of properties
		properties.Should().HaveCount(23, "ProductApplicability should have exactly 23 product properties");
	}

	[Fact]
	public void ProductApplicabilityToStringWithSomePropertiesOnlyIncludesSetProperties()
	{
		var productApplicability = new ProductApplicability
		{
			ApmAgentDotnet = AppliesCollection.GenerallyAvailable,
			Ecctl = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = new SemVersion(1, 0, 0) }])
		};

		var result = productApplicability.ToString();

		// Should include set properties
		result.Should().Contain("apm-agent-dotnet=");
		result.Should().Contain("ecctl=");

		// Should not include unset properties
		result.Should().NotContain("apm-agent-node=");
		result.Should().NotContain("curator=");
	}

	[Fact]
	public void ProductApplicabilityToStringEmptyReturnsEmptyString()
	{
		var productApplicability = new ProductApplicability();

		var result = productApplicability.ToString();

		result.Should().Be("");
	}

	[Fact]
	public void ProductApplicabilityToStringPropertyOrderMatchesReflectionOrder()
	{
		// This test ensures that properties appear in the order they are defined
		var productApplicability = new ProductApplicability
		{
			Ecctl = AppliesCollection.GenerallyAvailable,
			Curator = AppliesCollection.GenerallyAvailable,
			ApmAgentAndroid = AppliesCollection.GenerallyAvailable
		};

		var result = productApplicability.ToString();

		// Get the properties in reflection order
		var productType = typeof(ProductApplicability);
		var properties = productType.GetProperties()
			.Where(p => p.GetCustomAttribute<YamlMemberAttribute>() != null)
			.Select(p => p.GetCustomAttribute<YamlMemberAttribute>()!.Alias)
			.ToList();

		// Find positions in the string
		var positions = new Dictionary<string, int>();
		foreach (var alias in new[] { "ecctl", "curator", "apm-agent-android" })
		{
			var index = result.IndexOf($"{alias}=", StringComparison.Ordinal);
			if (index >= 0)
				positions[alias] = index;
		}

		// Verify that the properties appear in the correct order
		positions["ecctl"].Should().BeLessThan(positions["curator"],
			"ecctl should appear before curator");
		positions["curator"].Should().BeLessThan(positions["apm-agent-android"],
			"curator should appear before apm-agent-android");
	}
}
