// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ExternalCommands;
using Microsoft.Extensions.Logging;

namespace Elastic.Codex.Sourcing;

/// <summary>
/// Git repository operations optimized for shallow clones.
/// </summary>
public class CodexGitRepository(ILoggerFactory logFactory, IDiagnosticsCollector collector, IDirectoryInfo workingDirectory)
	: ExternalCommandExecutor(collector, workingDirectory, Environment.GetEnvironmentVariable("CI") is null or "" ? null : TimeSpan.FromMinutes(10))
{
	/// <inheritdoc />
	protected override ILogger Logger { get; } = logFactory.CreateLogger<CodexGitRepository>();

	private static readonly Dictionary<string, string> EnvironmentVars = new()
	{
		// Disable git editor prompts
		{ "GIT_EDITOR", "true" }
	};

	public string GetCurrentCommit() => Capture("git", "rev-parse", "HEAD");

	public void Init() => ExecIn(EnvironmentVars, "git", "init");

	public bool IsInitialized() => Directory.Exists(Path.Combine(WorkingDirectory.FullName, ".git"));

	public void Fetch(string reference) =>
		ExecIn(EnvironmentVars, "git", "fetch", "--no-tags", "--prune", "--no-recurse-submodules", "--depth", "1", "origin", reference);

	public void EnableSparseCheckout(string[] folders) =>
		ExecIn(EnvironmentVars, "git", ["sparse-checkout", "set", "--no-cone", .. folders]);

	public void Checkout(string reference) =>
		ExecIn(EnvironmentVars, "git", "checkout", "--force", reference);

	public void GitAddOrigin(string origin) =>
		ExecIn(EnvironmentVars, "git", "remote", "add", "origin", origin);
}
