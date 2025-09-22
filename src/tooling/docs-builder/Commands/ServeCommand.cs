// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using ConsoleAppFramework;
using Documentation.Builder.Http;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands;

internal sealed class ServeCommand(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext
)
{
	private readonly ILogger _logger = logFactory.CreateLogger<ServeCommand>();

	/// <summary>
	///	Continuously serve a documentation folder at http://localhost:3000.
	/// File systems changes will be reflected without having to restart the server.
	/// </summary>
	/// <param name="path">-p, Path to serve the documentation.
	/// Defaults to the`{pwd}/docs` folder
	/// </param>
	/// <param name="port">Port to serve the documentation.</param>
	/// <param name="ctx"></param>
	[Command("")]
	public async Task Serve(string? path = null, int port = 3000, Cancel ctx = default)
	{
		var host = new DocumentationWebHost(logFactory, path, port, new FileSystem(), new MockFileSystem(), configurationContext);
		await host.RunAsync(ctx);
		_logger.LogInformation("Find your documentation at http://localhost:{Port}/{Path}", port,
			host.GeneratorState.Generator.DocumentationSet.FirstInterestingUrl.TrimStart('/')
		);
		await host.StopAsync(ctx);
	}

}
