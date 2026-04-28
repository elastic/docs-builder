// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Documentation.Builder.Http;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands;

internal sealed class ServeCommand(ILoggerFactory logFactory, IConfigurationContext configurationContext)
{
	private readonly ILogger _logger = logFactory.CreateLogger<ServeCommand>();

	/// <summary>Serve a documentation folder at <c>http://localhost:3000</c> with live reload.</summary>
	/// <remarks>File-system changes are reflected without restarting the server.</remarks>
	/// <param name="path">-p, Path to serve. Defaults to the <c>cwd/docs</c> folder</param>
	/// <param name="port">Port to serve the documentation. Default: 3000</param>
	/// <param name="watch">Special flag for <c>dotnet watch</c> optimizations during development</param>
	[CommandName("serve")]
	public async Task Serve(GlobalCliOptions _, string? path = null, int port = 3000, bool watch = false, CancellationToken ct = default)
	{
		var host = new DocumentationWebHost(logFactory, path, port, FileSystemFactory.RealGitRootForPath(path), FileSystemFactory.InMemory(), configurationContext, watch);
		await host.RunAsync(ct);
		_logger.LogInformation("Find your documentation at http://localhost:{Port}/{Path}", port,
			host.GeneratorState.Generator.DocumentationSet.FirstInterestingUrl.TrimStart('/')
		);
		await host.StopAsync(ct);
	}
}
