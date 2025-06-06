// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core;
using Actions.Core.Services;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Tooling.Diagnostics.Console;

public class GithubAnnotationOutput(ICoreService? githubActions) : IDiagnosticsOutput
{
	public void Write(Diagnostic diagnostic)
	{
		if (githubActions == null)
			return;
		if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
			return;
		var properties = new AnnotationProperties
		{
			File = diagnostic.File,
			StartColumn = diagnostic.Column,
			StartLine = diagnostic.Line,
			EndColumn = diagnostic.Column + diagnostic.Length ?? 1
		};
		if (diagnostic.Severity == Severity.Error)
			githubActions.WriteError(diagnostic.Message, properties);
		if (diagnostic.Severity == Severity.Warning)
			githubActions.WriteWarning(diagnostic.Message, properties);
		if (diagnostic.Severity == Severity.Hint)
			githubActions.WriteNotice(diagnostic.Message, properties);
	}
}
