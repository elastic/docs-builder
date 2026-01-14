// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Encodings.Web;
using System.Text.Json;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using FluentAssertions;

namespace Elastic.Markdown.Tests.AppliesTo;

public class ApplicableToJsonConverterSerializationTests
{
	private readonly JsonSerializerOptions _options = new()
	{
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	[Fact]
	public void SerializeStackProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Stack = AppliesCollection.GenerallyAvailable
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().Be(
			"""
			[
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "ga",
			    "version": "all"
			  }
			]
			""");
	}

	[Fact]
	public void SerializeStackWithVersionProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Stack = new AppliesCollection([
				new Applicability
				{
					Lifecycle = ProductLifecycle.Beta,
					Version = (VersionSpec)"8.0.0"
				}
			])
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().Be(
			"""
			[
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "beta",
			    "version": "8.0+"
			  }
			]
			""");
	}

	[Fact]
	public void SerializeMultipleApplicabilitiesProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Stack = new AppliesCollection(
			[
				new Applicability
				{
					Lifecycle = ProductLifecycle.GenerallyAvailable,
					Version = (VersionSpec)"8.0.0"
				},
				new Applicability
				{
					Lifecycle = ProductLifecycle.Beta,
					Version = (VersionSpec)"7.17.0"
				}
			])
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().Be(
			"""
			[
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "ga",
			    "version": "8.0+"
			  },
			  {
			    "type": "stack",
			    "sub_type": "stack",
			    "lifecycle": "beta",
			    "version": "7.17+"
			  }
			]
			""");
	}

	[Fact]
	public void SerializeDeploymentProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Deployment = new DeploymentApplicability
			{
				Ece = new AppliesCollection([
					new Applicability
					{
						Lifecycle = ProductLifecycle.GenerallyAvailable,
						Version = (VersionSpec)"3.0.0"
					}
				]),
				Ess = AppliesCollection.GenerallyAvailable
			}
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().Be(
			"""
			[
			  {
			    "type": "deployment",
			    "sub_type": "ece",
			    "lifecycle": "ga",
			    "version": "3.0+"
			  },
			  {
			    "type": "deployment",
			    "sub_type": "ess",
			    "lifecycle": "ga",
			    "version": "all"
			  }
			]
			""");
	}

	[Fact]
	public void SerializeServerlessProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Serverless = new ServerlessProjectApplicability
			{
				Elasticsearch = new AppliesCollection([
					new Applicability
					{
						Lifecycle = ProductLifecycle.Beta,
						Version = (VersionSpec)"1.0.0"
					}
				]),
				Security = AppliesCollection.GenerallyAvailable
			}
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().Be(
			"""
			[
			  {
			    "type": "serverless",
			    "sub_type": "elasticsearch",
			    "lifecycle": "beta",
			    "version": "1.0+"
			  },
			  {
			    "type": "serverless",
			    "sub_type": "security",
			    "lifecycle": "ga",
			    "version": "all"
			  }
			]
			""");
	}

	[Fact]
	public void SerializeProductProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Product = new AppliesCollection([
				new Applicability
				{
					Lifecycle = ProductLifecycle.TechnicalPreview,
					Version = (VersionSpec)"0.5.0"
				}
			])
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().Be(
			"""
			[
			  {
			    "type": "product",
			    "sub_type": "product",
			    "lifecycle": "preview",
			    "version": "0.5+"
			  }
			]
			""");
	}

	[Fact]
	public void SerializeProductApplicabilityProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			ProductApplicability = new ProductApplicability
			{
				Ecctl = new AppliesCollection([
					new Applicability
					{
						Lifecycle = ProductLifecycle.Deprecated,
						Version = (VersionSpec)"5.0.0"
					}
				]),
				ApmAgentDotnet = AppliesCollection.GenerallyAvailable
			}
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// language=json
		json.Should().Be(
			"""
			[
			  {
			    "type": "product",
			    "sub_type": "ecctl",
			    "lifecycle": "deprecated",
			    "version": "5.0+"
			  },
			  {
			    "type": "product",
			    "sub_type": "apm-agent-dotnet",
			    "lifecycle": "ga",
			    "version": "all"
			  }
			]
			""");
	}

	[Fact]
	public void SerializeAllLifecyclesProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Stack = new AppliesCollection(
			[
				new Applicability
				{
					Lifecycle = ProductLifecycle.TechnicalPreview,
					Version = (VersionSpec)"1.0.0"
				},
				new Applicability
				{
					Lifecycle = ProductLifecycle.Beta,
					Version = (VersionSpec)"1.0.0"
				},
				new Applicability
				{
					Lifecycle = ProductLifecycle.GenerallyAvailable,
					Version = (VersionSpec)"1.0.0"
				},
				new Applicability
				{
					Lifecycle = ProductLifecycle.Deprecated,
					Version = (VersionSpec)"1.0.0"
				},
				new Applicability
				{
					Lifecycle = ProductLifecycle.Removed,
					Version = (VersionSpec)"1.0.0"
				}
			])
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		json.Should().Contain("\"lifecycle\": \"preview\"");
		json.Should().Contain("\"lifecycle\": \"beta\"");
		json.Should().Contain("\"lifecycle\": \"ga\"");
		json.Should().Contain("\"lifecycle\": \"deprecated\"");
		json.Should().Contain("\"lifecycle\": \"removed\"");
	}

	[Fact]
	public void SerializeComplexProducesCorrectJson()
	{
		var applicableTo = new ApplicableTo
		{
			Stack = new AppliesCollection([
				new Applicability
				{
					Lifecycle = ProductLifecycle.GenerallyAvailable,
					Version = (VersionSpec)"8.0.0"
				}
			]),
			Deployment = new DeploymentApplicability
			{
				Ece = AppliesCollection.GenerallyAvailable
			},
			Product = AppliesCollection.GenerallyAvailable
		};

		var json = JsonSerializer.Serialize(applicableTo, _options);

		// Verify it's an array with 3 items
		var jsonDoc = JsonDocument.Parse(json);
		jsonDoc.RootElement.GetArrayLength().Should().Be(3);

		// Verify each type is present
		json.Should().Contain("\"type\": \"stack\"");
		json.Should().Contain("\"type\": \"deployment\"");
		json.Should().Contain("\"type\": \"product\"");

		// Verify sub_types
		json.Should().Contain("\"sub_type\": \"stack\"");
		json.Should().Contain("\"sub_type\": \"ece\"");
		json.Should().Contain("\"sub_type\": \"product\"");
	}

	[Fact]
	public void SerializeEmptyProducesEmptyArray()
	{
		var applicableTo = new ApplicableTo();

		var json = JsonSerializer.Serialize(applicableTo, _options);

		json.Should().Be("[]");
	}

	[Fact]
	public void SerializeValidatesJsonStructure()
	{
		var original = new ApplicableTo
		{
			Stack = AppliesCollection.GenerallyAvailable,
			Deployment = new DeploymentApplicability
			{
				Ece = new AppliesCollection([
					new Applicability
					{
						Lifecycle = ProductLifecycle.Beta,
						Version = (VersionSpec)"3.0.0"
					}
				])
			}
		};

		var json = JsonSerializer.Serialize(original, _options);
		var jsonDoc = JsonDocument.Parse(json);
		var root = jsonDoc.RootElement;

		root.ValueKind.Should().Be(JsonValueKind.Array);
		var array = root.EnumerateArray().ToList();

		array.Should().HaveCount(2); // Stack + Deployment.Ece

		var stackEntry = array[0];
		stackEntry.GetProperty("type").GetString().Should().Be("stack");
		stackEntry.GetProperty("sub_type").GetString().Should().Be("stack");
		stackEntry.GetProperty("lifecycle").GetString().Should().Be("ga");
		stackEntry.GetProperty("version").GetString().Should().Be("all");

		var deploymentEntry = array[1];
		deploymentEntry.GetProperty("type").GetString().Should().Be("deployment");
		deploymentEntry.GetProperty("sub_type").GetString().Should().Be("ece");
		deploymentEntry.GetProperty("lifecycle").GetString().Should().Be("beta");
		deploymentEntry.GetProperty("version").GetString().Should().Be("3.0+");
	}
}
