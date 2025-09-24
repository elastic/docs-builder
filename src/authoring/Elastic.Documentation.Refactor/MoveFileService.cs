// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Services;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Refactor;

public class MoveFileService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext
) : IService
{
	public async Task<bool> Move(
		IDiagnosticsCollector collector,
		string source,
		string target,
		bool? dryRun,
		string? path,
		IFileSystem fs,
		Cancel ctx
	)
	{
		var context = new BuildContext(collector, fs, fs, configurationContext, ExportOptions.MetadataOnly, path, null);

		var set = new DocumentationSet(context, logFactory, NoopCrossLinkResolver.Instance);

		var moveCommand = new Move(logFactory, fs, fs, set);
		var result = await moveCommand.Execute(source, target, dryRun ?? false, ctx);
		return collector.Errors == 0 && result == 0;
	}
}
