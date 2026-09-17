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
		string? description,
		string? descriptionFile,
		bool clearDescription,
		TextReader? stdin,
		Cancel ctx
	)
	{
		var specified = 0;
		if (description != null)
			specified++;
		if (!string.IsNullOrWhiteSpace(descriptionFile))
			specified++;
		if (clearDescription)
			specified++;

		if (specified > 1)
		{
			collector.EmitError(string.Empty, "--description, --description-file, and --clear-description are mutually exclusive.");
			return BundleDescriptionInputResult.Fail;
		}

		if (specified == 0)
			return BundleDescriptionInputResult.None;

		if (clearDescription)
			return BundleDescriptionInputResult.Patch(string.Empty);

		if (description != null)
			return BundleDescriptionInputResult.Patch(description);

		var trimmedFile = descriptionFile!.Trim();
		if (trimmedFile == StdinPath)
		{
			if (stdin == null)
			{
				collector.EmitError(string.Empty, "--description-file - requires standard input.");
				return BundleDescriptionInputResult.Fail;
			}

			var stdinText = await stdin.ReadToEndAsync(ctx);
			return BundleDescriptionInputResult.Patch(stdinText.TrimEnd('\r', '\n'));
		}

		if (!fileSystem.File.Exists(trimmedFile))
		{
			collector.EmitError(trimmedFile, $"Description file not found: {trimmedFile}");
			return BundleDescriptionInputResult.Fail;
		}

		var fileText = await fileSystem.File.ReadAllTextAsync(trimmedFile, ctx);
		return BundleDescriptionInputResult.Patch(fileText.TrimEnd('\r', '\n'));
	}
}

/// <summary>Outcome of <see cref="BundleDescriptionInput.ResolveAsync"/>.</summary>
public sealed record BundleDescriptionInputResult(bool Success, bool HasPatch, string? Value)
{
	public static BundleDescriptionInputResult Fail { get; } = new(false, false, null);

	public static BundleDescriptionInputResult None { get; } = new(true, false, null);

	public static BundleDescriptionInputResult Patch(string? value) => new(true, true, value);
}
