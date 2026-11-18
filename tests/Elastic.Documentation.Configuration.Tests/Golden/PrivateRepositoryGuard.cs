// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Testing.Linked;
using Nullean.Snapshots;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>
/// Fails every snapshot of this project, and its verified file, that names a private repository of the shipped
/// <c>config/assembler.yml</c>. The names come from that file's <c>private: true</c> entries at test time and are never printed.
/// </summary>
internal static class PrivateRepositoryGuard
{
	internal static string[] Forbidden { get; } = Load();

#pragma warning disable CA2255 // compiled into a test project, which is application code
	[ModuleInitializer]
#pragma warning restore CA2255
	internal static void Register() => SnapshotDefaults.Forbid(Forbidden);

	private static string[] Load()
	{
		var root = Paths.GetSolutionDirectory();
		var path = root is null ? null : Path.Join(root.FullName, "config", "assembler.yml");
		return path is null || !File.Exists(path)
			? []
			: PrivateRepositoryScrubbing.Forbidden(AssemblyLoader.Deserialize(File.ReadAllText(path)).AllPrivateRepositoryNames);
	}
}
