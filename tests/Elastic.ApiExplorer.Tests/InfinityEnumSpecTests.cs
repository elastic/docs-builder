// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class InfinityEnumSpecTests
{
	[Fact]
	public async Task ReadAndSerialize_InfinityStringEnum_DoesNotThrow()
	{
		var yaml = await File.ReadAllTextAsync(
			Path.Combine(AppContext.BaseDirectory, "TestData", "infinity-enum.yaml"),
			TestContext.Current.CancellationToken
		);
		await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(yaml));
		var document = await OpenApiReader.Instance.ReadAsync(stream, "infinity-enum.yaml");
		document.Should().NotBeNull();

		await using var json = new MemoryStream();
		var writeJson =
			async () => await document.SerializeAsJsonAsync(json, OpenApiSpecVersion.OpenApi3_1, TestContext.Current.CancellationToken);
		await writeJson.Should().NotThrowAsync();
		Encoding.UTF8.GetString(json.ToArray()).Should().Contain("\"Infinity\"");

		await using var yamlOut = new MemoryStream();
		var writeYaml =
			async () => await document.SerializeAsYamlAsync(yamlOut, OpenApiSpecVersion.OpenApi3_1, TestContext.Current.CancellationToken);
		await writeYaml.Should().NotThrowAsync();
	}
}
