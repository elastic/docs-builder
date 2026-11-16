// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;
using Elastic.Documentation.Serialization;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// The one way a <c>links.json</c> (<see cref="LinkManifest"/>) or a <c>link-index.json</c> (<see cref="LinkRegistry"/>) becomes a record, wherever its
/// text comes from (a file, S3, a git clone, the application cache): the wire format's raw reader is internal to the domain, so nothing outside this
/// loader gets a record without the aggregate's rules having run, once.
/// <para>
/// A text that does not parse is an error diagnostic and no record. A text that parses always returns its record together with every finding, even an
/// error: the file belongs to another repository more often than not, so the caller decides whether a finding stops it (see
/// <see cref="LinkLoaderExtensions.Unwrap{T}"/> for the one that logs and carries on).
/// </para>
/// </summary>
public static class LinkLoader
{
	/// <summary>Loads a <c>links.json</c>: parses and checks it against <see cref="LinkManifestConstraints"/>.</summary>
	/// <param name="json">The file's text.</param>
	/// <param name="file">What diagnostics are reported against (a path, a bucket key).</param>
	public static Result<LinkManifest> LoadManifest(string json, string file) =>
		LoaderPipeline.Run(
			file,
			() => LinkJson.DeserializeManifest(json) ?? throw new JsonException("The links.json is empty."),
			LinkManifestConstraints.All,
			errorsAreFatal: false
		);

	/// <inheritdoc cref="LoadManifest(string,string)"/>
	public static Result<LinkManifest> LoadManifest(Stream json, string file) =>
		LoaderPipeline.Run(
			file,
			() => LinkJson.DeserializeManifest(json) ?? throw new JsonException("The links.json is empty."),
			LinkManifestConstraints.All,
			errorsAreFatal: false
		);

	/// <summary>Loads a <c>link-index.json</c>: parses and checks it against <see cref="LinkRegistryConstraints"/>.</summary>
	/// <param name="json">The file's text.</param>
	/// <param name="file">What diagnostics are reported against (a path, a bucket key).</param>
	/// <param name="eTag">The ETag of the object it came from, kept on the record so it can be written back conditionally.</param>
	public static Result<LinkRegistry> LoadRegistry(string json, string file, string? eTag = null) =>
		LoaderPipeline.Run(
			file,
			() => (LinkJson.DeserializeRegistry(json) ?? throw new JsonException("The link-index.json is empty.")) with { ETag = eTag },
			LinkRegistryConstraints.All,
			errorsAreFatal: false
		);

	/// <inheritdoc cref="LoadRegistry(string,string,string)"/>
	public static Result<LinkRegistry> LoadRegistry(Stream json, string file, string? eTag = null) =>
		LoaderPipeline.Run(
			file,
			() => (LinkJson.DeserializeRegistry(json) ?? throw new JsonException("The link-index.json is empty.")) with { ETag = eTag },
			LinkRegistryConstraints.All,
			errorsAreFatal: false
		);

	/// <summary>Writes a manifest as the <c>links.json</c> text.</summary>
	public static string Serialize(LinkManifest manifest) => LinkJson.Serialize(manifest);

	/// <summary>Writes a registry as the <c>link-index.json</c> text.</summary>
	public static string Serialize(LinkRegistry registry) => LinkJson.Serialize(registry);
}

/// <summary>What a consumer of a loaded link artifact does with the findings.</summary>
public static class LinkLoaderExtensions
{
	/// <summary>
	/// Logs every finding of <paramref name="result"/> and returns the value, or throws when the text did not parse. A finding is logged, never thrown:
	/// the artifact belongs to another repository, so a rule it breaks is that repository's to fix and never stops this build. An error is logged as a
	/// warning, a warning as information, anything else as debug.
	/// </summary>
	public static T Unwrap<T>(this Result<T> result, ILogger logger, string source) where T : class
	{
		if (result.Value is not null)
			result.LogFindings(logger, source);
		return result.OrThrowIfMissing();
	}

	/// <summary>Logs the findings of <paramref name="result"/>, mapping their severity as <see cref="Unwrap{T}"/> does.</summary>
	public static void LogFindings<T>(this Result<T> result, ILogger logger, string source) where T : class
	{
		foreach (var diagnostic in result.Diagnostics)
		{
			var level = diagnostic.Severity switch
			{
				Severity.Error => LogLevel.Warning,
				Severity.Warning => LogLevel.Information,
				_ => LogLevel.Debug
			};
			logger.Log(level, "{Source} breaks {RuleId}: {Message}", source, diagnostic.RuleId, diagnostic.Message);
		}
	}
}
