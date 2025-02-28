// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Actions.Core.Services;
using Amazon.S3;
using Amazon.S3.Model;
using ConsoleAppFramework;
using Documentation.Assembler.Links;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Discovery;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class InboundLinkCommands(ILoggerFactory logger, ICoreService githubActionsService)
{
	private void AssignOutputLogger()
	{
		var log = logger.CreateLogger<Program>();
#pragma warning disable CA2254
		ConsoleApp.Log = msg => log.LogInformation(msg);
		ConsoleApp.LogError = msg => log.LogError(msg);
#pragma warning restore CA2254
	}

	/// <summary> Validate all published cross_links in all published links.json files. </summary>
	/// <param name="ctx"></param>
	[Command("validate-all")]
	public async Task<int> ValidateAllInboundLinks(Cancel ctx = default)
	{
		AssignOutputLogger();
		return await new LinkIndexLinkChecker(logger).CheckAll(githubActionsService, ctx);
	}

	/// <summary> Validate all published cross_links in all published links.json files. </summary>
	/// <param name="repository"></param>
	/// <param name="ctx"></param>
	[Command("validate")]
	public async Task<int> ValidateRepoInboundLinks(string? repository = null, Cancel ctx = default)
	{
		AssignOutputLogger();
		var fs = new FileSystem();
		var root = fs.DirectoryInfo.New(Paths.Root.FullName);
		repository ??= GitCheckoutInformation.Create(root, new FileSystem()).RepositoryName;
		if (repository == null)
			throw new Exception("Unable to determine repository name");
		return await new LinkIndexLinkChecker(logger).CheckRepository(githubActionsService, repository, ctx);
	}

	/// <summary>
	/// Validate a locally published links.json file against all published links.json files in the registry
	/// </summary>
	/// <param name="file"></param>
	/// <param name="ctx"></param>
	[Command("validate-link-reference")]
	public async Task<int> ValidateLocalLinkReference(string? file = null, Cancel ctx = default)
	{
		AssignOutputLogger();
		file ??= ".artifacts/docs/html/links.json";
		var fs = new FileSystem();
		var root = fs.DirectoryInfo.New(Paths.Root.FullName);
		var repository = GitCheckoutInformation.Create(root, new FileSystem()).RepositoryName
						?? throw new Exception("Unable to determine repository name");

		return await new LinkIndexLinkChecker(logger).CheckWithLocalLinksJson(githubActionsService, repository, file, ctx);
	}
}
