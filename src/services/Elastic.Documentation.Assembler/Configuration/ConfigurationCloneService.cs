// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Configuration;

public class ConfigurationCloneService(ILoggerFactory logFactory, AssemblyConfiguration assemblyConfiguration, FileSystem fs) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ConfigurationCloneService>();

	public async Task<bool> InitConfigurationToApplicationData(IDiagnosticsCollector collector, string? gitRef, Cancel ctx)
	{
		var cachedPath = Path.Combine(Paths.ApplicationData.FullName, "config-clone");
		var checkoutFolder = fs.DirectoryInfo.New(cachedPath);
		var cloner = new RepositorySourcer(logFactory, checkoutFolder, fs, collector);

		// relies on the embedded configuration, but we don't expect this to change
		var repository = assemblyConfiguration.ReferenceRepositories["docs-builder"];
		repository = repository with
		{
			SparsePaths = ["config"]
		};
		if (string.IsNullOrEmpty(gitRef))
			gitRef = "main";

		_logger.LogInformation("Cloning configuration ({GitReference})", gitRef);
		var checkout = cloner.CloneRef(repository, gitRef, appendRepositoryName: false);
		_logger.LogInformation("Cloned configuration ({GitReference}) to {ConfigurationFolder}", checkout.HeadReference, checkout.Directory.FullName);

		var gitRefInformationFile = Path.Combine(cachedPath, "config", "git-ref.txt");
		await fs.File.WriteAllTextAsync(gitRefInformationFile, checkout.HeadReference, ctx);
		return collector.Errors == 0;
	}
}
