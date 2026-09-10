// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;

namespace Elastic.Changelog.Creation;

/// <summary>Arguments for <see cref="ChangelogUnpackService.UnpackBundle"/>.</summary>
public record UnpackBundleArguments
{
	public required string BundleFile { get; init; }
	public string? Output { get; init; }
	public string? Config { get; init; }
	public bool Concise { get; init; }
}

/// <summary>
/// Recreates individual changelog YAML files from a bundle by mapping each entry onto
/// <see cref="ChangelogCreationService.CreatePreparedChangelog"/> or
/// <see cref="ChangelogCreationService.CreateNote"/>.
/// </summary>
public sealed class ChangelogUnpackService(
	ILoggerFactory logFactory,
	IChangelogFileSystem fileSystem,
	IConfigurationContext configurationContext
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogUnpackService>();
	private readonly ChangelogCreationService _creation = new(logFactory, configurationContext, fileSystem);

	private readonly record struct UnpackEntryContext(UnpackBundleArguments Input, string Owner, string? Repo);

	public async Task<bool> UnpackBundle(IDiagnosticsCollector collector, UnpackBundleArguments input, Cancel ctx)
	{
		if (!fileSystem.File.Exists(input.BundleFile))
		{
			collector.EmitError(input.BundleFile, "Bundle file does not exist");
			return false;
		}

		Bundle bundle;
		try
		{
			var yaml = await fileSystem.File.ReadAllTextAsync(input.BundleFile, ctx);
			bundle = ReleaseNotesSerialization.DeserializeBundle(yaml);
		}
		catch (YamlException yamlEx)
		{
			collector.EmitError(input.BundleFile, $"Failed to deserialize bundle file: {yamlEx.Message}", yamlEx);
			return false;
		}

		var entries = await ResolveEntries(collector, input.BundleFile, bundle, ctx);
		if (entries == null)
			return false;

		if (entries.Count == 0)
		{
			_logger.LogInformation("No changelog entries to unpack from {BundleFile}", input.BundleFile);
			return true;
		}

		var (owner, repo) = ResolveOwnerRepo(bundle);
		var entryContext = new UnpackEntryContext(input, owner, repo);
		var anyFailed = false;
		foreach (var entry in entries)
		{
			if (!await UnpackEntry(collector, entryContext, entry, ctx))
				anyFailed = true;
		}

		return !anyFailed && collector.Errors == 0;
	}

	private async Task<IReadOnlyList<BundledEntry>?> ResolveEntries(
		IDiagnosticsCollector collector,
		string bundleFile,
		Bundle bundle,
		Cancel ctx
	)
	{
		if (BundleAmendMerger.IsAmendFile(bundleFile))
		{
			if (bundle.ExcludeEntries.Count > 0)
			{
				_logger.LogInformation(
					"Skipping {Count} exclude-entries on amend sidecar {BundleFile}; exclusions cannot be recreated as changelog files",
					bundle.ExcludeEntries.Count,
					bundleFile
				);
			}

			return bundle.Entries;
		}

		var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(fileSystem, bundleFile);
		if (amendFiles.Count == 0)
			return bundle.Entries;

		_logger.LogInformation("Found {Count} amend file(s) for bundle {BundleFile}", amendFiles.Count, bundleFile);
		var amendBundles = new List<Bundle>();
		foreach (var amendFile in amendFiles)
		{
			try
			{
				var amendContent = await fileSystem.File.ReadAllTextAsync(amendFile, ctx);
				var amendBundle = ReleaseNotesSerialization.DeserializeBundle(amendContent);
				amendBundles.Add(amendBundle);
				_logger.LogInformation(
					"Merging amend file {AmendFile} ({AddCount} additions, {ExcludeCount} exclusions)",
					amendFile,
					amendBundle.Entries.Count,
					amendBundle.ExcludeEntries.Count
				);
			}
			catch (YamlException yamlEx)
			{
				collector.EmitError(amendFile, $"Failed to deserialize amend file: {yamlEx.Message}", yamlEx);
				return null;
			}
		}

		return BundleAmendMerger.MergeEntries(bundle.Entries, amendBundles);
	}

	private async Task<bool> UnpackEntry(IDiagnosticsCollector collector, UnpackEntryContext context, BundledEntry entry, Cancel ctx)
	{
		if (IsMarker(entry))
		{
			_logger.LogInformation("Skipping marker entry {FileName}", entry.File?.Name ?? "<unnamed>");
			return true;
		}

		var mapped = MapToArguments(context, entry);
		if (mapped == null)
		{
			collector.EmitError(
				context.Input.BundleFile,
				EntryLabel(entry) + " cannot be unpacked: after stripping private-link sentinels it has no pull-request " +
					"references and its products have no versions. " +
					"changelog add requires a PR; changelog note requires product versions. " +
					"Bundles scrubbed during changelog upload often drop private PRs and issues — unpack the private-side bundle instead."
			);
			return false;
		}

		var (args, useNote) = mapped.Value;
		EmitFilenameWarning(collector, entry, args, useNote);

		return useNote ? await _creation.CreateNote(collector, args, ctx) : await _creation.CreatePreparedChangelog(collector, args, ctx);
	}

	private static (CreateChangelogArguments Args, bool UseNote)? MapToArguments(UnpackEntryContext context, BundledEntry entry)
	{
		var prs = ChangelogTextUtilities.StripPrivateReferenceSentinels(entry.Prs);
		var issues = ChangelogTextUtilities.StripPrivateReferenceSentinels(entry.Issues);
		var products = ToProductArguments(entry.Products);
		var hasPrs = prs is { Length: > 0 };
		var hasVersions = products.Any(p => p.Versions.Count > 0);

		if (!hasPrs && !hasVersions)
			return null;

		var useNote = !hasPrs || hasVersions;
		var args = new CreateChangelogArguments
		{
			Title = entry.Title,
			Type = entry.Type?.ToStringFast(true),
			Subtype = entry.Subtype?.ToStringFast(true),
			Products = products,
			Areas = entry.Areas?.ToArray() ?? [],
			Prs = prs,
			Issues = issues,
			Description = entry.Description,
			Impact = entry.Impact,
			Action = entry.Action,
			FeatureId = entry.FeatureId,
			Highlight = entry.Highlight,
			Owner = context.Owner,
			Repo = context.Repo,
			Output = context.Input.Output,
			Config = context.Input.Config,
			Concise = context.Input.Concise,
			ExtractReleaseNotes = false,
			ExtractIssues = false,
			UsePrNumber = true,
			IsNote = useNote
		};
		return (args, useNote);
	}

	private static void EmitFilenameWarning(
		IDiagnosticsCollector collector,
		BundledEntry entry,
		CreateChangelogArguments args,
		bool useNote
	)
	{
		var provenance = LeafFileName(entry.File?.Name);
		if (string.IsNullOrWhiteSpace(provenance))
			return;

		var expected = useNote
			? ChangelogFileWriter.GetNoteFileName(args.NoteName, args.Title)
			: ChangelogFileWriter.TryGetChangelogFileName(args);
		if (string.IsNullOrWhiteSpace(expected))
			return;

		if (string.Equals(provenance, expected, StringComparison.OrdinalIgnoreCase))
			return;

		collector.EmitWarning(
			string.Empty,
			$"Unpack will write '{expected}' for provenance file '{provenance}' using changelog {(useNote ? "note" : "add")} naming."
		);
	}

	private static IReadOnlyList<ProductArgument> ToProductArguments(IReadOnlyList<ProductReference>? products)
	{
		if (products is not { Count: > 0 })
			return [];

		return products.Select(
			p => new ProductArgument
			{
				Product = p.ProductId,
				Target = p.Versions.Count > 0 ? string.Join('|', p.Versions) : null,
				Lifecycle = p.Lifecycle?.ToStringFast(true)
			}
		).ToList();
	}

	private static (string Owner, string? Repo) ResolveOwnerRepo(Bundle bundle)
	{
		BundledProduct? product = null;
		foreach (var candidate in bundle.Products)
		{
			if (string.IsNullOrWhiteSpace(candidate.Repo))
				continue;
			product = candidate;
			break;
		}

		product ??= bundle.Products.Count > 0 ? bundle.Products[0] : null;
		return (string.IsNullOrWhiteSpace(product?.Owner) ? "elastic" : product.Owner, product?.Repo);
	}

	private static bool IsMarker(BundledEntry entry) => !string.IsNullOrWhiteSpace(entry.Link) && string.IsNullOrWhiteSpace(entry.Title);

	private static string EntryLabel(BundledEntry entry) =>
		!string.IsNullOrWhiteSpace(entry.File?.Name) ? $"Entry '{LeafFileName(entry.File.Name)}'" : $"Entry '{entry.Title ?? "<unnamed>"}'";

	private static string? LeafFileName(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return null;
		return Path.GetFileName(name.Replace('\\', '/'));
	}
}
