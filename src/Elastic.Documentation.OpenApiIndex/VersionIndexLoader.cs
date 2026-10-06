// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Nullean.Entities;

namespace Elastic.Documentation.OpenApiIndex;

/// <summary>
/// What loading the root <c>index.json</c> produced: the index when the text parsed, the parser's message when it did not, and the
/// <see cref="VersionIndexConstraints"/> report of the index (every violation, not the first). This project does not depend on the domain assembly
/// (the lambda that publishes the index stays small), so the outcome is a report rather than a <c>Result&lt;T&gt;</c>.
/// </summary>
/// <param name="Index">The index; null when the text did not parse or was empty.</param>
/// <param name="Error">Why there is no index; null when there is one.</param>
/// <param name="Report">What the index breaks; null when there is no index.</param>
public sealed record VersionIndexLoad(RootVersionIndex? Index, string? Error, ConstraintReport<RootVersionIndex>? Report)
{
	/// <summary>True when the index could not be used: it did not parse, or it breaks a rule of Error severity.</summary>
	public bool HasErrors => Error is not null || Report is { HasErrors: true };
}

/// <summary>
/// The one definition of a "checked version index", shared by the read side (the API explorer fetching it) and the write side (the lambda that rebuilds
/// it): text goes in, the index comes out together with what it breaks, and a built index is checked through the same gate before it is published.
/// </summary>
public static class VersionIndexLoader
{
	/// <summary>The message when the text parsed to nothing.</summary>
	public const string EmptyMessage = "the response was empty or could not be parsed";

	/// <summary>Checks an index once, against every rule of <see cref="VersionIndexConstraints"/>. The write side's gate, and the last step of every load.</summary>
	public static ConstraintReport<RootVersionIndex> Check(RootVersionIndex index) => VersionIndexConstraints.All.Report(index);

	/// <summary>Loads the index from its text.</summary>
	public static VersionIndexLoad Load(string json)
	{
		try
		{
			return Concluded(
				JsonSerializer.Deserialize(
					json,
					VersionIndexJsonContext.Default.SortedDictionaryStringSortedDictionaryStringSortedDictionaryStringVersionIndexEntry
				)
			);
		}
		catch (JsonException e)
		{
			return new VersionIndexLoad(null, e.Message, null);
		}
	}

	/// <summary>
	/// Loads the index from a stream. Only a text that is not valid JSON is a parse error; a failure of the stream itself (the network, the disk) is the
	/// caller's to handle, as it was before the index was checked.
	/// </summary>
	public static async Task<VersionIndexLoad> LoadAsync(Stream json, Cancel ctx = default)
	{
		try
		{
			var index = await JsonSerializer.DeserializeAsync(
				json,
				VersionIndexJsonContext.Default.SortedDictionaryStringSortedDictionaryStringSortedDictionaryStringVersionIndexEntry,
				ctx
			).ConfigureAwait(false);
			return Concluded(index);
		}
		catch (JsonException e)
		{
			return new VersionIndexLoad(null, e.Message, null);
		}
	}

	private static VersionIndexLoad Concluded(RootVersionIndex? index) =>
		index is null ? new VersionIndexLoad(null, EmptyMessage, null) : new VersionIndexLoad(index, null, Check(index));
}
