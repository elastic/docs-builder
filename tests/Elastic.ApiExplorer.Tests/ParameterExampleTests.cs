// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Components.PropertyTree._Partials;
using RazorSlices;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>A parameter row shows an example only when it says something the type does not.</summary>
public class ParameterExampleTests
{
	private const string Spec =
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "t", "version": "1" },
		  "paths": {
		    "/items": {
		      "get": {
		        "parameters": [
		          { "name": "case_id", "in": "query", "example": "9c235210-6834-11ea", "schema": { "type": "string" } },
		          { "name": "from_schema", "in": "query", "schema": { "type": "string", "example": "simple_list" } },
		          { "name": "own_wins", "in": "query", "example": "from-parameter", "schema": { "type": "string", "example": "from-schema" } },
		          { "name": "named", "in": "query", "examples": { "first": { "value": "from-examples" } }, "schema": { "type": "string" } },
		          { "name": "statuses", "in": "query", "example": ["open", "acknowledged"], "schema": { "type": "array", "items": { "type": "string" } } },
		          { "name": "filter", "in": "query", "example": { "a": 1 }, "schema": { "type": "object" } },
		          { "name": "long_filter", "in": "query", "example": { "message": "this value is far too long to fit on a line of its own" }, "schema": { "type": "object" } },
		          { "name": "page", "in": "query", "example": 1, "schema": { "type": "integer" } },
		          { "name": "since", "in": "query", "example": 1700000000, "schema": { "type": "integer", "format": "int64" } },
		          { "name": "debug", "in": "query", "example": true, "schema": { "type": "boolean" } },
		          { "name": "kbn-xsrf", "in": "header", "example": "true", "schema": { "type": "string" } },
		          { "name": "order", "in": "query", "example": "desc", "schema": { "type": "string", "enum": ["asc", "desc"] } },
		          { "name": "mode", "in": "query", "example": "auto", "schema": { "type": "string", "default": "auto" } },
		          { "name": "empty", "in": "query", "example": "", "schema": { "type": "string" } },
		          { "name": "none", "in": "query", "schema": { "type": "string" } }
		        ],
		        "responses": { "200": { "description": "ok" } }
		      }
		    }
		  }
		}
		""";

	// OpenAPI 3.1 allows a list of types, so the value of an example, not the schema's type, says what it is.
	private const string Spec31 =
		"""
		{
		  "openapi": "3.1.0",
		  "info": { "title": "t", "version": "1" },
		  "paths": {
		    "/items": {
		      "get": {
		        "parameters": [
		          { "name": "string_or_bool", "in": "query", "example": "9c235210-6834-11ea", "schema": { "type": ["string", "boolean"] } },
		          { "name": "string_or_int", "in": "query", "example": "latest", "schema": { "type": ["string", "integer"] } },
		          { "name": "bool_value", "in": "query", "example": true, "schema": { "type": ["string", "boolean"] } },
		          { "name": "int_value", "in": "query", "example": 5, "schema": { "type": ["string", "integer"] } },
		          { "name": "stamps", "in": "query", "example": [1700000000], "schema": { "type": "array", "items": { "type": "integer", "format": "int64" } } },
		          { "name": "numbers", "in": "query", "example": [1, 2], "schema": { "type": "array", "items": { "type": "integer" } } },
		          { "name": "quoted", "in": "query", "example": "a`b", "schema": { "type": "string" } }
		        ],
		        "responses": { "200": { "description": "ok" } }
		      }
		    }
		  }
		}
		""";

	[Test]
	[Arguments("string_or_bool", "9c235210-6834-11ea")]
	[Arguments("string_or_int", "latest")]
	[Arguments("stamps", "[1700000000]")]
	[Arguments("quoted", "a`b")]
	public async Task BuildParameterList_ValueDecidesNotSchemaType_ExampleIsShown(string name, string expected)
	{
		var rows = (await ListAsync(Spec31)).Items;

		rows.Single(r => r.Name == name).Example.Should().Be(expected);
	}

	[Test]
	[Arguments("bool_value", "a boolean value is a guess even when the schema also allows a string")]
	[Arguments("int_value", "a bare number is a guess even when the schema also allows a string")]
	[Arguments("numbers", "an array of bare numbers has no format on its items")]
	public async Task BuildParameterList_ValueIsSelfEvident_ExampleIsLeftOut(string name, string reason)
	{
		var rows = (await ListAsync(Spec31)).Items;

		rows.Single(r => r.Name == name).Example.Should().BeNull(reason);
	}

	[Test]
	public async Task Write_ExampleWithBacktick_KeepsItInsideTheCodeSpan()
	{
		var markdown = new StringBuilder();

		ApiPropertyMarkdown.WriteList(markdown, await ListAsync(Spec31), "/api/doc/fixture");

		markdown.ToString().Should().Contain("Example: ``a`b``");
	}

	[Test]
	[Arguments("case_id", "9c235210-6834-11ea")]
	[Arguments("from_schema", "simple_list")]
	[Arguments("own_wins", "from-parameter")]
	[Arguments("named", "from-examples")]
	[Arguments("statuses", """["open","acknowledged"]""")]
	[Arguments("filter", """{"a":1}""")]
	[Arguments("since", "1700000000")]
	public async Task BuildParameterList_MeaningfulExample_IsShown(string name, string expected)
	{
		var rows = await RowsAsync();

		rows.Single(r => r.Name == name).Example.Should().Be(expected);
	}

	[Test]
	[Arguments("long_filter", "a value too long for a line is left to the examples panel")]
	[Arguments("page", "a bare number is a guess the reader could make")]
	[Arguments("debug", "a boolean needs no example")]
	[Arguments("kbn-xsrf", "a boolean spelled as text needs none either")]
	[Arguments("order", "an enum member is already in the Values row")]
	[Arguments("mode", "the default is already shown")]
	[Arguments("empty", "an empty string says nothing")]
	[Arguments("none", "there is nothing to show")]
	public async Task BuildParameterList_ExampleThatSaysNothingNew_IsLeftOut(string name, string reason)
	{
		var rows = await RowsAsync();

		rows.Single(r => r.Name == name).Example.Should().BeNull(reason);
	}

	[Test]
	public async Task Render_ExampleRow_FollowsTheDescriptionAndValues()
	{
		var list = await ListAsync();

		var html = await _PropertyList.Create(list).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchRegex(
			"""id="query-case_id"[\s\S]*?<dd class="example-value">\s*<span class="values-label">Example:</span>\s*<code>9c235210-6834-11ea</code>"""
		);
		html.Should().NotContain("<code>true</code>");
	}

	[Test]
	public async Task Write_ExampleRow_ReadsAsAnExampleLine()
	{
		var markdown = new StringBuilder();

		ApiPropertyMarkdown.WriteList(markdown, await ListAsync(), "/api/doc/fixture");

		// AppendLine writes \r\n on Windows.
		markdown.ToString().ReplaceLineEndings("\n").Should().Contain("- `case_id` (string) — optional\n  Example: `9c235210-6834-11ea`");
		markdown.ToString().Should().Contain("Example: `[\"open\",\"acknowledged\"]`");
	}

	private static async Task<IReadOnlyList<ApiProperty>> RowsAsync() => (await ListAsync()).Items;

	private static async Task<ApiPropertyList> ListAsync(string spec = Spec)
	{
		var document = await LoadSpecAsync(spec);
		var parameters = document.Paths!["/items"].Operations![HttpMethod.Get].Parameters!.Select(
			p => new ParameterRowInput(p, p.Description)
		).ToArray();
		return BuilderFor(document).BuildParameterList(parameters, new PropertyTreeScope { Prefix = "query", IsRequest = true });
	}
}
