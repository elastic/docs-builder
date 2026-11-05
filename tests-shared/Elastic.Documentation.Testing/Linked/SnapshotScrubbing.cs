// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Nullean.Snapshots;

namespace Elastic.Documentation.Testing.Linked;

/// <summary>
/// Scrubbers every snapshot in docs-builder's tests gets: machine-specific paths and git SHAs never belong in a
/// verified file. Runs when any test project referencing this assembly loads it.
/// </summary>
internal static class SnapshotScrubbing
{
#pragma warning disable CA2255 // compiled into each test project, which is application code
	[ModuleInitializer]
#pragma warning restore CA2255
	internal static void Register()
	{
		SnapshotDefaults.AddScrubber(
			Scrubbers.Regex(
				new Regex(Regex.Escape(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)) + @"[^\s""'<>]*", RegexOptions.Compiled),
				"{temp}"
			)
		);
		if (FindSolutionRoot() is { } root)
			SnapshotDefaults.AddScrubber(Scrubbers.Regex(new Regex(Regex.Escape(root), RegexOptions.Compiled), "{repo}"));
	}

	private static string? FindSolutionRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docs-builder.slnx")))
			dir = dir.Parent;
		return dir?.FullName.TrimEnd(Path.DirectorySeparatorChar);
	}
}
