// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Equality;
using Generator.Equals;

namespace Elastic.Documentation.Tests.Domain;

// The two shapes the comparers exist for, wired the way a domain record wires them: Generator.Equals finds the comparer's static Default.
[Equatable]
internal sealed partial record ComparerHolder
{
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public required IFileInfo File { get; init; }

	[UnorderedEquality(typeof(StringListEntryComparer))]
	public required IReadOnlyDictionary<string, IReadOnlyList<string>> Lists { get; init; }
}

public class ComparersTests
{
	private static readonly MockFileSystem FileSystem = new();

	private static IFileInfo File(string path) => FileSystem.FileInfo.New(path);

	private static Gen<string> Names { get; } = Gen.String.AlphaNumeric.Where(s => s.Length > 0);

	private static Gen<IReadOnlyList<string>> StringLists { get; } = Names.List[0, 4].Select(l => (IReadOnlyList<string>)l);

	private static Gen<KeyValuePair<string, IReadOnlyList<string>>> Entries { get; } = Gen.Select(Names, StringLists).Select(
		t => new KeyValuePair<string, IReadOnlyList<string>>(t.Item1, t.Item2)
	);

	/// <summary>
	/// Law: two file handles to the same full path are equal and hash alike, however they were created.
	/// Consequence if broken: a record that holds a file compares unequal after loading the same file twice.
	/// </summary>
	[Test]
	public void FileSystemInfoLocation_SamePath_IsEqual() =>
		Names.Check(name =>
		{
			var comparer = FileSystemInfoLocationComparer.Default;
			var (a, b) = (File("/docs/" + name), File("/docs/" + name));

			comparer.Equals(a, b).Should().BeTrue();
			comparer.GetHashCode(a).Should().Be(comparer.GetHashCode(b));
			comparer.Equals(a, File("/docs/other-" + name)).Should().BeFalse();
		});

	[Test]
	public void FileSystemInfoLocation_NullAndSameReference()
	{
		var comparer = FileSystemInfoLocationComparer.Default;
		var file = File("/a");

		comparer.Equals(file, file).Should().BeTrue();
		comparer.Equals(null, null).Should().BeTrue();
		comparer.Equals(file, null).Should().BeFalse();
		comparer.Equals(null, file).Should().BeFalse();
	}

	/// <summary>
	/// Law: two entries are equal exactly when the keys match and the lists hold the same strings in the same order; equal entries hash alike.
	/// Consequence if broken: two loads of one catalog differ because a list was compared by reference.
	/// </summary>
	[Test]
	public void StringListEntry_EqualExactlyWhenKeyAndOrderedItemsMatch() =>
		Gen.Select(Entries, Entries).Check(pair =>
		{
			var (x, y) = pair;
			var comparer = StringListEntryComparer.Default;
			var expected = x.Key == y.Key && x.Value.SequenceEqual(y.Value);

			comparer.Equals(x, y).Should().Be(expected);
			if (expected)
				comparer.GetHashCode(x).Should().Be(comparer.GetHashCode(y));
			comparer.Equals(x, new KeyValuePair<string, IReadOnlyList<string>>(x.Key, [.. x.Value])).Should().BeTrue();
		});

	[Test]
	public void StringListEntry_OrderOfItemsMatters()
	{
		KeyValuePair<string, IReadOnlyList<string>> Entry(params string[] items) => new("k", items);

		StringListEntryComparer.Default.Equals(Entry("a", "b"), Entry("b", "a")).Should().BeFalse();
	}

	[Test]
	public void Generator_UsesTheComparers_ForFilesAndDictionaryEntries()
	{
		ComparerHolder Make(string path, params string[] items) =>
			new() { File = File(path), Lists = new Dictionary<string, IReadOnlyList<string>> { ["k"] = items, ["other"] = [] } };

		Make("/a", "x", "y").Should().Be(Make("/a", "x", "y"));
		Make("/a", "x", "y").Should().NotBe(Make("/b", "x", "y"));
		Make("/a", "x", "y").Should().NotBe(Make("/a", "y", "x"));
	}
}
