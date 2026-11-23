// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using ClrAssembly = System.Reflection.Assembly;

namespace Elastic.Documentation.Build.Tests;

public class RegexTimeoutsTests
{
	[Test]
	public void GeneratedRegexes_AllHaveAMatchTimeout()
	{
		var regexes = LoadProductAssemblies().SelectMany(GeneratedRegexes).ToList();

		regexes.Should().NotBeEmpty();
		var unbounded = regexes.Where(r => r.Regex.MatchTimeout == Regex.InfiniteMatchTimeout).Select(r => r.Name).Order().ToList();
		unbounded.Should().BeEmpty("every [GeneratedRegex] needs a match timeout, e.g. RegexTimeouts.MatchTimeoutMilliseconds");
	}

	[Test]
	public void GeneratedRegexes_ScanCoversTheShippedLibraries()
	{
		var scanned = LoadProductAssemblies().Where(a => GeneratedRegexes(a).Any()).Select(a => a.GetName().Name).ToList();

		scanned.Should().Contain([
			"Elastic.Markdown",
			"Elastic.ApiExplorer",
			"Elastic.Documentation",
			"Elastic.Changelog",
			"Elastic.Documentation.Search.Contract"
		]);
	}

	private static readonly string[] OwnPrefixes =
	[
		"Elastic.Documentation",
		"Elastic.Markdown",
		"Elastic.ApiExplorer",
		"Elastic.Changelog",
		"Elastic.Codex",
		"Elastic.LegacyDocs"
	];

	private static List<ClrAssembly> LoadProductAssemblies()
	{
		var assemblies = new List<ClrAssembly>();
		foreach (var path in Directory.EnumerateFiles(AppContext.BaseDirectory, "Elastic.*.dll"))
		{
			var name = AssemblyName.GetAssemblyName(path);
			if (
				name.Name!.EndsWith(".Tests", StringComparison.Ordinal)
				|| !OwnPrefixes.Any(prefix => name.Name.StartsWith(prefix, StringComparison.Ordinal))
			)
				continue;
			assemblies.Add(ClrAssembly.Load(name));
		}
		return assemblies;
	}

	private static IEnumerable<(string Name, Regex Regex)> GeneratedRegexes(ClrAssembly assembly)
	{
		const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
		foreach (var type in SafeTypes(assembly).Where(t => !t.ContainsGenericParameters))
		{
			foreach (var method in type.GetMethods(all).Where(
				m => m.GetCustomAttribute<GeneratedRegexAttribute>() is not null && m.GetParameters().Length == 0
			))
				yield return ($"{type.FullName}.{method.Name}", (Regex)method.Invoke(null, null)!);
			foreach (var property in type.GetProperties(all).Where(p => p.GetCustomAttribute<GeneratedRegexAttribute>() is not null))
				yield return ($"{type.FullName}.{property.Name}", (Regex)property.GetValue(null)!);
		}
	}

	private static Type[] SafeTypes(ClrAssembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException e)
		{
			return e.Types.Where(t => t is not null).ToArray()!;
		}
	}
}
