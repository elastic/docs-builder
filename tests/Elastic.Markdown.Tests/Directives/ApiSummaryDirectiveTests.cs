// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Myst.Directives.ApiSummary;

namespace Elastic.Markdown.Tests.Directives;

public class ApiSummaryOperationsDirectiveTests(ITestOutputHelper output) : DirectiveTest<ApiSummaryBlock>(output,
"""
:::{api-summary}
:product: testapi
:type: operations
:tag: search
:::
""")
{
	private const string MinimalOpenApiJson =
							 /*lang=json,strict*/
							 """
		{
		  "openapi": "3.0.0",
		  "info": { "title": "Directive Test API", "version": "1.0.0" },
		  "paths": {
		    "/items": {
		      "get": {
		        "tags": ["search"],
		        "operationId": "listItems",
		        "responses": { "200": { "description": "ok" } }
		      }
		    }
		  }
		}
		""";

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("docs/minimal-openapi.json", new MockFileData(MinimalOpenApiJson));
	}

	protected override IReadOnlyDictionary<string, string>? GetApiSpecs() =>
		new Dictionary<string, string> { ["testapi"] = "minimal-openapi.json" };

	[Fact]
	public void ParsesApiSummaryBlock() =>
		Block.Should().NotBeNull().And.Match<ApiSummaryBlock>(b => b.Directive == "api-summary");

	[Fact]
	public void FinalizeAndValidate_ReadsTagProductAndType()
	{
		Block!.Tag.Should().Be("search");
		Block.Product.Should().Be("testapi");
		Block.Type.Should().Be("operations");
	}

	[Fact]
	public void RendersOperationsTable_FromOpenApiSpec()
	{
		Html.Should().Contain("api-overview");
		Html.Should().Contain("api-method-get");
		Html.Should().Contain("/items");
		Html.Should().Contain("/api/testapi/listItems");
	}
}

public class ApiSummaryDescriptionDirectiveTests(ITestOutputHelper output) : DirectiveTest<ApiSummaryBlock>(output,
"""
:::{api-summary}
:product: testapi
:type: description
:::
""")
{
	private static readonly string MinimalOpenApiJson = JsonSerializer.Serialize(new
	{
		openapi = "3.0.0",
		info = new
		{
			title = "Desc Test API",
			version = "1.0.0",
			description = "## Subheading\n\n- **Bold item**\n- Plain item"
		},
		paths = new { }
	});

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("docs/minimal-openapi.json", new MockFileData(MinimalOpenApiJson));
	}

	protected override IReadOnlyDictionary<string, string>? GetApiSpecs() =>
		new Dictionary<string, string> { ["testapi"] = "minimal-openapi.json" };

	[Fact]
	public void ParsesApiSummaryBlock() =>
		Block.Should().NotBeNull().And.Match<ApiSummaryBlock>(b => b.Directive == "api-summary");

	[Fact]
	public void FinalizeAndValidate_ReadsProductAndType()
	{
		Block!.Product.Should().Be("testapi");
		Block.Type.Should().Be("description");
		Block.IsDescriptionKind.Should().BeTrue();
		Block.IsOperationsKind.Should().BeFalse();
	}

	[Fact]
	public void RendersDescription_FromOpenApiSpec()
	{
		Html.Should().Contain("<h2");
		Html.Should().Contain("Subheading");
		Html.Should().Contain("<strong>");
		Html.Should().Contain("Bold item");
		Html.Should().Contain("Plain item");
	}
}

public class ApiSummaryDefaultBehaviorTests(ITestOutputHelper output) : DirectiveTest<ApiSummaryBlock>(output,
"""
:::{api-summary}
:product: testapi
:::
""")
{
	private const string MinimalOpenApiJson =
							 /*lang=json,strict*/
							 """
		{
		  "openapi": "3.0.0",
		  "info": { "title": "Default Test API", "version": "1.0.0" },
		  "paths": {
		    "/test": {
		      "post": {
		        "operationId": "testOperation",
		        "responses": { "200": { "description": "ok" } }
		      }
		    }
		  }
		}
		""";

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("docs/minimal-openapi.json", new MockFileData(MinimalOpenApiJson));
	}

	protected override IReadOnlyDictionary<string, string>? GetApiSpecs() =>
		new Dictionary<string, string> { ["testapi"] = "minimal-openapi.json" };

	[Fact]
	public void DefaultsToOperationsType()
	{
		Block!.Type.Should().BeNull();
		Block.IsOperationsKind.Should().BeTrue();
		Block.IsDescriptionKind.Should().BeFalse();
	}

	[Fact]
	public void RendersOperationsTable_WhenNoTypeSpecified()
	{
		Html.Should().Contain("api-overview");
		Html.Should().Contain("api-method-post");
		Html.Should().Contain("/test");
	}
}

public class ApiSummaryErrorHandlingTests(ITestOutputHelper output) : DirectiveTest<ApiSummaryBlock>(output,
"""
:::{api-summary}
:product: nonexistent
:::
""")
{
	private const string MinimalOpenApiJson =
							 /*lang=json,strict*/
							 """
		{
		  "openapi": "3.0.0",
		  "info": { "title": "Test API", "version": "1.0.0" },
		  "paths": {}
		}
		""";

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("docs/minimal-openapi.json", new MockFileData(MinimalOpenApiJson));
	}

	protected override IReadOnlyDictionary<string, string>? GetApiSpecs() =>
		new Dictionary<string, string> { ["testapi"] = "minimal-openapi.json" };

	[Fact]
	public void RendersErrorComment_WhenProductNotFound()
	{
		Html.Should().Contain("<!-- API configuration for 'nonexistent' not found -->");
	}
}

public class ApiSummaryInvalidTypeTests(ITestOutputHelper output) : DirectiveTest<ApiSummaryBlock>(output,
"""
:::{api-summary}
:product: testapi
:type: invalid
:::
""")
{
	private const string MinimalOpenApiJson =
							 /*lang=json,strict*/
							 """
		{
		  "openapi": "3.0.0",
		  "info": { "title": "Test API", "version": "1.0.0" },
		  "paths": {}
		}
		""";

	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("docs/minimal-openapi.json", new MockFileData(MinimalOpenApiJson));
	}

	protected override IReadOnlyDictionary<string, string>? GetApiSpecs() =>
		new Dictionary<string, string> { ["testapi"] = "minimal-openapi.json" };

	[Fact]
	public void RendersErrorComment_WhenTypeIsInvalid()
	{
		Html.Should().Contain("<!-- api-summary: unknown :type: 'invalid' (use 'operations' or 'description') -->");
	}
}
