// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.ApiListing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Reader;
using RazorSlices;

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
	private readonly StaticFileContentHashProvider _contentHashProvider = new(new EmbeddedOrPhysicalFileProvider(context));

	public async Task Generate(Cancel ctx = default)
	{
		var openApiDocument = await OpenApiReader.Create();
		_logger.LogInformation("Generating OpenApiDocument {Title}", openApiDocument.Info.Title);

		foreach (var path in openApiDocument.Paths)
		{
			var fileName = $"{path.Key.Trim('/').Replace('/', '-').Replace("{", "").Replace("}", "")}.html";
			var outputFile = _writeFileSystem.FileInfo.New(Path.Combine(context.DocumentationOutputDirectory.FullName, "api", fileName));
			var apiInformation = new ApiInformation(path.Key, path.Value);

			var viewModel = new IndexViewModel
			{
				ApiInformation = apiInformation,
				StaticFileContentHashProvider = _contentHashProvider
			};
			var slice = ApiListing.Index.Create(viewModel);
			if (!outputFile.Directory!.Exists)
				outputFile.Directory.Create();

			var stream = _writeFileSystem.FileStream.New(outputFile.FullName, FileMode.OpenOrCreate);
			await slice.RenderAsync(stream, cancellationToken: ctx);
		}
	}
}


