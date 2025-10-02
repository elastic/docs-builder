// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;
using Elastic.Documentation.Assembler.Deploying.Redirects;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Serialization;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Deploying;

public class DeployUpdateRedirectsService(ILoggerFactory logFactory, FileSystem fileSystem) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<DeployUpdateRedirectsService>();

	public async Task<bool> UpdateRedirects(IDiagnosticsCollector collector, string environment, string? redirectsFile, Cancel ctx)
	{
		redirectsFile ??= ".artifacts/assembly/redirects.json";
		if (!fileSystem.File.Exists(redirectsFile))
		{
			collector.EmitError(redirectsFile, "Redirects mapping does not exist.");
			return false;
		}

		_logger.LogInformation("Parsing redirects mapping");
		var jsonContent = await fileSystem.File.ReadAllTextAsync(redirectsFile, ctx);
		var sourcedRedirects = JsonSerializer.Deserialize(jsonContent, SourceGenerationContext.Default.DictionaryStringString);

		if (sourcedRedirects is null)
		{
			collector.EmitError(redirectsFile, "Redirects mapping is invalid.");
			return false;
		}

		var kvsName = $"elastic-docs-v3-{environment}-redirects-kvs";
		var cloudFrontClient = new AwsCloudFrontKeyValueStoreProxy(collector, logFactory, fileSystem.DirectoryInfo.New(Directory.GetCurrentDirectory()));

		cloudFrontClient.UpdateRedirects(kvsName, sourcedRedirects);
		return collector.Errors == 0;
	}
}
