// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Components.PropertyTree._Partials;
using RazorSlices;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// A request lists no <c>readOnly</c> fields and a response no <c>writeOnly</c> ones; a type page lists both. Nested rows
/// sit in a container whose id no field anchor can take.
/// </summary>
public class FieldDirectionTests
{
	private const string Spec =
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "t", "version": "1" },
		  "paths": {},
		  "components": {
		    "schemas": {
		      "Account": {
		        "type": "object",
		        "required": ["id", "name"],
		        "properties": {
		          "id": { "type": "string", "readOnly": true },
		          "name": { "type": "string" },
		          "password": { "type": "string", "writeOnly": true },
		          "audit": {
		            "type": "object",
		            "properties": { "created_at": { "type": "string", "readOnly": true } }
		          }
		        }
		      },
		      "Tree": {
		        "type": "object",
		        "properties": {
		          "node": {
		            "type": "object",
		            "properties": {
		              "children": { "type": "array", "items": { "type": "string" } },
		              "name": { "type": "string" }
		            }
		          }
		        }
		      }
		    }
		  }
		}
		""";

	[Test]
	public async Task BuildPropertyList_Request_LeavesOutReadOnlyFields()
	{
		var rows = await RowsAsync("Account", new PropertyTreeScope { Prefix = "req", IsRequest = true });

		rows.Select(r => r.Name).Should().Equal("name", "password", "audit");
		rows
			.Single(r => r.Name == "audit")
			.Children
			.Kind
			.Should()
			.Be(ChildKind.None, "an object whose fields are all read-only has nothing to send");
	}

	[Test]
	public async Task BuildPropertyList_Response_LeavesOutWriteOnlyFields()
	{
		var rows = await RowsAsync("Account", new PropertyTreeScope { Prefix = "res-200", IsResponse = true });

		rows.Select(r => r.Name).Should().Equal("id", "name", "audit");
		rows.Single(r => r.Name == "audit").Children.Properties!.Items.Select(r => r.Name).Should().Equal("created_at");
	}

	[Test]
	public async Task BuildPropertyList_TypePage_ListsEveryField()
	{
		var rows = await RowsAsync("Account", new PropertyTreeScope { Prefix = "" });

		rows.Select(r => r.Name).Should().Equal("id", "name", "password", "audit");
	}

	[Test]
	public async Task Render_FieldNamedChildren_KeepsEveryIdUnique()
	{
		var document = await LoadSpecAsync(Spec);
		var list =
			BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Tree"], new PropertyTreeScope { Prefix = "res-200" })!;

		var html = await _PropertyList.Create(list).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("id=\"res-200-node-children\"", "the field keeps its anchor");
		html.Should().Contain("id=\"res-200-node--children\"", "the row's nested rows take the double-dash id");
		var ids = Regex.Matches(html, "\\bid=\"([^\"]*)\"").Select(m => m.Groups[1].Value).ToArray();
		ids.Should().OnlyHaveUniqueItems();
	}

	private static async Task<IReadOnlyList<ApiProperty>> RowsAsync(string schemaName, PropertyTreeScope scope)
	{
		var document = await LoadSpecAsync(Spec);
		return BuilderFor(document).BuildPropertyList(document.Components!.Schemas![schemaName], scope)!.Items;
	}
}
