// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Versions;
using Nullean.Snapshots.Json;

namespace Elastic.Markdown.Tests.Search;

public class DocumentationDocumentSerializationTests
{
	private readonly JsonSerializerOptions _options = new(SourceGenerationContext.Default.Options);

	[Test]
	public void SerializeDocumentWithStackAppliesToProducesCorrectJson()
	{
		var doc = new DocumentationDocument
		{
			ContentType = "doc",
			Path = "/test/page",
			Title = "Test Page",
			SearchTitle = "Test Page",
			Applies = new ApplicableTo { Stack = AppliesCollection.GenerallyAvailable }.ToAppliesTo()
		};

		var json = JsonSerializer.Serialize(doc, _options);
		json.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "applies_to": [
			    {
			      "type": "stack",
			      "sub_type": "stack",
			      "lifecycle": "ga",
			      "version": "all"
			    }
			  ],
			  "content_hash": "",
			  "content_last_updated": "0001-01-01T00:00:00+00:00",
			  "title": "Test Page",
			  "search_title": "Test Page",
			  "content_type": "doc",
			  "path": "/test/page",
			  "url": "/test/page",
			  "hash": "",
			  "batch_index_date": "0001-01-01T00:00:00+00:00",
			  "last_updated": "0001-01-01T00:00:00+00:00",
			  "headings": [],
			  "parents": [],
			  "content_tier": "reference",
			  "navigation": {
			    "depth": 50,
			    "table_of_contents": 50
			  }
			}
			"""
		);
	}

	[Test]
	public void SerializeDocumentWithDeploymentAppliesToProducesCorrectJson()
	{
		var doc = new DocumentationDocument
		{
			ContentType = "doc",
			Path = "/test/deployment",
			Title = "Deployment Test",
			SearchTitle = "Deployment Test",
			Applies = new ApplicableTo
			{
				Deployment = new DeploymentApplicability
				{
					Ess = AppliesCollection.GenerallyAvailable,
					Ece = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"3.5.0" }])
				}
			}.ToAppliesTo()
		};

		var json = JsonSerializer.Serialize(doc, _options);
		json.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "applies_to": [
			    {
			      "type": "deployment",
			      "sub_type": "ece",
			      "lifecycle": "beta",
			      "version": "3.5+"
			    },
			    {
			      "type": "deployment",
			      "sub_type": "ess",
			      "lifecycle": "ga",
			      "version": "all"
			    }
			  ],
			  "content_hash": "",
			  "content_last_updated": "0001-01-01T00:00:00+00:00",
			  "title": "Deployment Test",
			  "search_title": "Deployment Test",
			  "content_type": "doc",
			  "path": "/test/deployment",
			  "url": "/test/deployment",
			  "hash": "",
			  "batch_index_date": "0001-01-01T00:00:00+00:00",
			  "last_updated": "0001-01-01T00:00:00+00:00",
			  "headings": [],
			  "parents": [],
			  "content_tier": "reference",
			  "navigation": {
			    "depth": 50,
			    "table_of_contents": 50
			  }
			}
			"""
		);
	}

	[Test]
	public void SerializeDocumentWithServerlessAppliesToProducesCorrectJson()
	{
		var doc = new DocumentationDocument
		{
			ContentType = "doc",
			Path = "/test/serverless",
			Title = "Serverless Test",
			SearchTitle = "Serverless Test",
			Applies = new ApplicableTo
			{
				Serverless = new ServerlessProjectApplicability
				{
					Elasticsearch = new AppliesCollection([
						new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"8.0.0" }
					]),
					Security = new AppliesCollection([
						new Applicability { Lifecycle = ProductLifecycle.TechnicalPreview, Version = (VersionSpec)"1.0.0" }
					]),
					VectorDatabase = AppliesCollection.GenerallyAvailable
				}
			}.ToAppliesTo()
		};

		var json = JsonSerializer.Serialize(doc, _options);
		json.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "applies_to": [
			    {
			      "type": "serverless",
			      "sub_type": "elasticsearch",
			      "lifecycle": "ga",
			      "version": "8.0+"
			    },
			    {
			      "type": "serverless",
			      "sub_type": "security",
			      "lifecycle": "preview",
			      "version": "1.0+"
			    },
			    {
			      "type": "serverless",
			      "sub_type": "vectordb",
			      "lifecycle": "ga",
			      "version": "all"
			    }
			  ],
			  "content_hash": "",
			  "content_last_updated": "0001-01-01T00:00:00+00:00",
			  "title": "Serverless Test",
			  "search_title": "Serverless Test",
			  "content_type": "doc",
			  "path": "/test/serverless",
			  "url": "/test/serverless",
			  "hash": "",
			  "batch_index_date": "0001-01-01T00:00:00+00:00",
			  "last_updated": "0001-01-01T00:00:00+00:00",
			  "headings": [],
			  "parents": [],
			  "content_tier": "reference",
			  "navigation": {
			    "depth": 50,
			    "table_of_contents": 50
			  }
			}
			"""
		);
	}

	[Test]
	public void SerializeDocumentWithProductAppliesToProducesCorrectJson()
	{
		var doc = new DocumentationDocument
		{
			ContentType = "doc",
			Path = "/test/product",
			Title = "Product Test",
			SearchTitle = "Product Test",
			Applies = new ApplicableTo
			{
				Product = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"2.0.0" }])
			}.ToAppliesTo()
		};

		var json = JsonSerializer.Serialize(doc, _options);
		json.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "applies_to": [
			    {
			      "type": "product",
			      "sub_type": "product",
			      "lifecycle": "beta",
			      "version": "2.0+"
			    }
			  ],
			  "content_hash": "",
			  "content_last_updated": "0001-01-01T00:00:00+00:00",
			  "title": "Product Test",
			  "search_title": "Product Test",
			  "content_type": "doc",
			  "path": "/test/product",
			  "url": "/test/product",
			  "hash": "",
			  "batch_index_date": "0001-01-01T00:00:00+00:00",
			  "last_updated": "0001-01-01T00:00:00+00:00",
			  "headings": [],
			  "parents": [],
			  "content_tier": "reference",
			  "navigation": {
			    "depth": 50,
			    "table_of_contents": 50
			  }
			}
			"""
		);
	}

	[Test]
	public void SerializeDocumentWithProductApplicabilityProducesCorrectJson()
	{
		var doc = new DocumentationDocument
		{
			ContentType = "doc",
			Path = "/test/apm",
			Title = "APM Test",
			SearchTitle = "APM Test",
			Applies = new ApplicableTo
			{
				ProductApplicability = new ProductApplicability
				{
					ApmAgentDotnet = new AppliesCollection([
						new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"1.5.0" }
					]),
					ApmAgentNode = new AppliesCollection([
						new Applicability { Lifecycle = ProductLifecycle.Deprecated, Version = (VersionSpec)"2.0.0" }
					])
				}
			}.ToAppliesTo()
		};

		var json = JsonSerializer.Serialize(doc, _options);
		json.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "applies_to": [
			    {
			      "type": "product",
			      "sub_type": "apm-agent-dotnet",
			      "lifecycle": "ga",
			      "version": "1.5+"
			    },
			    {
			      "type": "product",
			      "sub_type": "apm-agent-node",
			      "lifecycle": "deprecated",
			      "version": "2.0+"
			    }
			  ],
			  "content_hash": "",
			  "content_last_updated": "0001-01-01T00:00:00+00:00",
			  "title": "APM Test",
			  "search_title": "APM Test",
			  "content_type": "doc",
			  "path": "/test/apm",
			  "url": "/test/apm",
			  "hash": "",
			  "batch_index_date": "0001-01-01T00:00:00+00:00",
			  "last_updated": "0001-01-01T00:00:00+00:00",
			  "headings": [],
			  "parents": [],
			  "content_tier": "reference",
			  "navigation": {
			    "depth": 50,
			    "table_of_contents": 50
			  }
			}
			"""
		);
	}

	[Test]
	public void SerializeDocumentWithComplexAppliesToProducesCorrectJson()
	{
		var doc = new DocumentationDocument
		{
			ContentType = "doc",
			Path = "/test/complex",
			Title = "Complex Test",
			SearchTitle = "Complex Test",
			Applies = new ApplicableTo
			{
				Stack = new AppliesCollection([
					new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"8.0.0" }
				]),
				Deployment = new DeploymentApplicability { Ess = AppliesCollection.GenerallyAvailable },
				Serverless = new ServerlessProjectApplicability { Elasticsearch = AppliesCollection.GenerallyAvailable }
			}.ToAppliesTo()
		};

		var json = JsonSerializer.Serialize(doc, _options);
		json.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "applies_to": [
			    {
			      "type": "stack",
			      "sub_type": "stack",
			      "lifecycle": "ga",
			      "version": "8.0+"
			    },
			    {
			      "type": "deployment",
			      "sub_type": "ess",
			      "lifecycle": "ga",
			      "version": "all"
			    },
			    {
			      "type": "serverless",
			      "sub_type": "elasticsearch",
			      "lifecycle": "ga",
			      "version": "all"
			    }
			  ],
			  "content_hash": "",
			  "content_last_updated": "0001-01-01T00:00:00+00:00",
			  "title": "Complex Test",
			  "search_title": "Complex Test",
			  "content_type": "doc",
			  "path": "/test/complex",
			  "url": "/test/complex",
			  "hash": "",
			  "batch_index_date": "0001-01-01T00:00:00+00:00",
			  "last_updated": "0001-01-01T00:00:00+00:00",
			  "headings": [],
			  "parents": [],
			  "content_tier": "reference",
			  "navigation": {
			    "depth": 50,
			    "table_of_contents": 50
			  }
			}
			"""
		);
	}

	[Test]
	public void SerializeDocumentWithNullAppliesToOmitsField()
	{
		var doc = new DocumentationDocument
		{
			ContentType = "doc",
			Path = "/test/no-applies",
			Title = "No Applies Test",
			SearchTitle = "No Applies Test",
			Applies = null
		};

		var json = JsonSerializer.Serialize(doc, _options);
		json.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "content_hash": "",
			  "content_last_updated": "0001-01-01T00:00:00+00:00",
			  "title": "No Applies Test",
			  "search_title": "No Applies Test",
			  "content_type": "doc",
			  "path": "/test/no-applies",
			  "url": "/test/no-applies",
			  "hash": "",
			  "batch_index_date": "0001-01-01T00:00:00+00:00",
			  "last_updated": "0001-01-01T00:00:00+00:00",
			  "headings": [],
			  "parents": [],
			  "content_tier": "reference",
			  "navigation": {
			    "depth": 50,
			    "table_of_contents": 50
			  }
			}
			"""
		);
	}

	[Test]
	public void SerializeDocumentWithEmptyAppliesToProducesEmptyArray()
	{
		var doc = new DocumentationDocument
		{
			ContentType = "doc",
			Path = "/test/empty-applies",
			Title = "Empty Applies Test",
			SearchTitle = "Empty Applies Test",
			Applies = new ApplicableTo().ToAppliesTo()
		};

		var json = JsonSerializer.Serialize(doc, _options);
		json.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "applies_to": [],
			  "content_hash": "",
			  "content_last_updated": "0001-01-01T00:00:00+00:00",
			  "title": "Empty Applies Test",
			  "search_title": "Empty Applies Test",
			  "content_type": "doc",
			  "path": "/test/empty-applies",
			  "url": "/test/empty-applies",
			  "hash": "",
			  "batch_index_date": "0001-01-01T00:00:00+00:00",
			  "last_updated": "0001-01-01T00:00:00+00:00",
			  "headings": [],
			  "parents": [],
			  "content_tier": "reference",
			  "navigation": {
			    "depth": 50,
			    "table_of_contents": 50
			  }
			}
			"""
		);
	}

	[Test]
	public void RoundTripDocumentWithAppliesToPreservesData()
	{
		var originalApplies = new ApplicableTo
		{
			Stack = new AppliesCollection([
				new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"8.5.0" }
			]),
			Deployment = new DeploymentApplicability
			{
				Ess = new AppliesCollection([new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"8.6.0" }])
			}
		}.ToAppliesTo();

		var original = new DocumentationDocument
		{
			ContentType = "doc",
			Path = "/test/roundtrip",
			Title = "Round Trip Test",
			SearchTitle = "Round Trip Test",
			Hash = "abc123",
			BatchIndexDate = DateTimeOffset.Parse("2024-01-15T10:00:00Z", CultureInfo.InvariantCulture),
			LastUpdated = DateTimeOffset.Parse("2024-01-15T09:00:00Z", CultureInfo.InvariantCulture),
			ContentLastUpdated = DateTimeOffset.Parse("2024-01-14T08:00:00Z", CultureInfo.InvariantCulture),
			ContentBodyHash = "abc123def456abc1",
			Applies = originalApplies,
			Headings = ["Introduction", "Getting Started"],
			Links = ["/link1", "/link2"],
			Body = "Test body content",
			Description = "Test description"
		};

		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<DocumentationDocument>(json, _options);

		deserialized.Should().NotBeNull();
		deserialized.Path.Should().Be(original.Path);
		deserialized.Title.Should().Be(original.Title);
		deserialized.Applies.Should().NotBeNull();
		deserialized.Applies.Should().HaveCount(2);
		deserialized.Applies.Should().Contain(e => e.Type == "stack" && e.Lifecycle == "ga" && e.Version == "8.5+");
		deserialized
			.Applies
			.Should()
			.Contain(e => e.Type == "deployment" && e.SubType == "ess" && e.Lifecycle == "beta" && e.Version == "8.6+");
		deserialized.ContentLastUpdated.Should().Be(original.ContentLastUpdated);
		deserialized.ContentBodyHash.Should().Be(original.ContentBodyHash);
		deserialized.ContentType.Should().Be(original.ContentType);
	}

	[Test]
	public void SerializeDocumentationDocument_IncludesContentType_MatchingType()
	{
		foreach (var type in new[] { "doc", "api" })
		{
			var doc = new DocumentationDocument { ContentType = type, Path = $"/test/{type}", Title = "T", SearchTitle = "T" };

			var json = JsonSerializer.Serialize(doc, _options);
			using var parsed = JsonDocument.Parse(json);
			var root = parsed.RootElement;

			root.GetProperty("content_type").GetString().Should().Be(type);
			root.GetProperty("content_type").GetString().Should().Be(type);
		}
	}

	[Test]
	public void ContentType_FromJson_Overrides_Type()
	{
		var json =
			"""
		{
			"title": "Legacy",
			"search_title": "Legacy",
			"path": "/x",
			"type": "doc",
			"hash": "h",
			"content_type": "archived-docs"
		}
		""";

		var deserialized = JsonSerializer.Deserialize<DocumentationDocument>(json, _options);

		deserialized.Should().NotBeNull();
		// Type is now the polymorphic CLR discriminator (always "docs" for DocumentationDocument);
		// docs-builder's legacy `type: "doc"` JSON value is no longer mapped to a property.
		deserialized.Type.Should().Be("docs");
		deserialized.ContentType.Should().Be("archived-docs");
	}

	[Test]
	public void SerializeDocumentWithMultipleApplicabilitiesPerTypeProducesMultipleArrayEntries()
	{
		var doc = new DocumentationDocument
		{
			ContentType = "doc",
			Path = "/test/multiple",
			Title = "Multiple Test",
			SearchTitle = "Multiple Test",
			Applies = new ApplicableTo
			{
				Stack = new AppliesCollection([
					new Applicability { Lifecycle = ProductLifecycle.GenerallyAvailable, Version = (VersionSpec)"8.0.0" },
					new Applicability { Lifecycle = ProductLifecycle.Beta, Version = (VersionSpec)"7.17.0" },
					new Applicability { Lifecycle = ProductLifecycle.Deprecated, Version = (VersionSpec)"7.0.0" }
				])
			}.ToAppliesTo()
		};

		var json = JsonSerializer.Serialize(doc, _options);
		json.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "applies_to": [
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
			    },
			    {
			      "type": "stack",
			      "sub_type": "stack",
			      "lifecycle": "deprecated",
			      "version": "7.0+"
			    }
			  ],
			  "content_hash": "",
			  "content_last_updated": "0001-01-01T00:00:00+00:00",
			  "title": "Multiple Test",
			  "search_title": "Multiple Test",
			  "content_type": "doc",
			  "path": "/test/multiple",
			  "url": "/test/multiple",
			  "hash": "",
			  "batch_index_date": "0001-01-01T00:00:00+00:00",
			  "last_updated": "0001-01-01T00:00:00+00:00",
			  "headings": [],
			  "parents": [],
			  "content_tier": "reference",
			  "navigation": {
			    "depth": 50,
			    "table_of_contents": 50
			  }
			}
			"""
		);
	}

	[Test]
	public void SerializeDocument_IncludesSourceUrlAsSnakeCaseKeyword()
	{
		var doc = new DocumentationDocument
		{
			Path = "/docs/some-page",
			Title = "Some Page",
			SearchTitle = "Some Page",
			SourceUrl = "https://github.com/elastic/docs-content/blob/main/docs/some-page.md"
		};

		var json = JsonSerializer.Serialize(doc, _options);
		json.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "content_hash": "",
			  "content_last_updated": "0001-01-01T00:00:00+00:00",
			  "source_url": "https://github.com/elastic/docs-content/blob/main/docs/some-page.md",
			  "title": "Some Page",
			  "search_title": "Some Page",
			  "content_type": "docs",
			  "path": "/docs/some-page",
			  "url": "/docs/some-page",
			  "hash": "",
			  "batch_index_date": "0001-01-01T00:00:00+00:00",
			  "last_updated": "0001-01-01T00:00:00+00:00",
			  "headings": [],
			  "parents": [],
			  "content_tier": "reference",
			  "navigation": {
			    "depth": 50,
			    "table_of_contents": 50
			  }
			}
			"""
		);
	}

	[Test]
	public void SerializeDocument_OmitsSourceUrlWhenNull()
	{
		var doc = new DocumentationDocument { Path = "/docs/some-page", Title = "Some Page", SearchTitle = "Some Page", SourceUrl = null };

		var json = JsonSerializer.Serialize(doc, _options);
		json.Should().MatchInlineJsonSnapshot(
			"""
			{
			  "content_hash": "",
			  "content_last_updated": "0001-01-01T00:00:00+00:00",
			  "title": "Some Page",
			  "search_title": "Some Page",
			  "content_type": "docs",
			  "path": "/docs/some-page",
			  "url": "/docs/some-page",
			  "hash": "",
			  "batch_index_date": "0001-01-01T00:00:00+00:00",
			  "last_updated": "0001-01-01T00:00:00+00:00",
			  "headings": [],
			  "parents": [],
			  "content_tier": "reference",
			  "navigation": {
			    "depth": 50,
			    "table_of_contents": 50
			  }
			}
			"""
		);
	}
}
