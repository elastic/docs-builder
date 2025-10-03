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

public class ConfigurationCloneService(
	ILoggerFactory logFactory,
	AssemblyConfiguration assemblyConfiguration,
	FileSystem fs
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ConfigurationCloneService>();

	public async Task<bool> InitConfigurationToApplicationData(
		IDiagnosticsCollector collector,
		string? gitRef,
		bool saveLocal,
		Cancel ctx
	)
	{
		var checkoutFolder = fs.DirectoryInfo.New(ConfigurationFileProvider.AppDataConfigurationDirectory).Parent;
		if (saveLocal)
			checkoutFolder = fs.DirectoryInfo.New(ConfigurationFileProvider.LocalConfigurationDirectory);
		if (checkoutFolder is null)
		{
			collector.EmitGlobalError($"Unable to find checkout folder {checkoutFolder}");
			return false;
		}

		var cloner = new RepositorySourcer(logFactory, checkoutFolder, fs, collector);
		if (gitRef is not null && gitRef.Length <= 32)
		{
			collector.EmitGlobalError($"gitRef must be at least 32 characters long '{gitRef}'");
			ClearAppDataConfiguration();
			return false;
		}

		// relies on the embedded configuration, but we don't expect this to change
		var repository = assemblyConfiguration.ReferenceRepositories["docs-builder"];
		repository = repository with
		{
			SparsePaths = ["config"]
		};
		var gitReference = gitRef;
		if (string.IsNullOrEmpty(gitReference))
			gitReference = "main";

		_logger.LogInformation("Cloning configuration ({GitReference})", gitReference);
		var checkout = cloner.CloneRef(repository, gitReference, appendRepositoryName: false);
		_logger.LogInformation("Cloned configuration ({GitReference}) to {ConfigurationFolder}", checkout.HeadReference, checkout.Directory.FullName);

		if (gitRef is not null && !checkout.HeadReference.StartsWith(gitRef, StringComparison.OrdinalIgnoreCase))
		{
			collector.EmitError("", $"Checkout of {checkout.HeadReference} does start with requested gitRef {gitRef}.");
			ClearAppDataConfiguration();
			return false;
		}

		var gitRefInformationFile = Path.Combine(checkoutFolder.FullName, "config", "git-ref.txt");
		await fs.File.WriteAllTextAsync(gitRefInformationFile, checkout.HeadReference, ctx);

		return collector.Errors == 0;

		void ClearAppDataConfiguration()
		{
			// if we intended to save to a system location, ensure we delete the config folder since it's not in the state we want
			if (saveLocal)
				return;
			_logger.LogInformation("Deleting cached config folder");
			fs.Directory.Delete(ConfigurationFileProvider.AppDataConfigurationDirectory, true);
		}
	}
}
