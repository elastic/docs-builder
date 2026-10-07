// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using DotNet.Globbing;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using DocumentationSetReport = Nullean.Entities.ConstraintReport<Elastic.Documentation.DocumentationSets.DocumentationSetInCatalog>;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// Joins a <see cref="DocumentationSetDefinition"/> against the catalog into a <see cref="ResolvedDocumentationSet"/>.
/// Every <see cref="DocumentationSetConstraints"/> violation is reported; resolution then keeps what satisfies the
/// rules and drops the rest: an item a rule reported as its evidence is dropped, so the loader never re-decides what a rule decided.
/// </summary>
internal static class DocumentationSetResolver
{
	internal static Result<ResolvedDocumentationSet> Resolve(
		DocumentationSetDefinition docset,
		VersioningSystemCatalog versionsConfig,
		ProductCatalog productsConfig,
		IEnvironmentVariables environment
	)
	{
		var report = DocumentationSetConstraints.All.Report(new DocumentationSetInCatalog(docset, productsConfig));
		var diagnostics = report.ToDiagnostics(docset.SourceFile.FullName).ToList();

		// Colliding substitution keys leave no substitution table to build from; the load fails, as it always has.
		if (report.Of(DocumentationSetConstraints.SubstitutionKeysUnique).Count > 0)
			return Result.Failure<ResolvedDocumentationSet>(diagnostics);

		try
		{
			return Result.Success(Map(docset, versionsConfig, productsConfig, environment, report), diagnostics);
		}
		catch (Exception e)
		{
			diagnostics.Add(new Diagnostic
			{
				Severity = Severity.Error,
				File = docset.SourceFile.FullName,
				Message = $"Could not load docset.yml: {e.Message}"
			});
			return Result.Failure<ResolvedDocumentationSet>(diagnostics);
		}
	}

	/// <summary>Resolves an already-parsed <c>docset.yml</c>, reading its redirects: for tests that start from the file's content.</summary>
	internal static Result<ResolvedDocumentationSet> Resolve(
		DocumentationSetDto dto,
		IDocumentationSetContext context,
		VersioningSystemCatalog versionsConfig,
		ProductCatalog productsConfig
	)
	{
		var buffer = new BufferingDiagnosticsCollector();
		IReadOnlyDictionary<string, LinkRedirect>? redirects = null;
		RedirectFile? redirectFile = null;
		if (context.ConfigurationPath.Exists)
		{
			try
			{
				redirectFile = new RedirectFile(context.ConfigurationPath, buffer);
				redirects = redirectFile.Redirects;
			}
			catch (Exception)
			{
				return Result.Failure<ResolvedDocumentationSet>(buffer.Diagnostics);
			}
		}

		var docset = DocumentationSetLoader.ToDefinition(
			dto,
			context.ConfigurationPath,
			context.DocumentationSourceDirectory,
			context.ReadFileSystem,
			redirects,
			redirectFile
		);
		var resolved = Resolve(docset, versionsConfig, productsConfig, context.Environment);
		return new Result<ResolvedDocumentationSet>(resolved.Value, [.. buffer.Diagnostics, .. resolved.Diagnostics]);
	}

	private static ResolvedDocumentationSet Map(
		DocumentationSetDefinition docset,
		VersioningSystemCatalog versionsConfig,
		ProductCatalog productsConfig,
		IEnvironmentVariables environment,
		DocumentationSetReport report
	)
	{
		var empty = new ResolvedDocumentationSet { SourceFile = docset.SourceFile, ScopeDirectory = docset.SourceFile.Directory! };
		if (!docset.SourceFile.Exists)
			return empty with { Project = "unknown" };

		// Glob patterns are parsed here so an invalid one fails the load, as it always has; matching lives in IsExcluded.
		var excludeGlobs = docset.Exclude.Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith('!')).ToArray();
		_ = excludeGlobs.Select(Glob.Parse).ToArray();
		var exclusions = new DocumentationSetExclusions(
			excludeGlobs,
			[.. docset.Exclude.Where(s => !string.IsNullOrEmpty(s) && s.StartsWith('!')).Select(s => s.TrimStart('!'))],
			docset.FolderExcludedFiles
		);

		// What a rule rejected, by the rule itself, is what resolution drops.
		var registry = DocumentationSetRules.ParseRegistry(docset.Registry);
		var crossLinkEntries = report
			.Keep(
				docset.CrossLinks.Where(raw => !string.IsNullOrWhiteSpace(raw)),
				DocumentationSetConstraints.CrossLinkWellFormed,
				raw => raw.Trim()
			)
			.Select(raw => CrossLinkEntryOf(raw.Trim(), registry))
			.ToArray();

		var releaseNotesProducts = report
			.Keep(docset.ReleaseNotesProducts, DocumentationSetConstraints.ReleaseNotesProductKnown)
			.OfType<string>()
			.Select(p => productsConfig.Products[ProductNames.Normalize(p)].Id)
			.Distinct(StringComparer.Ordinal)
			.ToArray();

		var substitutions = docset.Substitutions.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

		var rejectedApis = report.Rejected(DocumentationSetConstraints.ApiEntryValid);
		var apiConfigurations = new Dictionary<string, ResolvedApiConfiguration>(StringComparer.OrdinalIgnoreCase);
		foreach (var api in report.Keep(docset.Api, DocumentationSetConstraints.ApiEntryValid, api => new RejectedApi(api.ProductKey)))
			apiConfigurations[api.ProductKey] = ResolveApi(api, docset, productsConfig, rejectedApis);

		string? storybookRegistry = null;
		string? storybookRegistryFallback = null;
		if (docset.HasStorybook)
		{
			var interpolated = EnvironmentInterpolation.Interpolate(docset.StorybookRegistry?.Trim(), environment);
			storybookRegistry = interpolated.Value;
			storybookRegistryFallback = interpolated.Fallback;
		}

		// Unknown product ids are dropped silently, as they always have been
		var products = docset
			.Products
			.Select(id => productsConfig.Products.GetValueOrDefault(ProductNames.Normalize(id)))
			.OfType<Product>()
			.ToHashSet();

		var features = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
		if (docset.Features.PrimaryNav is { } primaryNav)
			features["primary-nav"] = primaryNav;
		if (docset.Features.DisableGithubEditLink is { } disableEditLink)
			features["disable-github-edit-link"] = disableEditLink;
		if (docset.Features.GuideNav is { } guideNav)
			features["guide-nav"] = guideNav;

		AddVersionSubstitutions(substitutions, versionsConfig);
		AddProductSubstitutions(substitutions, productsConfig);

		return empty with
		{
			Project = docset.Project,
			MaxTocDepth = docset.MaxTocDepth,
			DevelopmentDocs = docset.DevDocs,
			Exclusions = exclusions,
			Registry = registry,
			CrossLinkEntries = crossLinkEntries,
			CrossLinkRepositories = [.. crossLinkEntries.Select(e => e.Repository)],
			ReleaseNotesProducts = releaseNotesProducts,
			Extensions = new EnabledExtensions(docset.Extensions),
			Redirects = docset.Redirects,
			Products = products,
			Substitutions = substitutions,
			FeatureToggles = features,
			StorybookRegistry = storybookRegistry,
			StorybookRegistryFallback = storybookRegistryFallback,
			ApiConfigurations = apiConfigurations.Count > 0 ? apiConfigurations : null,
			SuppressDiagnostics = docset.SuppressDiagnostics,
			Branding = docset.Branding is { } branding
				? ResolveBranding(branding, docset, report.Rejected(DocumentationSetConstraints.BrandingImagesValid))
				: null,
			Ctas = ResolveCtas(report.Keep(docset.Ctas, DocumentationSetConstraints.CtaTemplateValid)),
			TocDefaultCtas = docset.TocDefaultCtas.ToFrozenDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
			TableOfContents = docset.TableOfContents
		};
	}

	private static CrossLinkEntry CrossLinkEntryOf(string raw, DocSetRegistry docsetRegistry)
	{
		var colonSlash = raw.IndexOf("://", StringComparison.Ordinal);
		return colonSlash < 0
			? new CrossLinkEntry(raw, docsetRegistry)
			: new CrossLinkEntry(
				raw[(colonSlash + 3)..],
				DocSetRegistryExtensions.TryParse(raw[..colonSlash], out var registry, true) ? registry : docsetRegistry
			);
	}

	// The built-in default, overlaid by every cta the rules accept, by case-insensitive name.
	private static Dictionary<string, Cta> ResolveCtas(IEnumerable<CtaDeclaration> accepted)
	{
		var ctas = new Dictionary<string, Cta>(StringComparer.OrdinalIgnoreCase) { [Cta.DefaultName] = Cta.Default };
		foreach (var declared in accepted)
			ctas[declared.Name] = new Cta
			{
				Name = declared.Name,
				Label = declared.Label!,
				Url = declared.Url!.Trim(),
				Benefits = declared.Benefits
			};
		return ctas;
	}

	private static ResolvedApiConfiguration ResolveApi(
		ApiDeclaration api,
		DocumentationSetDefinition docset,
		ProductCatalog catalog,
		IReadOnlySet<ApiRejection> rejected
	)
	{
		var productKey = api.ProductKey;
		var entry = api.Entries[0];
		var sourceDirectory = docset.SourceDirectory;
		var specFile = docset.FileSystem.FileInfo.New(Path.GetFullPath(Path.Join(sourceDirectory.FullName, entry.Spec)));
		var apiContentDirectory = docset.FileSystem.DirectoryInfo.New(Path.Join(sourceDirectory.FullName, "api", productKey));
		var seen = entry.CatalogCategories.Select(ApiCatalogCategory.Normalize).OfType<string>().ToHashSet(StringComparer.Ordinal);
		return new ResolvedApiConfiguration
		{
			ProductKey = productKey,
			Product = catalog.Products[ProductNames.Normalize(entry.Product)],
			SpecFileName = Path.GetFileName(entry.Spec!.Trim()),
			LocalSpecFile = specFile.Exists ? specFile : null,
			Repository = string.IsNullOrWhiteSpace(entry.Repository) ? null : entry.Repository.Trim(),
			Children =
			[
				.. entry
					.Children
					.OfType<string>()
					.Where(c => !string.IsNullOrWhiteSpace(c) && !rejected.Contains(new RejectedApiChild(productKey, c)))
					.Select(c => docset.FileSystem.FileInfo.New(Path.GetFullPath(Path.Join(apiContentDirectory.FullName, c))))
			],
			ApiContentDirectory = apiContentDirectory,
			CatalogCategories = [.. ApiCatalogCategory.DisplayOrder.Where(seen.Contains)]
		};
	}

	private static Branding ResolveBranding(
		BrandingDeclaration branding,
		DocumentationSetDefinition docset,
		IReadOnlySet<DeclaredBrandingImage> rejected
	)
	{
		string? Valid(string? path, string field) =>
			string.IsNullOrEmpty(path) || rejected.Contains(new DeclaredBrandingImage(field, path)) ? null : path;

		return new()
		{
			Icon = Valid(branding.Icon, "branding.icon"),
			HeaderBg = branding.HeaderBg,
			OgImage = Valid(branding.OgImage, "branding.og-image"),
			Favicon = string.IsNullOrEmpty(branding.Favicon)
				? DiscoverBrandingFile(["favicon.ico", "favicon.png", "favicon.svg"], docset)
				: Valid(branding.Favicon, "branding.favicon"),
			AppleTouchIcon = string.IsNullOrEmpty(branding.AppleTouchIcon)
				? DiscoverBrandingFile(["apple-touch-icon.png"], docset)
				: Valid(branding.AppleTouchIcon, "branding.apple-touch-icon")
		};
	}

	private static string? DiscoverBrandingFile(string[] candidates, DocumentationSetDefinition docset)
	{
		foreach (var name in candidates)
		{
			var f = docset.FileSystem.FileInfo.New(Path.Join(docset.SourceDirectory.FullName, name));
			if (f.Exists && f.LinkTarget is null)
				return name;
		}
		return null;
	}

	private static void AddVersionSubstitutions(Dictionary<string, string> substitutions, VersioningSystemCatalog versionsConfig)
	{
		foreach (var (id, system) in versionsConfig.VersioningSystems)
		{
			var name = id.ToStringFast(true);
			var alternativeName = name.Replace('-', '_');
			substitutions[$"version.{name}"] = system.Current;
			substitutions[$"version.{alternativeName}"] = system.Current;
			substitutions[$"version.{name}.base"] = system.Base;
			substitutions[$"version.{alternativeName}.base"] = system.Base;
		}
	}

	// Only products with the public-reference feature get substitutions
	private static void AddProductSubstitutions(Dictionary<string, string> substitutions, ProductCatalog productsConfig)
	{
		foreach (var product in productsConfig.PublicReferenceProducts.Values)
		{
			var alternativeProductId = product.Id.Replace('-', '_');
			substitutions[$"product.{product.Id}"] = product.DisplayName;
			substitutions[$".{product.Id}"] = product.DisplayName;
			substitutions[$"product.{alternativeProductId}"] = product.DisplayName;
			substitutions[$".{alternativeProductId}"] = product.DisplayName;
		}
	}
}
