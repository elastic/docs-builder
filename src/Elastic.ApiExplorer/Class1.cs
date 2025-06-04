// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer;

public class OpenApiReader
{
	public static async Task<OpenApiDocument> Create()
	{
		var settings = new OpenApiReaderSettings { LeaveStreamOpen = false };
		await using var fs = File.Open("/Users/mpdreamz/Projects/docs-builder/src/Elastic.ApiExplorer/elasticsearch-openapi.json", FileMode.Open);
		var openApiDocument = await OpenApiDocument.LoadAsync(fs, settings: settings);
		return openApiDocument.Document;
	}
}
