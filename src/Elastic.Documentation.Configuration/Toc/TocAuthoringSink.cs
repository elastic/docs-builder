// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// Collects what <see cref="TocItemYamlConverter"/> hides while it reads <c>toc:</c> entries (see <c>Constraints-Conventions.md</c>).
/// The converter runs inside the YAML deserializer, which has no way to hand a side result back, so a load opens a scope on its
/// own thread and every entry read inside it is recorded; outside a scope nothing is recorded. Loading is synchronous.
/// </summary>
internal static class TocAuthoringSink
{
	[ThreadStatic]
	private static List<TocEntryAuthoring>? CurrentEntries;

	[ThreadStatic]
	private static string? CurrentFile;

	/// <summary>Starts collecting into <paramref name="entries"/>; dispose the result to restore what was collecting before.</summary>
	public static Scope Begin(List<TocEntryAuthoring> entries, string file)
	{
		var previous = new Scope(CurrentEntries, CurrentFile);
		CurrentEntries = entries;
		CurrentFile = file;
		return previous;
	}

	/// <summary>Names the file entries are read from, until the returned scope is disposed.</summary>
	public static Scope InFile(string file)
	{
		var previous = new Scope(CurrentEntries, CurrentFile);
		CurrentFile = file;
		return previous;
	}

	public static void Record(TocEntryAuthoring entry) => CurrentEntries?.Add(entry with { File = CurrentFile });

	internal readonly struct Scope(List<TocEntryAuthoring>? entries, string? file) : IDisposable
	{
		public void Dispose()
		{
			CurrentEntries = entries;
			CurrentFile = file;
		}
	}
}
