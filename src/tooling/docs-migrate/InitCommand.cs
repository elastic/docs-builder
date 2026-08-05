// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.LegacyDocs.Migration;
using ProcNet;

namespace Documentation.Migrate;

internal sealed class InitCommand
{
	/// <summary>Clone the legacy docs repo and extract conf.yaml.</summary>
	/// <param name="workDir">Working directory for migration artifacts</param>
	/// <param name="force">Re-clone even if conf.yaml already exists</param>
	/// <param name="ct">Cancellation token</param>
	public async Task<int> Init(string? workDir = null, bool force = false, CancellationToken ct = default)
	{
		var dir = SharedOptions.ResolveWorkDir(workDir);
		var confPath = Path.Combine(dir, "conf.yaml");

		if (File.Exists(confPath) && !force)
		{
			Console.WriteLine($"conf.yaml already exists at {confPath} (use --force to overwrite)");
			return 0;
		}

		_ = Directory.CreateDirectory(dir);

		var docsRepoDir = Path.Combine(dir, "docs-repo");
		if (Directory.Exists(docsRepoDir))
			Directory.Delete(docsRepoDir, recursive: true);

		Console.WriteLine("Cloning elastic/docs (shallow)...");
		var arguments = new ExecArguments("git", ["clone", "--depth", "1", "https://github.com/elastic/docs.git", docsRepoDir]);
		var exitCode = await Proc.ExecAsync(arguments, ct);
		if (exitCode != 0)
		{
			Console.Error.WriteLine($"git clone failed with exit code {exitCode}");
			return 1;
		}

		var sourceConf = Path.Combine(docsRepoDir, "conf.yaml");
		if (!File.Exists(sourceConf))
		{
			Console.Error.WriteLine($"conf.yaml not found in cloned docs repo at {sourceConf}");
			return 1;
		}

		File.Copy(sourceConf, confPath, overwrite: true);
		Console.WriteLine($"Copied conf.yaml to {confPath}");

		var yaml = await File.ReadAllTextAsync(confPath, ct);
		var conf = LegacyConfParser.Parse(yaml);
		var bookCount = conf.Contents.SelectMany(c => c.Sections).Count();
		Console.WriteLine($"Parsed conf.yaml: {bookCount} books across {conf.Contents.Count} categories");

		return 0;
	}
}
