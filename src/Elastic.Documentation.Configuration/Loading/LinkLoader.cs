// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;
using Elastic.Documentation.Serialization;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// How a <c>links.json</c> (<see cref="LinkManifest"/>) or a <c>link-index.json</c> (<see cref="LinkRegistry"/>) becomes a record, wherever its text
/// comes from (a file, S3, a git clone, the application cache). Text that does not parse is an error finding with the parser's message and no record;
/// text that parses always returns its record together with every finding, even an error, because the file belongs to another repository more often
/// than not and the caller decides whether a finding stops it.
/// </summary>
public static class LinkLoader
{
	private static readonly Loader<LinkManifest, LinkManifest> Manifest = Loader.For(
		parse: (text, _) => JsonSerializer.Deserialize(text, SourceGenerationContext.Default.LinkManifest) ??
			throw new JsonException("The links.json is empty."),
		map: (manifest, _) => manifest,
		errorsAreFatal: false
	);

	private static readonly Loader<LinkRegistry, LinkRegistry> Registry = Loader.For(
		parse: (text, _) => JsonSerializer.Deserialize(text, SourceGenerationContext.Default.LinkRegistry) ??
			throw new JsonException("The link-index.json is empty."),
		map: (registry, _) => registry,
		errorsAreFatal: false
	);

	/// <summary>Loads a <c>links.json</c>.</summary>
	/// <param name="json">The file's text.</param>
	/// <param name="file">What findings are reported against (a path, a bucket key).</param>
	public static LoadResult<LinkManifest> LoadManifest(string json, string file) => Manifest.Load(json, file);

	/// <summary>Loads a <c>link-index.json</c>.</summary>
	/// <param name="json">The file's text.</param>
	/// <param name="file">What findings are reported against (a path, a bucket key).</param>
	/// <param name="eTag">The ETag of the object it came from, kept on the record so it can be written back conditionally.</param>
	public static LoadResult<LinkRegistry> LoadRegistry(string json, string file, string? eTag = null) => Registry.Load(json, file) switch
	{
		Loaded<LinkRegistry> loaded when eTag is not null => new Loaded<LinkRegistry>(loaded.Value with { ETag = eTag }, loaded.Findings),
		var result => result
	};

	/// <summary>
	/// The manifest a consumer builds on, read as <c>links.json</c> always was: the same <see cref="JsonException"/> with the parser's message for text
	/// that does not parse, and <c>null</c> for the literal <c>null</c> (which the loader reports as an empty file). The findings are not reported.
	/// </summary>
	public static LinkManifest ReadManifest(string json, string file) =>
		IsNullLiteral(json) ? null! : LoadManifest(json, file).OrThrowAsJsonException();

	/// <summary>The registry a consumer builds on, read as <c>link-index.json</c> always was; see <see cref="ReadManifest"/>.</summary>
	public static LinkRegistry ReadRegistry(string json, string file, string? eTag = null) =>
		IsNullLiteral(json) ? NullRegistry(eTag) : LoadRegistry(json, file, eTag).OrThrowAsJsonException();

	// the S3 reader always wrote the ETag with `with`, which throws on the null the raw reader returned for a literal null; the other readers returned the null
	private static LinkRegistry NullRegistry(string? eTag) => eTag is null ? null! : ((LinkRegistry)null!) with { ETag = eTag };

	private static bool IsNullLiteral(string json) => json.AsSpan().Trim().SequenceEqual("null");

	private static T OrThrowAsJsonException<T>(this LoadResult<T> result) where T : class =>
		result.ValueOrNull() ?? throw new JsonException(result.Findings().Last(d => d.Severity == Severity.Error).Message);
}
