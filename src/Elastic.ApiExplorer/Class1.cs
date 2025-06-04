// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer;

public static class OpenApiReader
{
	public static async Task<OpenApiDocument> Create()
	{
		var settings = new OpenApiReaderSettings
		{
			LeaveStreamOpen = false
		};
		await using var fs = File.Open("/Users/mpdreamz/Projects/docs-builder/src/Elastic.ApiExplorer/elasticsearch-openapi.json", FileMode.Open);
		var openApiDocument = await OpenApiDocument.LoadAsync(fs, settings: settings);
		return openApiDocument.Document;
	}
}

public class OpenApiGenerator(BuildContext context, ILoggerFactory logger)
{
	private readonly ILogger _logger = logger.CreateLogger<OpenApiGenerator>();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;

	public async Task Generate()
	{
		var openApiDocument = await OpenApiReader.Create();
		_logger.LogInformation("Generating OpenApiDocument {Title}", openApiDocument.Info.Title);

		foreach (var path in openApiDocument.Paths)
		{
			var fileName = path.Key;
			var outputFile = _writeFileSystem.FileInfo.New(Path.Combine(context.DocumentationOutputDirectory.FullName, fileName));
		}


	}
}
