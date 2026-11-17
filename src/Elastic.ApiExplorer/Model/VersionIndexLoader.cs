// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>Reads the version index (<c>index.json</c>) into the <see cref="VersionIndex"/> model. Every level keeps the order the index wrote it in.</summary>
internal static class VersionIndexLoader
{
	/// <summary>The message when the text parsed to nothing.</summary>
	public const string EmptyMessage = "the response was empty or could not be parsed";

	// The index belongs to the CDN, not to this build: what it breaks never stops the render, so errors are not fatal here.
	public static readonly Loader<RootVersionIndex, VersionIndex> Json = Loader.For(
		parse: (text, _) => JsonSerializer.Deserialize(
			text,
			VersionIndexJsonContext.Default.DictionaryStringDictionaryStringDictionaryStringVersionIndexEntry
		) ?? throw new JsonException(EmptyMessage),
		map: (dto, _) => ToModel(dto),
		errorsAreFatal: false
	);

	/// <summary>The index of the text, or the parser's message; null for a text that parsed to nothing is the message <see cref="EmptyMessage"/>.</summary>
	public static LoadResult<VersionIndex> Load(string text, string file) => Json.Load(text, file);

	private static VersionIndex ToModel(RootVersionIndex dto) =>
		new([
			.. dto.Select(
				repo => new VersionIndexRepository(
					repo.Key,
					[
						.. repo.Value.Select(
							spec => new VersionIndexSpec(spec.Key, [.. spec.Value.Select(m => new VersionMoniker(m.Key, m.Value.Version))])
						)
					]
				)
			)
		]);
}
