// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Navigation;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Navigation;

public class GlobalNavigationService(
	ILoggerFactory logFactory,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	IFileSystem fileSystem
) : IService
{
	public async Task<bool> Validate(IDiagnosticsCollector collector, Cancel ctx)
	{
		var assembleContext = new AssembleContext(configuration, configurationContext, "dev", collector, fileSystem, fileSystem, null, null);
		var namespaceChecker = new NavigationPrefixChecker(logFactory, assembleContext);

		// this validates all path prefixes are unique, early exit if duplicates are detected
		if (!GlobalNavigationFile.ValidatePathPrefixes(collector, assembleContext.ConfigurationFileProvider, configuration) || collector.Errors > 0)
			return false;

		await namespaceChecker.CheckAllPublishedLinks(assembleContext.Collector, ctx);
		return collector.Errors == 0;
	}

	public async Task<bool> ValidateLocalLinkReference(IDiagnosticsCollector collector, string? file, Cancel ctx)
	{
		file ??= ".artifacts/docs/html/links.json";
		var assembleContext = new AssembleContext(configuration, configurationContext, "dev", collector, fileSystem, fileSystem, null, null);

		var root = fileSystem.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		var repository = GitCheckoutInformation.Create(root, fileSystem, logFactory.CreateLogger(nameof(GitCheckoutInformation))).RepositoryName
						 ?? throw new Exception("Unable to determine repository name");

		var namespaceChecker = new NavigationPrefixChecker(logFactory, assembleContext);

		await namespaceChecker.CheckWithLocalLinksJson(assembleContext.Collector, repository, file, ctx);
		return collector.Errors == 0;
	}
}
