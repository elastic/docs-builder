// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Resolves bundle-intro text from mutually exclusive CLI sources: inline string, file, stdin, or clear.
/// </summary>
public static class BundleDescriptionInput
{
	/// <summary>Sentinel path that reads the description from stdin.</summary>
	public const string StdinPath = "-";

	/// <summary>
	/// Shared error when an amend run specifies neither entry changes nor a description patch.
	/// </summary>
	public const string AmendRequiresChange =
		"At least one of --add, --remove, --description, --description-file, or --clear-description must be specified";

	/// <summary>
	/// Reads the description patch. <see cref="BundleDescriptionInputResult.HasPatch"/> is false when
	/// no description source was given. Empty <see cref="BundleDescriptionInputResult.Value"/> means clear.
	/// </summary>
	public static async Task<BundleDescriptionInputResult> ResolveAsync(
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		BundleDescriptionRequest request,
		Cancel ctx
	)
	{
		// Presence decides, not content. A whitespace-only path still means the caller passed the flag,
		// and an empty value from a CI variable must fail rather than fall back to the config intro.
		var specified = 0;
		if (request.Description != null)
			specified++;
		if (request.DescriptionFile != null)
			specified++;
		if (request.ClearDescription)
			specified++;

		if (specified > 1)
		{
			collector.EmitError(string.Empty, "--description, --description-file, and --clear-description are mutually exclusive.");
			return BundleDescriptionInputResult.Fail;
		}

		if (specified == 0)
			return BundleDescriptionInputResult.None;

		if (request.ClearDescription)
			return BundleDescriptionInputResult.Patch(string.Empty);

		return request.Description != null
			? BundleDescriptionInputResult.Patch(request.Description)
			: await ReadAsync(collector, fileSystem, request, ctx).ConfigureAwait(false);
	}

	private static async Task<BundleDescriptionInputResult> ReadAsync(
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		BundleDescriptionRequest request,
		Cancel ctx
	)
	{
		var trimmedFile = request.DescriptionFile!.Trim();
		if (trimmedFile.Length == 0)
		{
			collector.EmitError(string.Empty, $"--description-file requires a path, or {StdinPath} to read from standard input.");
			return BundleDescriptionInputResult.Fail;
		}

		if (trimmedFile == StdinPath)
		{
			if (request.Stdin == null)
			{
				collector.EmitError(string.Empty, "--description-file - requires standard input.");
				return BundleDescriptionInputResult.Fail;
			}

			var stdinText = await request.Stdin.ReadToEndAsync(ctx).ConfigureAwait(false);
			return Accept(collector, null, stdinText);
		}

		if (!fileSystem.File.Exists(trimmedFile))
		{
			collector.EmitError(trimmedFile, $"Description file not found: {trimmedFile}");
			return BundleDescriptionInputResult.Fail;
		}

		var fileText = await fileSystem.File.ReadAllTextAsync(trimmedFile, ctx).ConfigureAwait(false);
		return Accept(collector, trimmedFile, fileText);
	}

	/// <summary>
	/// A NUL byte means the source was not UTF-8 text, most often a UTF-16 file saved with no byte order
	/// mark. The bundle serializer escapes the byte rather than breaking the YAML, but the intro would
	/// still be unreadable, so the run stops here where the source can be named.
	/// </summary>
	private static BundleDescriptionInputResult Accept(IDiagnosticsCollector collector, string? file, string text)
	{
		if (text.Contains('\0', StringComparison.Ordinal))
		{
			collector.EmitError(
				file ?? string.Empty,
				$"The description read from {file ?? "standard input"} is not UTF-8 text because it contains a NUL byte. " +
					"Save the source as UTF-8 and run the command again."
			);
			return BundleDescriptionInputResult.Fail;
		}

		return BundleDescriptionInputResult.Patch(text.TrimEnd('\r', '\n'));
	}
}

/// <summary>
/// Mutually exclusive description sources passed to <see cref="BundleDescriptionInput.ResolveAsync"/>.
/// A <c>DescriptionFile</c> of <see cref="BundleDescriptionInput.StdinPath"/> reads from <c>Stdin</c>.
/// </summary>
public sealed record BundleDescriptionRequest(string? Description, string? DescriptionFile, bool ClearDescription, TextReader? Stdin);

/// <summary>Outcome of <see cref="BundleDescriptionInput.ResolveAsync"/>.</summary>
public sealed record BundleDescriptionInputResult(bool Success, bool HasPatch, string? Value)
{
	public static BundleDescriptionInputResult Fail { get; } = new(false, false, null);

	public static BundleDescriptionInputResult None { get; } = new(true, false, null);

	public static BundleDescriptionInputResult Patch(string? value) => new(true, true, value);
}
