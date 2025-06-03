// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Tooling.ExternalCommands;

namespace Documentation.Builder.Tracking;

public class GitRepositoryTracker(DiagnosticsCollector collector, IDirectoryInfo workingDirectory) : ExternalCommandExecutor(collector, workingDirectory), IRepositoryTracker
{
	public IEnumerable<string> GetChangedFiles()
	{
		var output = CaptureMultiple("git", "diff", "--name-status", "main...HEAD", "--", "./docs");
		if (output.Length == 0)
			return [];

		var movedOrDeleted = output
			.Where(line => line.StartsWith('R') || line.StartsWith('D'))
			.Select(line => line.Split('\t')[1]);
		return movedOrDeleted;
	}
}
