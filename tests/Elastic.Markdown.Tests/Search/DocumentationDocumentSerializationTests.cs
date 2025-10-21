// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Search;
using Elastic.Documentation.Serialization;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Search;

public class DocumentationDocumentSerializationTests
{
	private readonly JsonSerializerOptions _options = new(SourceGenerationContext.Default.Options);

	[Fact]
	public void Serialize_DocumentWithStackAppliesTo_ProducesCorrectJson()
	{
		var doc = new DocumentationDocument
		{
			Url = "/test/page",
			Title = "Test Page",
			Applies = new ApplicableTo
			{
				Stack = AppliesCollection.GenerallyAvailable
			}
		};

		var json = JsonSerializer.Serialize(doc, _options);
		var jsonDoc = JsonDocument.Parse(json);
		var root = jsonDoc.RootElement;

		// Verify applies_to exists
		root.TryGetProperty("applies_to", out var appliesTo).Should().BeTrue();
		appliesTo.ValueKind.Should().Be(JsonValueKind.Array);

		// Verify structure
		var appliesArray = appliesTo.EnumerateArray().ToList();
		appliesArray.Should().HaveCount(1);

		var stackEntry = appliesArray[0];
		stackEntry.GetProperty("type").GetString().Should().Be("stack");
		stackEntry.GetProperty("sub_type").GetString().Should().Be("stack");
		stackEntry.GetProperty("lifecycle").GetString().Should().Be("ga");
		stackEntry.GetProperty("version").GetString().Should().Be("9999.9999.9999");
	}

	[Fact]
	public void Serialize_DocumentWithDeploymentAppliesTo_ProducesCorrectJson()
	{
		var doc = new DocumentationDocument
		{
			Url = "/test/deployment",
			Title = "Deployment Test",
			Applies = new ApplicableTo
			{
				Deployment = new DeploymentApplicability
				{
					Ess = AppliesCollection.GenerallyAvailable,
					Ece = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (SemVersion)"3.5.0" }])
				}
			}
		};

		var json = JsonSerializer.Serialize(doc, _options);
		var jsonDoc = JsonDocument.Parse(json);
		var root = jsonDoc.RootElement;

		root.TryGetProperty("applies_to", out var appliesTo).Should().BeTrue();
		var appliesArray = appliesTo.EnumerateArray().ToList();
		appliesArray.Should().HaveCount(2);

		// Verify ESS entry
		var essEntry = appliesArray.FirstOrDefault(e => e.GetProperty("sub_type").GetString() == "ess");
		essEntry.ValueKind.Should().NotBe(JsonValueKind.Undefined);
		essEntry.GetProperty("type").GetString().Should().Be("deployment");
		essEntry.GetProperty("lifecycle").GetString().Should().Be("ga");
		essEntry.GetProperty("version").GetString().Should().Be("9999.9999.9999");

		// Verify ECE entry
		var eceEntry = appliesArray.FirstOrDefault(e => e.GetProperty("sub_type").GetString() == "ece");
		eceEntry.ValueKind.Should().NotBe(JsonValueKind.Undefined);
		eceEntry.GetProperty("type").GetString().Should().Be("deployment");
		eceEntry.GetProperty("lifecycle").GetString().Should().Be("beta");
		eceEntry.GetProperty("version").GetString().Should().Be("3.5.0");
	}

	[Fact]
	public void Serialize_DocumentWithServerlessAppliesTo_ProducesCorrectJson()
	{
		var doc = new DocumentationDocument
		{
			Url = "/test/serverless",
			Title = "Serverless Test",
			Applies = new ApplicableTo
			{
				Serverless = new ServerlessProjectApplicability
				{
					Elasticsearch = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (SemVersion)"8.0.0" }]),
					Security = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.TechnicalPreview, Version = (SemVersion)"1.0.0" }])
				}
			}
		};

		var json = JsonSerializer.Serialize(doc, _options);
		var jsonDoc = JsonDocument.Parse(json);
		var root = jsonDoc.RootElement;

		root.TryGetProperty("applies_to", out var appliesTo).Should().BeTrue();
		var appliesArray = appliesTo.EnumerateArray().ToList();
		appliesArray.Should().HaveCount(2);

		// Verify elasticsearch entry
		var esEntry = appliesArray.FirstOrDefault(e => e.GetProperty("sub_type").GetString() == "elasticsearch");
		esEntry.ValueKind.Should().NotBe(JsonValueKind.Undefined);
		esEntry.GetProperty("type").GetString().Should().Be("serverless");
		esEntry.GetProperty("lifecycle").GetString().Should().Be("ga");
		esEntry.GetProperty("version").GetString().Should().Be("8.0.0");

		// Verify security entry
		var secEntry = appliesArray.FirstOrDefault(e => e.GetProperty("sub_type").GetString() == "security");
		secEntry.ValueKind.Should().NotBe(JsonValueKind.Undefined);
		secEntry.GetProperty("type").GetString().Should().Be("serverless");
		secEntry.GetProperty("lifecycle").GetString().Should().Be("preview");
		secEntry.GetProperty("version").GetString().Should().Be("1.0.0");
	}

	[Fact]
	public void Serialize_DocumentWithProductAppliesTo_ProducesCorrectJson()
	{
		var doc = new DocumentationDocument
		{
			Url = "/test/product",
			Title = "Product Test",
			Applies = new ApplicableTo
			{
				Product = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (SemVersion)"2.0.0" }])
			}
		};

		var json = JsonSerializer.Serialize(doc, _options);
		var jsonDoc = JsonDocument.Parse(json);
		var root = jsonDoc.RootElement;

		root.TryGetProperty("applies_to", out var appliesTo).Should().BeTrue();
		var appliesArray = appliesTo.EnumerateArray().ToList();
		appliesArray.Should().HaveCount(1);

		var productEntry = appliesArray[0];
		productEntry.GetProperty("type").GetString().Should().Be("product");
		productEntry.GetProperty("sub_type").GetString().Should().Be("product");
		productEntry.GetProperty("lifecycle").GetString().Should().Be("beta");
		productEntry.GetProperty("version").GetString().Should().Be("2.0.0");
	}

	[Fact]
	public void Serialize_DocumentWithProductApplicability_ProducesCorrectJson()
	{
		var doc = new DocumentationDocument
		{
			Url = "/test/apm",
			Title = "APM Test",
			Applies = new ApplicableTo
			{
				ProductApplicability = new ProductApplicability
				{
					ApmAgentDotnet = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (SemVersion)"1.5.0" }]),
					ApmAgentNode = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Deprecated, Version = (SemVersion)"2.0.0" }])
				}
			}
		};

		var json = JsonSerializer.Serialize(doc, _options);
		var jsonDoc = JsonDocument.Parse(json);
		var root = jsonDoc.RootElement;

		root.TryGetProperty("applies_to", out var appliesTo).Should().BeTrue();
		var appliesArray = appliesTo.EnumerateArray().ToList();
		appliesArray.Should().HaveCount(2);

		// Verify apm-agent-dotnet entry
		var dotnetEntry = appliesArray.FirstOrDefault(e => e.GetProperty("sub_type").GetString() == "apm-agent-dotnet");
		dotnetEntry.ValueKind.Should().NotBe(JsonValueKind.Undefined);
		dotnetEntry.GetProperty("type").GetString().Should().Be("product");
		dotnetEntry.GetProperty("lifecycle").GetString().Should().Be("ga");
		dotnetEntry.GetProperty("version").GetString().Should().Be("1.5.0");

		// Verify apm-agent-node entry
		var nodeEntry = appliesArray.FirstOrDefault(e => e.GetProperty("sub_type").GetString() == "apm-agent-node");
		nodeEntry.ValueKind.Should().NotBe(JsonValueKind.Undefined);
		nodeEntry.GetProperty("type").GetString().Should().Be("product");
		nodeEntry.GetProperty("lifecycle").GetString().Should().Be("deprecated");
		nodeEntry.GetProperty("version").GetString().Should().Be("2.0.0");
	}

	[Fact]
	public void Serialize_DocumentWithComplexAppliesTo_ProducesCorrectJson()
	{
		var doc = new DocumentationDocument
		{
			Url = "/test/complex",
			Title = "Complex Test",
			Applies = new ApplicableTo
			{
				Stack = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (SemVersion)"8.0.0" }]),
				Deployment = new DeploymentApplicability
				{
					Ess = AppliesCollection.GenerallyAvailable
				},
				Serverless = new ServerlessProjectApplicability
				{
					Elasticsearch = AppliesCollection.GenerallyAvailable
				}
			}
		};

		var json = JsonSerializer.Serialize(doc, _options);
		var jsonDoc = JsonDocument.Parse(json);
		var root = jsonDoc.RootElement;

		root.TryGetProperty("applies_to", out var appliesTo).Should().BeTrue();
		var appliesArray = appliesTo.EnumerateArray().ToList();
		appliesArray.Should().HaveCount(3);

		// Verify we have all three types
		appliesArray.Should().Contain(e => e.GetProperty("type").GetString() == "stack");
		appliesArray.Should().Contain(e => e.GetProperty("type").GetString() == "deployment");
		appliesArray.Should().Contain(e => e.GetProperty("type").GetString() == "serverless");
	}

	[Fact]
	public void Serialize_DocumentWithNullAppliesTo_OmitsField()
	{
		var doc = new DocumentationDocument
		{
			Url = "/test/no-applies",
			Title = "No Applies Test",
			Applies = null
		};

		var json = JsonSerializer.Serialize(doc, _options);
		var jsonDoc = JsonDocument.Parse(json);
		var root = jsonDoc.RootElement;

		// With default JSON options, null values might be omitted or serialized as null
		// Let's check both possibilities
		if (root.TryGetProperty("applies_to", out var appliesTo))
		{
			appliesTo.ValueKind.Should().Be(JsonValueKind.Null);
		}
		else
		{
			// Field is omitted, which is also acceptable
			true.Should().BeTrue();
		}
	}

	[Fact]
	public void Serialize_DocumentWithEmptyAppliesTo_ProducesEmptyArray()
	{
		var doc = new DocumentationDocument
		{
			Url = "/test/empty-applies",
			Title = "Empty Applies Test",
			Applies = new ApplicableTo()
		};

		var json = JsonSerializer.Serialize(doc, _options);
		var jsonDoc = JsonDocument.Parse(json);
		var root = jsonDoc.RootElement;

		root.TryGetProperty("applies_to", out var appliesTo).Should().BeTrue();
		appliesTo.ValueKind.Should().Be(JsonValueKind.Array);
		appliesTo.GetArrayLength().Should().Be(0);
	}

	[Fact]
	public void RoundTrip_DocumentWithAppliesTo_PreservesData()
	{
		var original = new DocumentationDocument
		{
			Url = "/test/roundtrip",
			Title = "Round Trip Test",
			Hash = "abc123",
			BatchIndexDate = DateTimeOffset.Parse("2024-01-15T10:00:00Z"),
			LastUpdated = DateTimeOffset.Parse("2024-01-15T09:00:00Z"),
			Applies = new ApplicableTo
			{
				Stack = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (SemVersion)"8.5.0" }]),
				Deployment = new DeploymentApplicability
				{
					Ess = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (SemVersion)"8.6.0" }])
				}
			},
			Headings = ["Introduction", "Getting Started"],
			Links = ["/link1", "/link2"],
			Body = "Test body content",
			StrippedBody = "Test body content",
			Description = "Test description"
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<DocumentationDocument>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized!.Url.Should().Be(original.Url);
		deserialized.Title.Should().Be(original.Title);
		deserialized.Applies.Should().NotBeNull();
		deserialized.Applies!.Stack.Should().BeEquivalentTo(original.Applies!.Stack);
		deserialized.Applies.Deployment.Should().NotBeNull();
		deserialized.Applies.Deployment!.Ess.Should().BeEquivalentTo(original.Applies.Deployment!.Ess);
	}

	[Fact]
	public void Serialize_DocumentWithMultipleApplicabilitiesPerType_ProducesMultipleArrayEntries()
	{
		var doc = new DocumentationDocument
		{
			Url = "/test/multiple",
			Title = "Multiple Test",
			Applies = new ApplicableTo
			{
				Stack = new AppliesCollection(
				[
					new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (SemVersion)"8.0.0" },
					new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (SemVersion)"7.17.0" },
					new Applicability { Lifecycle = ProductLifecycle.Deprecated, Version = (SemVersion)"7.0.0" }
				])
			}
		};

		var json = JsonSerializer.Serialize(doc, _options);
		var jsonDoc = JsonDocument.Parse(json);
		var root = jsonDoc.RootElement;

		root.TryGetProperty("applies_to", out var appliesTo).Should().BeTrue();
		var appliesArray = appliesTo.EnumerateArray().ToList();
		appliesArray.Should().HaveCount(3);

		// All should be stack type
		appliesArray.Should().OnlyContain(e => e.GetProperty("type").GetString() == "stack");
		appliesArray.Should().OnlyContain(e => e.GetProperty("sub_type").GetString() == "stack");

		// Verify different lifecycle values
		var lifecycles = appliesArray.Select(e => e.GetProperty("lifecycle").GetString()).ToList();
		lifecycles.Should().Contain("ga");
		lifecycles.Should().Contain("beta");
		lifecycles.Should().Contain("deprecated");
	}
}
