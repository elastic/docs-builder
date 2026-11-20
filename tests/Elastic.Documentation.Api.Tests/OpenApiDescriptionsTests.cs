// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Api.Tests.Fixtures;

namespace Elastic.Documentation.Api.Tests;

/// <summary>Everything a reader of the OpenAPI document sees has a description, so a new field or endpoint can't ship without one.</summary>
public class OpenApiDescriptionsTests
{
	private static readonly Lazy<ApiWebApplicationFactory> SharedFactory = new(() => new ApiWebApplicationFactory());

	[After(Class)]
	public static void DisposeSharedFactory()
	{
		if (SharedFactory.IsValueCreated)
			SharedFactory.Value.Dispose();
	}

	// What ASP.NET writes when nothing describes a response.
	private static readonly string[] StatusPhrases =
	[
		"OK",
		"No Content",
		"Bad Request",
		"Not Found",
		"Internal Server Error",
		"Service Unavailable"
	];

	private static async Task<JsonElement> Document()
	{
		using var client = SharedFactory.Value.CreateClient();
		using var response = await client.GetAsync("/docs/_api/v1/openapi.json", TestContext.Current!.Execution.CancellationToken);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		return JsonElement.Parse(await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken));
	}

	private static bool Described(JsonElement element) =>
		element.TryGetProperty("description", out var description) && !string.IsNullOrWhiteSpace(description.GetString());

	// A nullable reference is written as oneOf [null, {$ref, description}].
	private static bool DescribedProperty(JsonElement property) =>
		Described(property) || (property.TryGetProperty("oneOf", out var branches) && branches.EnumerateArray().Any(Described));

	[Test]
	public async Task EveryOperationParameterResponseAndTag_IsDescribed()
	{
		var document = await Document();
		var missing = new List<string>();
		foreach (var path in document.GetProperty("paths").EnumerateObject())
		{
			foreach (var operation in path.Value.EnumerateObject().Where(o => o.Name is "get" or "post" or "put" or "delete"))
			{
				var name = $"{operation.Name.ToUpperInvariant()} {path.Name}";
				if (!Described(operation.Value))
					missing.Add(name);
				foreach (var parameter in operation.Value.TryGetProperty("parameters", out var parameters)
					? parameters.EnumerateArray()
					: [])
				{
					if (!Described(parameter))
						missing.Add($"{name} parameter {parameter.GetProperty("name").GetString()}");
				}
				foreach (var response in operation.Value.GetProperty("responses").EnumerateObject())
				{
					if (StatusPhrases.Contains(response.Value.GetProperty("description").GetString()))
						missing.Add($"{name} response {response.Name}");
				}
			}
		}
		missing.AddRange(
			document.GetProperty("tags").EnumerateArray().Where(t => !Described(t)).Select(t => $"tag {t.GetProperty("name").GetString()}")
		);

		missing.Should().BeEmpty();
	}

	private static bool HasExample(JsonElement element) =>
		element.TryGetProperty("example", out _) || (element.TryGetProperty("examples", out var examples) && examples.GetArrayLength() > 0);

	// A property that points to another schema gets its example from that schema's own fields.
	private static bool IsReference(JsonElement property) =>
		property.TryGetProperty("$ref", out _)
			|| (property.TryGetProperty("oneOf", out var branches) && branches.EnumerateArray().Any(b => b.TryGetProperty("$ref", out _)))
			|| (property.TryGetProperty("items", out var items) && items.TryGetProperty("$ref", out _))
			|| (property.TryGetProperty("additionalProperties", out var values)
				&& values.ValueKind == JsonValueKind.Object
				&& values.TryGetProperty("$ref", out _));

	[Test]
	public async Task EveryFieldAndParameter_HasAnExample()
	{
		var document = await Document();
		var missing = new List<string>();
		foreach (var schema in document.GetProperty("components").GetProperty("schemas").EnumerateObject())
		{
			foreach (var property in schema.Value.TryGetProperty("properties", out var properties) ? properties.EnumerateObject() : [])
			{
				if (!IsReference(property.Value) && !HasExample(property.Value))
					missing.Add($"{schema.Name}.{property.Name}");
			}
		}
		foreach (var path in document.GetProperty("paths").EnumerateObject())
		{
			foreach (var operation in path.Value.EnumerateObject().Where(o => o.Name is "get" or "post" or "put" or "delete"))
			{
				foreach (var parameter in operation.Value.TryGetProperty("parameters", out var parameters)
					? parameters.EnumerateArray()
					: [])
				{
					if (!HasExample(parameter))
						missing.Add(
							$"{operation.Name.ToUpperInvariant()} {path.Name} parameter {parameter.GetProperty("name").GetString()}"
						);
				}
			}
		}

		missing.Should().BeEmpty();
	}

	[Test]
	public async Task EverySchemaAndProperty_IsDescribed()
	{
		var document = await Document();
		var missing = new List<string>();
		foreach (var schema in document.GetProperty("components").GetProperty("schemas").EnumerateObject())
		{
			if (!Described(schema.Value))
				missing.Add(schema.Name);
			foreach (var property in schema.Value.TryGetProperty("properties", out var properties) ? properties.EnumerateObject() : [])
			{
				if (!DescribedProperty(property.Value))
					missing.Add($"{schema.Name}.{property.Name}");
			}
		}

		missing.Should().BeEmpty();
	}
}
