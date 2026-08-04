// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;

namespace Elastic.Changelog.Backfill.Inventory;

public record BuildInventoryArguments
{
	/// <summary>Path to the hand-maintained seed YAML (see <see cref="InventorySourcesSeed"/>). Optional: without it every product is reported as unresolved.</summary>
	public string? SourcesPath { get; init; }

	/// <summary>Where to write the inventory document JSON.</summary>
	public required string OutputPath { get; init; }

	/// <summary>
	/// The scrubber link allowlist to compute each attributed repository's status against, as
	/// <c>owner/repo</c> entries. Sourced from the local <c>assembler.yml</c> today; planning
	/// re-validates against the deployed allowlist identity before any upload.
	/// </summary>
	public IReadOnlyList<string> AllowRepos { get; init; } = [];
}

/// <summary>
/// The census (docs-eng-team#673): enumerates every product in <c>products.yml</c> that
/// participates in release notes — the feature defaults to enabled, so <c>products.yml</c>
/// alone cannot say which products have release-note surfaces — merges in the hand-maintained
/// source seed, and writes the inventory document planning consumes. Products the seed does
/// not cover stay visible as <c>source-unresolved</c> entries: "we looked and decided no"
/// must always be distinguishable from "we never looked", and an unresolved scope can never
/// silently produce empty bundles.
/// </summary>
public class InventoryCensusService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	IFileSystem fileSystem
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<InventoryCensusService>();

	/// <summary>Builds the inventory document and writes it to the output path. Returns false after emitting errors when the census cannot be produced.</summary>
	public async Task<bool> BuildInventoryAsync(IDiagnosticsCollector collector, BuildInventoryArguments args, Cancel ctx = default)
	{
		var releaseNotesProducts = configurationContext.ProductsConfiguration.Products.Values
			.Where(p => p.Features.ReleaseNotes)
			.OrderBy(p => p.Id, StringComparer.Ordinal)
			.ToList();

		var seed = await LoadSeedAsync(collector, args.SourcesPath, ctx);
		if (seed is null)
			return false;

		if (!ValidateSeed(collector, seed, releaseNotesProducts))
			return false;

		var sources = new List<InventorySource>();
		foreach (var seedSource in seed.Sources)
			sources.Add(BuildSource(seedSource, args.AllowRepos, releaseNotesProducts));

		var mappedProductIds = seed.Sources.SelectMany(s => s.Products)
			.Concat(seed.Unmapped.Select(u => u.Product ?? ""))
			.ToHashSet(StringComparer.Ordinal);

		foreach (var unmapped in seed.Unmapped)
		{
			var product = releaseNotesProducts.First(p => p.Id == unmapped.Product);
			sources.Add(BuildUnresolvedSource(product, unmapped.Reason!));
		}

		foreach (var product in releaseNotesProducts.Where(p => !mappedProductIds.Contains(p.Id)))
		{
			collector.EmitWarning(args.SourcesPath ?? string.Empty,
				$"Product '{product.Id}' declares release notes but the seed neither maps a source for it nor defers it under 'unmapped'; recording it as source-unresolved.");
			sources.Add(BuildUnresolvedSource(product,
				"Not covered by the census seed: no release-note source recorded and not explicitly deferred."));
		}

		var inventory = new InventoryDocument { Sources = sources };

		string json;
		try
		{
			json = BackfillDocuments.Serialize(inventory);
		}
		catch (BackfillDocumentException e)
		{
			collector.EmitError(args.OutputPath, $"The census produced an invalid inventory document: {e.Message}", e);
			return false;
		}

		var outputDirectory = fileSystem.Path.GetDirectoryName(args.OutputPath);
		if (!string.IsNullOrEmpty(outputDirectory))
			_ = fileSystem.Directory.CreateDirectory(outputDirectory);
		await fileSystem.File.WriteAllTextAsync(args.OutputPath, json, ctx);

		var hash = BackfillDocuments.ComputeHash(json);
		LogSummary(sources, args.OutputPath, hash);
		return true;
	}

	private async Task<InventorySourcesSeed?> LoadSeedAsync(IDiagnosticsCollector collector, string? sourcesPath, Cancel ctx)
	{
		if (string.IsNullOrWhiteSpace(sourcesPath))
		{
			_logger.LogWarning("No census seed provided; every release-notes product will be reported as source-unresolved");
			return new InventorySourcesSeed();
		}

		if (!fileSystem.File.Exists(sourcesPath))
		{
			collector.EmitError(sourcesPath, "The census seed file does not exist.");
			return null;
		}

		var yaml = await fileSystem.File.ReadAllTextAsync(sourcesPath, ctx);
		try
		{
			return InventorySourcesSeed.Deserialize(yaml);
		}
		catch (YamlException e)
		{
			collector.EmitError(sourcesPath, $"The census seed is not valid YAML: {e.Message}", e);
			return null;
		}
	}

	private static bool ValidateSeed(IDiagnosticsCollector collector, InventorySourcesSeed seed, IReadOnlyList<Product> releaseNotesProducts)
	{
		var valid = true;
		var knownIds = releaseNotesProducts.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
		var sourceProductIds = seed.Sources.SelectMany(s => s.Products).ToHashSet(StringComparer.Ordinal);

		for (var i = 0; i < seed.Sources.Count; i++)
			valid &= ValidateSeedSource(collector, seed.Sources[i], i, knownIds);

		for (var i = 0; i < seed.Unmapped.Count; i++)
			valid &= ValidateSeedUnmapped(collector, seed.Unmapped[i], i, knownIds, sourceProductIds);

		return valid;
	}

	private static bool ValidateSeedSource(IDiagnosticsCollector collector, SeedSource source, int index, IReadOnlySet<string> knownIds)
	{
		var valid = true;
		var where = $"sources[{index}]";

		if (TryParseRepository(source.Repository) is null)
		{
			collector.EmitError(string.Empty, $"{where}: 'repository' must be owner/name (e.g. elastic/docs-content), but found '{source.Repository}'.");
			valid = false;
		}
		if (string.IsNullOrWhiteSpace(source.GitRef))
		{
			collector.EmitError(string.Empty, $"{where}: 'git_ref' is required.");
			valid = false;
		}
		if (source.Products.Count == 0)
		{
			collector.EmitError(string.Empty, $"{where}: at least one product is required.");
			valid = false;
		}
		foreach (var product in source.Products.Where(p => !knownIds.Contains(p)))
		{
			collector.EmitError(string.Empty,
				$"{where}: product '{product}' is not a release-notes product in products.yml (unknown id, or its release-notes feature is disabled).");
			valid = false;
		}
		if (ParseTargetScheme(source.TargetScheme) is null)
		{
			collector.EmitError(string.Empty, $"{where}: 'target_scheme' must be semver, date, or monthly, but found '{source.TargetScheme}'.");
			valid = false;
		}
		if (ParseClassification(source.Classification) is null)
		{
			collector.EmitError(string.Empty,
				$"{where}: 'classification' must be one of published-history-found, native-artifacts-found, hybrid-page, declared-no-history, outside-cutoff, already-live, but found '{source.Classification}'.");
			valid = false;
		}
		if (ParseAdoption(source.Adoption) is null)
		{
			collector.EmitError(string.Empty, $"{where}: 'adoption' must be not-adopted, partially-adopted, or fully-adopted, but found '{source.Adoption}'.");
			valid = false;
		}
		if (source.Cutoff is { } cutoff && ParseCutoffKind(cutoff.Kind) is null)
		{
			collector.EmitError(string.Empty, $"{where}: 'cutoff.kind' must be version or date, but found '{cutoff.Kind}'.");
			valid = false;
		}
		foreach (var attributed in source.AttributedRepositories.Where(r => TryParseRepository(r) is null))
		{
			collector.EmitError(string.Empty, $"{where}: attributed repository '{attributed}' must be owner/name.");
			valid = false;
		}
		if (source.DefaultRepository is not null && TryParseRepository(source.DefaultRepository) is null)
		{
			collector.EmitError(string.Empty, $"{where}: 'default_repository' must be owner/name, but found '{source.DefaultRepository}'.");
			valid = false;
		}

		return valid;
	}

	private static bool ValidateSeedUnmapped(
		IDiagnosticsCollector collector,
		SeedUnmapped unmapped,
		int index,
		IReadOnlySet<string> knownIds,
		IReadOnlySet<string> sourceProductIds)
	{
		var valid = true;
		var where = $"unmapped[{index}]";

		if (string.IsNullOrWhiteSpace(unmapped.Product) || !knownIds.Contains(unmapped.Product))
		{
			collector.EmitError(string.Empty, $"{where}: product '{unmapped.Product}' is not a release-notes product in products.yml.");
			valid = false;
		}
		else if (sourceProductIds.Contains(unmapped.Product))
		{
			collector.EmitError(string.Empty,
				$"{where}: product '{unmapped.Product}' is both mapped by a source and listed as unmapped; remove one of the two.");
			valid = false;
		}
		if (string.IsNullOrWhiteSpace(unmapped.Reason))
		{
			collector.EmitError(string.Empty, $"{where}: a reason is required so the deferral is auditable.");
			valid = false;
		}

		return valid;
	}

	private InventorySource BuildSource(SeedSource seedSource, IReadOnlyList<string> allowRepos, IReadOnlyList<Product> releaseNotesProducts)
	{
		var scheme = ParseTargetScheme(seedSource.TargetScheme)!.Value;
		var cutoff = seedSource.Cutoff is { } seedCutoff
			? new BackfillCutoff { Kind = ParseCutoffKind(seedCutoff.Kind)!.Value, Value = seedCutoff.Value ?? "", Notes = seedCutoff.Notes }
			: DefaultCutoff(seedSource.Products, releaseNotesProducts, scheme);

		if (seedSource.Cutoff is null && cutoff is null)
		{
			_logger.LogWarning("Source {Repository} has no cutoff and no stack default applies; planning will require one before this scope can be applied",
				seedSource.Repository);
		}

		return new InventorySource
		{
			SourceRepository = TryParseRepository(seedSource.Repository),
			GitRef = seedSource.GitRef,
			Docset = seedSource.Docset,
			Paths = seedSource.Paths,
			ProductIds = seedSource.Products,
			TargetScheme = scheme,
			Cutoff = cutoff,
			Substitutions = seedSource.Substitutions,
			LinkMappings = seedSource.LinkMappings,
			AttributedRepositories = seedSource.AttributedRepositories
				.Select(r => TryParseRepository(r)!)
				.Select(repository => new AttributedRepository
				{
					Repository = repository,
					OnScrubberAllowlist = allowRepos.Contains($"{repository.Owner}/{repository.Name}", StringComparer.OrdinalIgnoreCase)
				})
				.ToList(),
			DefaultRepository = TryParseRepository(seedSource.DefaultRepository),
			BundleFilenameConvention = seedSource.BundleFilenameConvention,
			AdoptionState = ParseAdoption(seedSource.Adoption)!.Value,
			Classification = ParseClassification(seedSource.Classification)!.Value,
			UnresolvedItems = seedSource.Unresolved
		};
	}

	private static InventorySource BuildUnresolvedSource(Product product, string reason)
	{
		var (scheme, schemeNote) = DeriveTargetScheme(product);
		var unresolved = new List<string> { reason };
		if (schemeNote is not null)
			unresolved.Add(schemeNote);

		return new InventorySource
		{
			ProductIds = [product.Id],
			TargetScheme = scheme,
			AdoptionState = AdoptionState.NotAdopted,
			Classification = SourceClassification.SourceUnresolved,
			UnresolvedItems = unresolved
		};
	}

	/// <summary>The epic's default boundary: stack-versioned products backfill from 9.0.0; anything else needs an explicit product-specific cutoff.</summary>
	private static BackfillCutoff? DefaultCutoff(IReadOnlyList<string> productIds, IReadOnlyList<Product> releaseNotesProducts, TargetScheme scheme)
	{
		if (scheme != TargetScheme.Semver)
			return null;

		var allStackVersioned = productIds
			.Select(id => releaseNotesProducts.FirstOrDefault(p => p.Id == id))
			.All(p => p?.VersioningSystem?.Id == VersioningSystemId.Stack);

		return allStackVersioned
			? new BackfillCutoff { Kind = CutoffKind.Version, Value = "9.0.0", Notes = "Stack default boundary (docs-builder era starts at stack 9.0)." }
			: null;
	}

	/// <summary>
	/// Best guess at how an unresolved product names its releases, from its versioning system.
	/// Always paired with an unresolved note — a guess must never read as a confirmed fact.
	/// </summary>
	private static (TargetScheme Scheme, string? Note) DeriveTargetScheme(Product product)
	{
		var scheme = product.VersioningSystem?.Id switch
		{
			VersioningSystemId.Serverless or VersioningSystemId.ElasticsearchProject or
				VersioningSystemId.ObservabilityProject or VersioningSystemId.SecurityProject => TargetScheme.Date,
			VersioningSystemId.Ech => TargetScheme.Monthly,
			_ => TargetScheme.Semver
		};
		return (scheme, $"Target scheme '{Name(scheme)}' was derived from the '{product.VersioningSystem?.Id.ToString() ?? "unknown"}' versioning system; confirm it when mapping this source.");
	}

	private static GitRepository? TryParseRepository(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;

		var parts = value.Split('/');
		if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
			return null;

		return new GitRepository { Owner = parts[0], Name = parts[1] };
	}

	private static TargetScheme? ParseTargetScheme(string? value) => value switch
	{
		"semver" => TargetScheme.Semver,
		"date" => TargetScheme.Date,
		"monthly" => TargetScheme.Monthly,
		_ => null
	};

	private static CutoffKind? ParseCutoffKind(string? value) => value switch
	{
		"version" => CutoffKind.Version,
		"date" => CutoffKind.Date,
		_ => null
	};

	private static AdoptionState? ParseAdoption(string? value) => value switch
	{
		"not-adopted" => AdoptionState.NotAdopted,
		"partially-adopted" => AdoptionState.PartiallyAdopted,
		"fully-adopted" => AdoptionState.FullyAdopted,
		_ => null
	};

	private static SourceClassification? ParseClassification(string? value) => value switch
	{
		"published-history-found" => SourceClassification.PublishedHistoryFound,
		"native-artifacts-found" => SourceClassification.NativeArtifactsFound,
		"hybrid-page" => SourceClassification.HybridPage,
		"declared-no-history" => SourceClassification.DeclaredNoHistory,
		"outside-cutoff" => SourceClassification.OutsideCutoff,
		"already-live" => SourceClassification.AlreadyLive,
		// source-unresolved is deliberately not seedable: it is the census's own conclusion
		// for products nobody mapped, never something an operator writes by hand.
		_ => null
	};

	private static string Name(TargetScheme scheme) => scheme switch
	{
		TargetScheme.Semver => "semver",
		TargetScheme.Date => "date",
		TargetScheme.Monthly => "monthly",
		_ => scheme.ToString()
	};

	private static string Name(SourceClassification classification) => classification switch
	{
		SourceClassification.PublishedHistoryFound => "published-history-found",
		SourceClassification.NativeArtifactsFound => "native-artifacts-found",
		SourceClassification.HybridPage => "hybrid-page",
		SourceClassification.DeclaredNoHistory => "declared-no-history",
		SourceClassification.SourceUnresolved => "source-unresolved",
		SourceClassification.OutsideCutoff => "outside-cutoff",
		SourceClassification.AlreadyLive => "already-live",
		_ => classification.ToString()
	};

	private void LogSummary(IReadOnlyList<InventorySource> sources, string outputPath, string hash)
	{
		var byClassification = sources
			.GroupBy(s => s.Classification)
			.OrderBy(g => g.Key)
			.Select(g => $"{Name(g.Key)}: {g.Count()}");
		_logger.LogInformation("Census: {SourceCount} sources ({Breakdown})", sources.Count, string.Join(", ", byClassification));
		_logger.LogInformation("Inventory written to {OutputPath} ({Hash})", outputPath, hash);
	}
}
