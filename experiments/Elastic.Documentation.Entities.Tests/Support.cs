// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Publishing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Entities.Tests;

public static class Support
{
	public static string SolutionDirectory => Paths.GetSolutionDirectory()!.FullName;

	/// <summary>A provider reading the files in <paramref name="sourceDirectory"/> through a mock file system seeded as the Remote config source (the same way the golden tests do).</summary>
	public static ConfigurationFileProvider ProviderFor(string sourceDirectory)
	{
		var fs = new MockFileSystem();
		var configDirectory = ConfigurationFileProvider.AppDataConfigurationDirectory;
		foreach (var file in Directory.EnumerateFiles(sourceDirectory))
			fs.AddFile(Path.Join(configDirectory, Path.GetFileName(file)), new MockFileData(File.ReadAllText(file)));
		fs.AddFile(Path.Join(configDirectory, "git-ref.txt"), new MockFileData("entities"));
		return new ConfigurationFileProvider(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(fs),
			skipPrivateRepositories: false,
			ConfigurationSource.Remote
		);
	}

	public static ConfigurationFileProvider ShippedProvider => ProviderFor(Path.Join(SolutionDirectory, "config"));

	public static AssemblyConfiguration ShippedAssembly => AssemblyLoader.Create(ShippedProvider);

	/// <summary>
	/// The derivation the loader performs: available are the references that publish, private are the available private ones. Written once and handed to the generator as the
	/// normalizer, so generated data satisfies the invariants only the loader knows (and mixed and adversarial data still breaks them).
	/// </summary>
	public static AssemblyConfiguration Derive(AssemblyConfiguration assembly)
	{
		var references = assembly
			.ReferenceRepositories
			.Values
			.Select(r => r with { Skip = false })
			.GroupBy(r => r.Name)
			.Select(g => g.First())
			.ToList();
		var available = references.ToFrozenDictionary(r => r.Name);
		return assembly with
		{
			ReferenceRepositories = available,
			AvailableRepositories = available,
			PrivateRepositories = references.Where(r => r.Private).ToFrozenDictionary(r => r.Name),
			AllPrivateRepositoryNames = references.Where(r => r.Private).Select(r => r.Name).ToHashSet(),
		};
	}
}
