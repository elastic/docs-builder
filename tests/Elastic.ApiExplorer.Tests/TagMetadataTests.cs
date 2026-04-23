// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Nullean.ScopedFileSystem;

namespace Elastic.ApiExplorer.Tests;

public class TagMetadataTests
{
	[Fact]
	public async Task ApiTag_WithXDisplayName_UsesDisplayNameForNavigation()
	{
		// Arrange - minimal OpenAPI spec with x-displayName and multiple tags to trigger TagNavigationItem creation
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": {
		    "title": "Test API",
		    "version": "1.0.0"
		  },
		  "paths": {
		    "/test-tasks": {
		      "get": {
		        "operationId": "test-tasks-operation",
		        "tags": ["tasks"],
		        "responses": {
		          "200": {
		            "description": "Success"
		          }
		        }
		      }
		    },
		    "/test-transform": {
		      "get": {
		        "operationId": "test-transform-operation",
		        "tags": ["transform"],
		        "responses": {
		          "200": {
		            "description": "Success"
		          }
		        }
		      }
		    }
		  },
		  "tags": [
		    {
		      "name": "tasks",
		      "x-displayName": "Task management"
		    },
		    {
		      "name": "transform",
		      "x-displayName": "Transform"
		    }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);

		// Act
		var navigation = generator.CreateNavigation("test", openApiDocument);

		// Assert
		navigation.Should().NotBeNull();

		// Navigate to the tag navigation item
		var tagNavItem = FindTagNavigationItem(navigation, "tasks");
		tagNavItem.Should().NotBeNull();

		// The NavigationTitle should use the display name
		tagNavItem.NavigationTitle.Should().Be("Task management");

		// The Id should be stable and based on the canonical tag name (ShortId creates a hash)
		tagNavItem.Id.Should().NotBeNull();

		// Verify the underlying tag model has the correct canonical name and display name
		tagNavItem.Index.Model.Should().BeOfType<ApiTag>();
		var apiTag = tagNavItem.Index.Model;
		apiTag.Name.Should().Be("tasks"); // canonical name
		apiTag.DisplayName.Should().Be("Task management"); // display name
	}

	[Fact]
	public async Task ApiTag_WithoutXDisplayName_FallsBackToCanonicalName()
	{
		// Arrange - spec without x-displayName, multiple tags to trigger TagNavigationItem creation
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": {
		    "title": "Test API",
		    "version": "1.0.0"
		  },
		  "paths": {
		    "/test-transform": {
		      "get": {
		        "operationId": "test-transform-operation",
		        "tags": ["transform"],
		        "responses": {
		          "200": {
		            "description": "Success"
		          }
		        }
		      }
		    },
		    "/test-search": {
		      "get": {
		        "operationId": "test-search-operation",
		        "tags": ["search"],
		        "responses": {
		          "200": {
		            "description": "Success"
		          }
		        }
		      }
		    }
		  },
		  "tags": [
		    {
		      "name": "transform"
		    },
		    {
		      "name": "search"
		    }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);

		// Act
		var navigation = generator.CreateNavigation("test", openApiDocument);

		// Assert
		var tagNavItem = FindTagNavigationItem(navigation, "transform");
		tagNavItem.Should().NotBeNull();

		// Should fallback to canonical name when no x-displayName
		tagNavItem.NavigationTitle.Should().Be("transform");
	}

	[Fact]
	public async Task ApiTag_WithMultipleTagsAndDisplayNames_ParsesCorrectly()
	{
		// Arrange - multiple tags with different display name scenarios
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": {
		    "title": "Test API",
		    "version": "1.0.0"
		  },
		  "paths": {
		    "/tasks": {
		      "get": {
		        "operationId": "get-tasks",
		        "tags": ["tasks"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/transform": {
		      "get": {
		        "operationId": "get-transform",
		        "tags": ["transform"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/usage": {
		      "get": {
		        "operationId": "get-usage",
		        "tags": ["xpack"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    }
		  },
		  "tags": [
		    {
		      "name": "tasks",
		      "x-displayName": "Task management"
		    },
		    {
		      "name": "transform",
		      "x-displayName": "Transform"
		    },
		    {
		      "name": "xpack",
		      "x-displayName": "Usage"
		    }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);

		// Act
		var navigation = generator.CreateNavigation("test", openApiDocument);

		// Assert - test real examples from the plan
		var tasksTag = FindTagNavigationItem(navigation, "tasks");
		tasksTag.Should().NotBeNull();
		tasksTag.NavigationTitle.Should().Be("Task management");

		var transformTag = FindTagNavigationItem(navigation, "transform");
		transformTag.Should().NotBeNull();
		transformTag.NavigationTitle.Should().Be("Transform");

		var xpackTag = FindTagNavigationItem(navigation, "xpack");
		xpackTag.Should().NotBeNull();
		xpackTag.NavigationTitle.Should().Be("Usage");
	}

	[Fact]
	public async Task ApiTag_StableNavigationIds_UsesCanonicalTagName()
	{
		// Arrange - tag where display name differs significantly from canonical name, multiple tags for TagNavigationItem creation
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": {
		    "title": "Test API",
		    "version": "1.0.0"
		  },
		  "paths": {
		    "/test-ml": {
		      "get": {
		        "operationId": "test-ml-operation",
		        "tags": ["ml_anomaly"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/test-other": {
		      "get": {
		        "operationId": "test-other-operation",
		        "tags": ["other"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    }
		  },
		  "tags": [
		    {
		      "name": "ml_anomaly",
		      "x-displayName": "Machine Learning Anomaly Detection APIs"
		    },
		    {
		      "name": "other",
		      "x-displayName": "Other APIs"
		    }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);

		// Act
		var navigation = generator.CreateNavigation("test", openApiDocument);

		// Assert
		var tagNavItem = FindTagNavigationItem(navigation, "ml_anomaly");
		tagNavItem.Should().NotBeNull();

		// Display name is user-friendly
		tagNavItem.NavigationTitle.Should().Be("Machine Learning Anomaly Detection APIs");

		// But ID should be based on canonical name for URL stability
		// ShortId.Create uses the canonical name as input, so verify the pattern
		tagNavItem.Id.Should().NotBeNull();
		tagNavItem.Id.Should().NotBe("Machine Learning Anomaly Detection APIs"); // Not the display name
	}

	private static async Task<(OpenApiGenerator generator, OpenApiDocument document)> CreateGeneratorWithSpec(string openApiJson)
	{
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(collector, FileSystemFactory.RealRead, configurationContext);

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);

		// Parse the OpenAPI spec directly from JSON using a stream
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(openApiJson));
		var settings = new OpenApiReaderSettings { LeaveStreamOpen = false };
		var result = await OpenApiDocument.LoadAsync(stream, settings: settings);

		var parseErrors = result.Diagnostic?.Errors;
		if (parseErrors is not null && parseErrors.Any())
		{
			throw new InvalidOperationException($"OpenAPI parsing failed: {string.Join(", ", parseErrors.Select(e => e.Message))}");
		}

		return (generator, result.Document!);
	}

	private static TagNavigationItem? FindTagNavigationItem(LandingNavigationItem navigation, string tagName)
	{
		// Navigate through the structure to find the tag
		// Structure can be: root -> [classification] -> tag or root -> tag directly

		// Check direct children first
		foreach (var item in navigation.NavigationItems)
		{
			if (item is TagNavigationItem tagItem && IsTagForName(tagItem, tagName))
				return tagItem;

			// Check children of classification items
			if (item is ClassificationNavigationItem classificationItem)
			{
				foreach (var classificationChild in classificationItem.NavigationItems)
				{
					if (classificationChild is TagNavigationItem nestedTagItem && IsTagForName(nestedTagItem, tagName))
						return nestedTagItem;
				}
			}
		}

		return null;
	}

	private static bool IsTagForName(TagNavigationItem tagItem, string expectedTagName)
	{
		// Check if the underlying tag model has the expected canonical name
		// Access the ApiTag model through the Index property
		return tagItem.Index.Model is ApiTag tag && tag.Name == expectedTagName;
	}

	[Fact]
	public async Task Tags_WithMixedDisplayNames_SortedAlphabeticallyByDisplayName()
	{
		// Arrange - spec with mixed x-displayName and canonical names
		var openApiJson = /*lang=json*/ """
		{
		  "openapi": "3.0.3",
		  "info": {
		    "title": "Test API",
		    "version": "1.0.0"
		  },
		  "paths": {
		    "/zebra": {
		      "get": {
		        "operationId": "zebra-op",
		        "tags": ["zebra"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/apple": {
		      "get": {
		        "operationId": "apple-op",
		        "tags": ["apple"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/charlie": {
		      "get": {
		        "operationId": "charlie-op",
		        "tags": ["charlie"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    }
		  },
		  "tags": [
		    {
		      "name": "zebra",
		      "x-displayName": "Animal Zoo"
		    },
		    {
		      "name": "apple",
		      "x-displayName": "Fruit Store"  
		    },
		    {
		      "name": "charlie"
		    }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);

		// Act
		var navigation = generator.CreateNavigation("test", openApiDocument);

		// Assert - should be sorted alphabetically by display name: "Animal Zoo", "charlie", "Fruit Store"
		navigation.NavigationItems.Should().HaveCount(3);

		var tagItems = navigation.NavigationItems.OfType<TagNavigationItem>().ToList();
		tagItems.Should().HaveCount(3);

		// Verify sorted order
		tagItems[0].NavigationTitle.Should().Be("Animal Zoo", "First tag should be 'Animal Zoo' (zebra with x-displayName)");
		tagItems[1].NavigationTitle.Should().Be("charlie", "Second tag should be 'charlie' (canonical name, no x-displayName)");
		tagItems[2].NavigationTitle.Should().Be("Fruit Store", "Third tag should be 'Fruit Store' (apple with x-displayName)");
	}

	[Fact]
	public async Task Tags_CaseInsensitiveSorting_WorksCorrectly()
	{
		// Arrange - spec with case variations
		var openApiJson = /*lang=json*/ """
		{
		  "openapi": "3.0.3",
		  "info": {
		    "title": "Test API",
		    "version": "1.0.0"
		  },
		  "paths": {
		    "/bravo": {
		      "get": {
		        "operationId": "bravo-op",
		        "tags": ["bravo"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/alpha": {
		      "get": {
		        "operationId": "alpha-op",
		        "tags": ["alpha"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    }
		  },
		  "tags": [
		    {
		      "name": "bravo",
		      "x-displayName": "beta Service"
		    },
		    {
		      "name": "alpha", 
		      "x-displayName": "Alpha Service"
		    }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);

		// Act
		var navigation = generator.CreateNavigation("test", openApiDocument);

		// Assert - should be sorted case-insensitively: "Alpha Service", "beta Service"
		var tagItems = navigation.NavigationItems.OfType<TagNavigationItem>().ToList();
		tagItems.Should().HaveCount(2);

		tagItems[0].NavigationTitle.Should().Be("Alpha Service", "Should sort case-insensitively");
		tagItems[1].NavigationTitle.Should().Be("beta Service", "Should sort case-insensitively");
	}

	[Fact]
	public async Task Tags_OnlyCanonicalNames_SortedAlphabetically()
	{
		// Arrange - spec with no x-displayName values
		var openApiJson = /*lang=json*/ """
		{
		  "openapi": "3.0.3",
		  "info": {
		    "title": "Test API",
		    "version": "1.0.0"
		  },
		  "paths": {
		    "/zebra": {
		      "get": {
		        "operationId": "zebra-op",
		        "tags": ["zebra"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/alpha": {
		      "get": {
		        "operationId": "alpha-op",
		        "tags": ["alpha"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/mike": {
		      "get": {
		        "operationId": "mike-op",
		        "tags": ["mike"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    }
		  },
		  "tags": [
		    {
		      "name": "zebra",
		      "description": "Zebra operations"
		    },
		    {
		      "name": "alpha",
		      "description": "Alpha operations"
		    },
		    {
		      "name": "mike",
		      "description": "Mike operations" 
		    }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);

		// Act
		var navigation = generator.CreateNavigation("test", openApiDocument);

		// Assert - should be sorted alphabetically by canonical name: "alpha", "mike", "zebra"
		var tagItems = navigation.NavigationItems.OfType<TagNavigationItem>().ToList();
		tagItems.Should().HaveCount(3);

		tagItems[0].NavigationTitle.Should().Be("alpha", "Should sort by canonical name");
		tagItems[1].NavigationTitle.Should().Be("mike", "Should sort by canonical name");
		tagItems[2].NavigationTitle.Should().Be("zebra", "Should sort by canonical name");
	}

	[Fact]
	public async Task Tags_WithinClassification_SortedCorrectly()
	{
		// Arrange - x-tagGroups (Redocly-style) drives classification; sort tags by display name within a group
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": {
		    "title": "Elasticsearch Request & Response Specification",
		    "version": "1.0.0"
		  },
		  "paths": {
		    "/search": {
		      "get": {
		        "operationId": "search-op",
		        "tags": ["search"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/indices": {
		      "get": {
		        "operationId": "indices-op",
		        "tags": ["indices"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/watcher": {
		      "get": {
		        "operationId": "watcher-op",
		        "tags": ["watcher"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/tasks": {
		      "get": {
		        "operationId": "tasks-op",
		        "tags": ["tasks"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    }
		  },
		  "tags": [
		    {
		      "name": "search",
		      "x-displayName": "Search API"
		    },
		    {
		      "name": "indices",
		      "x-displayName": "Indices Management"
		    },
		    {
		      "name": "watcher",
		      "x-displayName": "Watcher API"
		    },
		    {
		      "name": "tasks",
		      "x-displayName": "Task management"
		    }
		  ],
		  "x-tagGroups": [
		    {
		      "name": "Search & Document APIs",
		      "tags": ["search"]
		    },
		    {
		      "name": "Cluster Management",
		      "tags": ["indices"]
		    },
		    {
		      "name": "Information & Monitoring",
		      "tags": ["watcher", "tasks"]
		    }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);

		// Act
		var navigation = generator.CreateNavigation("elasticsearch", openApiDocument);

		// Assert
		var classificationItems = navigation.NavigationItems.OfType<ClassificationNavigationItem>().ToList();
		classificationItems.Should().HaveCount(3, "Should have one nav group per x-tagGroups entry that has operations");

		var infoClassification = classificationItems.First(c => c.NavigationTitle == "Information & Monitoring");

		var tagItems = infoClassification.NavigationItems.OfType<TagNavigationItem>().ToList();
		tagItems.Should().HaveCount(2);

		tagItems[0].NavigationTitle.Should().Be("Task management", "Should sort by displayName");
		tagItems[1].NavigationTitle.Should().Be("Watcher API", "Should sort by displayName");
	}

	[Fact]
	public async Task XTagGroups_Classification_Url_PointsToApiOverview_NotFirstTag()
	{
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": { "title": "ES", "version": "1.0" },
		  "paths": {
		    "/a": { "get": { "operationId": "a1", "tags": ["watcher"], "responses": { "200": { "description": "ok" } } } },
		    "/b": { "get": { "operationId": "b1", "tags": ["tasks"], "responses": { "200": { "description": "ok" } } } },
		    "/c": { "get": { "operationId": "c1", "tags": ["search"], "responses": { "200": { "description": "ok" } } } }
		  },
		  "tags": [
		    { "name": "watcher", "x-displayName": "Watcher" },
		    { "name": "tasks", "x-displayName": "Task management" },
		    { "name": "search" }
		  ],
		  "x-tagGroups": [
		    { "name": "Information", "tags": ["watcher", "tasks"] },
		    { "name": "Search", "tags": ["search"] }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);
		var navigation = generator.CreateNavigation("elasticsearch", openApiDocument);

		var expectedOverviewUrl = navigation.Index.Url;
		var informationGroup = navigation.NavigationItems
			.OfType<ClassificationNavigationItem>()
			.First(c => c.NavigationTitle == "Information");
		informationGroup.Url.Should().Be(expectedOverviewUrl);

		var firstTag = informationGroup.NavigationItems.OfType<TagNavigationItem>().First();
		informationGroup.Url.Should().NotBe(firstTag.Url);
		firstTag.Url.Should().Contain("/tags/");
	}

	[Fact]
	public async Task WithoutXTagGroups_ElasticsearchTitle_UsesFlatTagNavigation()
	{
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": {
		    "title": "Elasticsearch Request & Response Specification",
		    "version": "1.0.0"
		  },
		  "paths": {
		    "/search": {
		      "get": {
		        "operationId": "search-op",
		        "tags": ["search"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/indices": {
		      "get": {
		        "operationId": "indices-op",
		        "tags": ["indices"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    }
		  },
		  "tags": [
		    { "name": "search" },
		    { "name": "indices" }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);
		var navigation = generator.CreateNavigation("elasticsearch", openApiDocument);

		navigation.NavigationItems.OfType<ClassificationNavigationItem>().Should().BeEmpty();
		navigation.NavigationItems.OfType<TagNavigationItem>().Should().HaveCount(2);
	}

	[Fact]
	public async Task XTagGroups_ClassificationOrder_FollowsSpecOrder()
	{
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": { "title": "Order Test", "version": "1.0.0" },
		  "paths": {
		    "/z": {
		      "get": {
		        "operationId": "z-op",
		        "tags": ["ztag"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/a": {
		      "get": {
		        "operationId": "a-op",
		        "tags": ["atag"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/b": {
		      "get": {
		        "operationId": "b-op",
		        "tags": ["btag"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    }
		  },
		  "tags": [
		    { "name": "ztag" },
		    { "name": "atag" },
		    { "name": "btag" }
		  ],
		  "x-tagGroups": [
		    { "name": "Z Group", "tags": ["ztag"] },
		    { "name": "A Group", "tags": ["atag"] },
		    { "name": "B Group", "tags": ["btag"] }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);
		var navigation = generator.CreateNavigation("test", openApiDocument);

		var titles = navigation.NavigationItems
			.OfType<ClassificationNavigationItem>()
			.Select(c => c.NavigationTitle)
			.ToList();

		titles.Should().Equal("Z Group", "A Group", "B Group");
	}

	[Fact]
	public async Task XTagGroups_OrphanTag_AssignsUnknownGroup()
	{
		// Two unlisted tags so the "unknown" classification has multiple tags; each is still a TagNavigationItem.
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": { "title": "Orphan Test", "version": "1.0.0" },
		  "paths": {
		    "/ok": {
		      "get": {
		        "operationId": "ok-op",
		        "tags": ["listed"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/orphan": {
		      "get": {
		        "operationId": "orphan-op",
		        "tags": ["not_in_any_group"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    },
		    "/orphan2": {
		      "get": {
		        "operationId": "orphan2-op",
		        "tags": ["other_orphan"],
		        "responses": {"200": {"description": "Success"}}
		      }
		    }
		  },
		  "tags": [
		    { "name": "listed" },
		    { "name": "not_in_any_group" },
		    { "name": "other_orphan" }
		  ],
		  "x-tagGroups": [
		    { "name": "Main", "tags": ["listed"] }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);
		var navigation = generator.CreateNavigation("test", openApiDocument);

		var classifications = navigation.NavigationItems.OfType<ClassificationNavigationItem>().ToList();
		classifications.Should().HaveCount(2);

		classifications[0].NavigationTitle.Should().Be("Main");
		classifications[1].NavigationTitle.Should().Be("unknown");

		var unknownTags = classifications[1].NavigationItems.OfType<TagNavigationItem>().ToList();
		unknownTags.Should().HaveCount(2);
		unknownTags
			.Select(t => t.Index.Model.Name)
			.OrderBy(name => name, StringComparer.Ordinal)
			.Should()
			.Equal("not_in_any_group", "other_orphan");
	}

	[Fact]
	public async Task Single_Tag_Still_Creates_TagNavigationItem()
	{
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": { "title": "Solo", "version": "1.0" },
		  "paths": {
		    "/solo": {
		      "get": {
		        "operationId": "solo-op",
		        "tags": ["only"],
		        "responses": { "200": { "description": "ok" } }
		      }
		    }
		  },
		  "tags": [
		    { "name": "only", "description": "Solo tag group." }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);
		var navigation = generator.CreateNavigation("test", openApiDocument);

		var items = navigation.NavigationItems.OfType<TagNavigationItem>().ToList();
		items.Should().HaveCount(1);
		var tag = items[0].Index.Model.Should().BeOfType<ApiTag>().Subject;
		tag.Name.Should().Be("only");
		tag.TagUrlSegment.Should().Be("only");
		tag.Description.Should().Be("Solo tag group.");
	}

	[Fact]
	public void GenerateTagMoniker_DataStream_Uses_Hyphen()
	{
		OpenApiGenerator.GenerateTagMoniker("data stream").Should().Be("data-stream");
	}

	[Fact]
	public async Task Tag_Url_Uses_Tags_Segment()
	{
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": { "title": "T", "version": "1.0" },
		  "paths": {
		    "/a": { "get": { "operationId": "a1", "tags": ["alpha"], "responses": { "200": { "description": "ok" } } } },
		    "/b": { "get": { "operationId": "b1", "tags": ["beta"], "responses": { "200": { "description": "ok" } } } }
		  },
		  "tags": [ { "name": "alpha" }, { "name": "beta" } ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);
		var navigation = generator.CreateNavigation("es", openApiDocument);

		var alpha = FindTagNavigationItem(navigation, "alpha");
		alpha.Should().NotBeNull();
		alpha.Url.Should().EndWith("/api/es/tags/alpha/");

		alpha.Index.Model.Should().BeOfType<ApiTag>().Which.TagUrlSegment.Should().Be("alpha");
	}

	[Fact]
	public async Task Tag_Landing_Parses_Description_And_ExternalDocs_Like_Elasticsearch_Connector_Tag()
	{
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": { "title": "ES", "version": "1.0" },
		  "paths": {
		    "/c": { "get": { "operationId": "c1", "tags": ["connector"], "responses": { "200": { "description": "ok" } } } }
		  },
		  "tags": [
		    {
		      "name": "connector",
		      "description": "The connector and sync jobs APIs provide a convenient way to create and manage Elastic connectors and sync jobs in an internal index.",
		      "externalDocs": {
		        "description": "Learn more.",
		        "url": "https://www.elastic.co/docs/reference/search-connectors/api-tutorial"
		      },
		      "x-displayName": "Connector"
		    }
		  ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);
		var navigation = generator.CreateNavigation("elasticsearch", openApiDocument);

		var t = FindTagNavigationItem(navigation, "connector");
		t.Should().NotBeNull();
		t.NavigationTitle.Should().Be("Connector");
		var model = t.Index.Model.Should().BeOfType<ApiTag>().Subject;
		model.Description.Should().Contain("sync jobs");
		model.DisplayName.Should().Be("Connector");
		model.ExternalDocs.Should().NotBeNull();
		model.ExternalDocs.Url.Should().Be("https://www.elastic.co/docs/reference/search-connectors/api-tutorial");
		model.ExternalDocs.Description.Should().Be("Learn more.");
	}

	[Fact]
	public async Task CreateNavigation_Throws_When_Two_Tag_Names_Normalize_To_Same_Url_Segment()
	{
		var openApiJson = /*lang=json,strict*/ """
		{
		  "openapi": "3.0.3",
		  "info": { "title": "X", "version": "1.0" },
		  "paths": {
		    "/a": { "get": { "operationId": "a1", "tags": ["a b"], "responses": { "200": { "description": "ok" } } } },
		    "/b": { "get": { "operationId": "b1", "tags": ["a  b"], "responses": { "200": { "description": "ok" } } } }
		  },
		  "tags": [ { "name": "a b" }, { "name": "a  b" } ]
		}
		""";

		var (generator, openApiDocument) = await CreateGeneratorWithSpec(openApiJson);

		var act = () => generator.CreateNavigation("test", openApiDocument);
		act.Should()
			.Throw<InvalidOperationException>()
			.WithMessage("*tag URL segment conflict*");
	}
}
