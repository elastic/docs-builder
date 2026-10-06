// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using YamlDotNet.Core;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// Regression cases for texts that once made a loader spin forever or throw a NullReferenceException (found by mutating the shipped files; the loader
/// property laws first saw them as a test process at 100% CPU and a NullReferenceException out of <c>SiteNavigationLoader.Load</c>, which looked like a race
/// but was the text: the same text fails alone, on one thread).
/// </summary>
public class LoaderTerminationTests
{
	public static IEnumerable<(string Entry, string Text)> Cases()
	{
		// a toc entry that is not a mapping: a loop that does not consume it never ends
		yield return ("SiteNavigationLoader.Load", "toc:\n  - toc: a://b\n    path_prefix: x\n  - plain\n  - [1, 2]\n");
		yield return ("SiteNavigationLoader.Load", "toc:\n  - toc: a://b\n    path_prefix: x\n    children:\n      - plain\n      - [1, 2]\n      - toc: a://c\n");
		yield return ("SiteNavigationLoader.Load", "toc:\n  - section: S\n    dropdown:\n      - plain\n      - [1, 2]\n      - title: t\n        url: /u\n");
		// empty values where a list, a mapping or a string is expected
		yield return ("SiteNavigationLoader.Load", "phantoms:\ntoc:\n");
		yield return ("SiteNavigationLoader.Load", "phantoms:\n  -\n  - toc:\ntoc:\n  - toc: a://b\n    path_prefix: x\n");
		yield return ("AssemblyLoader.Load", "environments:\n  prod:\n");
		yield return ("AssemblyLoader.Load", "environments:\n  prod:\n    feature_flags:\n");
		yield return ("AssemblyLoader.Load", "references:\n  x:\n    sparse_paths:\n");
		yield return ("AssemblyLoader.Load", "environments:\nreferences:\nshared_configuration:\n");
	}

	/// <summary>
	/// Law: a loader ends, and throws nothing but a parser exception, for each of these texts.
	/// Consequence if broken: one stray line in a configuration file hangs a build or a Lambda, or ends it with a NullReferenceException and no file or line.
	/// </summary>
	[Test]
	[Timeout(60_000)]
	[MethodDataSource(nameof(Cases))]
	public async Task ALoader_EndsOnTheseTexts_AndThrowsOnlyWhatItDocuments(string entry, string text, CancellationToken cancellationToken)
	{
		var load = LoaderProperties.YamlEntries().Single(e => e.Name == entry);
		var run = Task.Run(
			() =>
			{
				try
				{
					_ = load.Load(text);
				}
				catch (YamlException)
				{
					// the parser rejects the document, and says where
				}
			},
			cancellationToken
		);

		// a loader that spins fails the test instead of hanging it
		var act = async () => await run.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
		_ = await act.Should().NotThrowAsync($"{entry} over\n{text}");
	}
}
