// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using DotNet.Globbing;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// Joins a <see cref="DocumentationSetDefinition"/> against the catalog into a <see cref="ResolvedDocumentationSet"/>.
/// Every <see cref="DocumentationSetConstraints"/> violation is reported; resolution then keeps what satisfies the
/// rules and drops the rest, deciding item by item with the same <see cref="DocumentationSetRules"/>.
/// </summary>
public static class DocumentationSetResolver
{
	public static Result<ResolvedDocumentationSet> Resolve(
		DocumentationSetDefinition docset,
		VersioningSystemCatalog versionsConfig,
		ProductCatalog productsConfig,
		IEnvironmentVariables environment
	)
	{
		var violations = DocumentationSetConstraints.Check(docset, productsConfig);
		var diagnostics = violations.Select(v => v.ToDiagnostic(docset.SourceFile.FullName)).ToList();

		// Colliding substitution keys leave no substitution table to build from; the load fails, as it always has.
		if (violations.Any(v => v.ConstraintId == "substitution-keys-unique"))
			return Result.Failure<ResolvedDocumentationSet>(diagnostics);

		try
		{
			return Result.Success(Map(docset, versionsConfig, productsConfig, environment), diagnostics);
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
		if (context.ConfigurationPath.Exists)
		{
			try
			{
				redirects = new RedirectFile(context.ConfigurationPath, buffer).Redirects;
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
			redirects
		);
		var resolved = Resolve(docset, versionsConfig, productsConfig, context.Environment);
		return new Result<ResolvedDocumentationSet>(resolved.Value, [.. buffer.Diagnostics, .. resolved.Diagnostics]);
	}

	private static ResolvedDocumentationSet Map(
		DocumentationSetDefinition docset,
		VersioningSystemCatalog versionsConfig,
		ProductCatalog productsConfig,
		IEnvironmentVariables environment
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

		var registry = DocumentationSetRules.ParseRegistry(docset.Registry);
		var crossLinkEntries = DocumentationSetRules
			.DeclaredCrossLinks(docset)
			.Select(raw => DocumentationSetRules.ParseCrossLink(raw, registry).Entry)
			.OfType<CrossLinkEntry>()
			.ToArray();

		var releaseNotesProducts = docset
			.ReleaseNotesProducts
			.Select(p => DocumentationSetRules.ResolveReleaseNotesProduct(p, productsConfig).ProductId)
			.OfType<string>()
			.Distinct(StringComparer.Ordinal)
			.ToArray();

		var substitutions = docset.Substitutions.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

		var apiConfigurations = new Dictionary<string, ResolvedApiConfiguration>(StringComparer.OrdinalIgnoreCase);
		foreach (var api in docset.Api)
		{
			if (DocumentationSetRules.ResolveApi(api, docset, productsConfig).Api is { } resolved)
				apiConfigurations[api.ProductKey] = resolved;
		}

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
			.Select(id => productsConfig.Products.GetValueOrDefault(id.Replace('_', '-')))
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
			Branding = docset.Branding is { } branding ? ResolveBranding(branding, docset) : null,
			Ctas = DocumentationSetRules.ValidCtas(docset),
			TocDefaultCtas = docset.TocDefaultCtas.ToFrozenDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
			TableOfContents = docset.TableOfContents
		};
	}

	private static Branding ResolveBranding(BrandingDeclaration branding, DocumentationSetDefinition docset) =>
		new()
		{
			Icon = DocumentationSetRules.ValidateBrandingImage(branding.Icon, "branding.icon", docset).Path,
			HeaderBg = branding.HeaderBg,
			OgImage = DocumentationSetRules.ValidateBrandingImage(branding.OgImage, "branding.og-image", docset).Path,
			Favicon = string.IsNullOrEmpty(branding.Favicon)
				? DiscoverBrandingFile(["favicon.ico", "favicon.png", "favicon.svg"], docset)
				: DocumentationSetRules.ValidateBrandingImage(branding.Favicon, "branding.favicon", docset).Path,
			AppleTouchIcon = string.IsNullOrEmpty(branding.AppleTouchIcon)
				? DiscoverBrandingFile(["apple-touch-icon.png"], docset)
				: DocumentationSetRules.ValidateBrandingImage(branding.AppleTouchIcon, "branding.apple-touch-icon", docset).Path
		};

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
