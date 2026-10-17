// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Types;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// A type page end to end: navigation lists it under the reference types group, and a row of that type on an operation page links
/// to the very URL the page is served at, instead of listing the type again.
/// </summary>
[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class TypePageWiringTests(ApiExplorerFixture fixture)
{
	private const string Spec =
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "Ingest", "version": "1" },
		  "paths": {
		    "/_ingest/pipeline/{id}": {
		      "put": {
		        "operationId": "ingest-put-pipeline",
		        "summary": "Create or update a pipeline",
		        "tags": ["ingest"],
		        "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } } ],
		        "requestBody": {
		          "content": {
		            "application/json": {
		              "schema": {
		                "type": "object",
		                "properties": {
		                  "description": { "type": "string" },
		                  "processors": { "type": "array", "items": { "$ref": "#/components/schemas/ingest._types.ProcessorContainer" } }
		                }
		              }
		            }
		          }
		        },
		        "responses": { "200": { "description": "OK" } }
		      }
		    }
		  },
		  "components": {
		    "schemas": {
		      "ingest._types.ProcessorContainer": {
		        "type": "object",
		        "properties": {
		          "set": { "$ref": "#/components/schemas/ingest._types.SetProcessor" },
		          "remove": { "$ref": "#/components/schemas/ingest._types.RemoveProcessor" }
		        }
		      },
		      "ingest._types.SetProcessor": {
		        "type": "object",
		        "properties": {
		          "field": { "type": "string" },
		          "on_failure": { "type": "array", "items": { "$ref": "#/components/schemas/ingest._types.ProcessorContainer" } }
		        }
		      },
		      "ingest._types.RemoveProcessor": { "type": "object", "properties": { "field": { "type": "string" } } }
		    }
		  }
		}
		""";

	[Test]
	public async Task ProcessorContainer_GetsAnIngestPageThatPipelineRowsLinkTo()
	{
		var document = await LoadSpecAsync(Spec);
		var navigation = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			fixture.Context,
			PassthroughMarkdownRenderer.Instance
		).CreateNavigation("ingest-wiring", document);
		var items = ApiExplorerFixture.Walk(navigation).ToArray();

		var typePage = items.OfType<SchemaNavigationItem>().Single(n => n.Model.SchemaId == "ingest._types.ProcessorContainer");
		items
			.OfType<SchemaCategoryNavigationItem>()
			.Single(c => c.NavigationItems.Contains(typePage))
			.NavigationTitle
			.Should()
			.Be(TypePages.GroupTitle);

		var operation = items.OfType<OperationNavigationItem>().Single(n => n.Model.Operation.OperationId == "ingest-put-pipeline");
		var page = OperationPageModel.Create(operation.Model, RenderContext(document, operation));
		var processors = page.RequestProperties!.Items.Single(p => p.Name == "processors");
		processors.TypeLink!.Url.Should().Be(typePage.Url.TrimEnd('/'), "the row links to the page navigation serves");
		processors.Children.Kind.Should().Be(ChildKind.None, "the processors are listed on their own page, not again here");

		var schemaPage = SchemaPageModel.Create(typePage.Model, RenderContext(document, typePage));
		schemaPage.Properties!.Items.Select(p => p.Name).Should().Equal("set", "remove");
		var onFailure = schemaPage.Properties.Items[0].Children.Properties!.Items.Single(p => p.Name == "on_failure");
		onFailure.TypeLink.Should().BeNull("the page never links to itself");
	}

	private const string MappingSpec =
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "Mappings", "version": "1" },
		  "paths": {
		    "/{index}/_mapping": {
		      "put": {
		        "operationId": "indices-put-mapping",
		        "summary": "Update field mappings",
		        "tags": ["indices"],
		        "parameters": [ { "name": "index", "in": "path", "required": true, "schema": { "type": "string" } } ],
		        "requestBody": {
		          "content": {
		            "application/json": {
		              "schema": {
		                "type": "object",
		                "properties": {
		                  "properties": { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/_types.mapping.Property" } }
		                }
		              }
		            }
		          }
		        },
		        "responses": { "200": { "description": "OK" } }
		      }
		    }
		  },
		  "components": {
		    "schemas": {
		      "_types.mapping.Property": {
		        "discriminator": {
		          "propertyName": "type",
		          "mapping": {
		            "text": "#/components/schemas/_types.mapping.TextProperty",
		            "object": "#/components/schemas/_types.mapping.ObjectProperty"
		          }
		        },
		        "oneOf": [
		          { "$ref": "#/components/schemas/_types.mapping.TextProperty" },
		          { "$ref": "#/components/schemas/_types.mapping.ObjectProperty" }
		        ]
		      },
		      "_types.mapping.TextProperty": {
		        "type": "object",
		        "properties": {
		          "type": { "type": "string", "enum": ["text"] },
		          "analyzer": { "type": "string" },
		          "fields": { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/_types.mapping.Property" } }
		        },
		        "required": ["type"]
		      },
		      "_types.mapping.ObjectProperty": {
		        "type": "object",
		        "properties": {
		          "type": { "type": "string", "enum": ["object"] },
		          "properties": { "type": "object", "additionalProperties": { "$ref": "#/components/schemas/_types.mapping.Property" } }
		        }
		      }
		    }
		  }
		}
		""";

	[Test]
	public async Task Property_GetsAFieldMappingPageThatMappingRowsLinkTo()
	{
		var document = await LoadSpecAsync(MappingSpec);
		var navigation = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			fixture.Context,
			PassthroughMarkdownRenderer.Instance
		).CreateNavigation("mapping-wiring", document);
		var items = ApiExplorerFixture.Walk(navigation).ToArray();

		var typePage = items.OfType<SchemaNavigationItem>().Single(n => n.Model.SchemaId == "_types.mapping.Property");
		typePage.NavigationTitle.Should().Be("Field mapping");

		var operation = items.OfType<OperationNavigationItem>().Single(n => n.Model.Operation.OperationId == "indices-put-mapping");
		var page = OperationPageModel.Create(operation.Model, RenderContext(document, operation));
		var properties = page.RequestProperties!.Items.Single(p => p.Name == "properties");
		properties.TypeLink!.Url.Should().Be(typePage.Url.TrimEnd('/'), "the row links to the page navigation serves");
		properties.TypeLink.TypeName.Should().Be("Field mapping");

		var schemaPage = SchemaPageModel.Create(typePage.Model, RenderContext(document, typePage));
		var variants = schemaPage.UnionVariants!.Variants.Select(v => v.DisplayName).ToArray();
		ChipLabels.Shorten(variants).Should().Equal(["text", "object"], "the chips read as the type a mapping sets");
	}

	private ApiRenderContext RenderContext(OpenApiDocument document, INavigationItem current) =>
		new(fixture.Context, document, new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context)))
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = current,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance
		};
}
