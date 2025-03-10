// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Assembler.Sourcing;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class RepositoryCommands(ICoreService githubActionsService, ILoggerFactory logger)
{
	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	private void AssignOutputLogger()
	{
		var log = logger.CreateLogger<Program>();
		ConsoleApp.Log = msg => log.LogInformation(msg);
		ConsoleApp.LogError = msg => log.LogError(msg);
	}

	// would love to use libgit2 so there is no git dependency but
	// libgit2 is magnitudes slower to clone repositories https://github.com/libgit2/libgit2/issues/4674
	/// <summary> Clones all repositories </summary>
	/// <param name="ctx"></param>
	[Command("clone-all")]
	public async Task CloneAll(Cancel ctx = default)
	{
		AssignOutputLogger();

		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);

		var assembleContext = new AssembleContext(collector, new FileSystem(), new FileSystem(), null);
		var cloner = new RepositoryCloner(logger, assembleContext);
		await cloner.CloneAll(ctx);
	}


}
