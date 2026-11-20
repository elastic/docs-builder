// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using DotNet.Globbing;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Extensions;
using static Elastic.Documentation.SymlinkValidator;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// Joins a <see cref="DocumentationSetDefinition"/> against the catalog and the files next to it into a <see cref="ResolvedDocumentationSet"/>.
/// Findings are reported to the context as they are made, in the order they always were, and an exception propagates after the
/// <c>Could not load docset.yml:</c> error: the code moved here out of <c>ConfigurationFile</c> unchanged.
/// </summary>
internal static class DocumentationSetResolver
{
	/// <summary>The resolved set with the <c>exclude</c> globs parsed while it was resolved, so matching does not parse them again.</summary>
	internal sealed record Resolution(ResolvedDocumentationSet Set, Glob[] ExcludeGlobs);

	/// <summary>The set of a <c>docset.yml</c> that is not there.</summary>
	internal static Resolution Missing(IDocumentationSetContext context)
	{
		context.EmitWarning(context.ConfigurationPath, "No configuration file found");
		return new Resolution(Empty(context) with { Project = "unknown" }, []);
	}

	internal static Resolution Resolve(
		DocumentationSetDefinition docSetFile,
		IDocumentationSetContext context,
		VersioningSystemCatalog versionsConfig,
		ProductCatalog productsConfig
	)
	{
		try
		{
			return Map(docSetFile, context, versionsConfig, productsConfig);
		}
		catch (Exception e)
		{
			context.EmitError(context.ConfigurationPath, $"Could not load docset.yml: {e.Message}");
			throw;
		}
	}

	private static ResolvedDocumentationSet Empty(IDocumentationSetContext context) =>
		new() { SourceFile = context.ConfigurationPath, ScopeDirectory = context.ConfigurationPath.Directory! };

	private static Resolution Map(
		DocumentationSetDefinition docSetFile,
		IDocumentationSetContext context,
		VersioningSystemCatalog versionsConfig,
		ProductCatalog productsConfig
	)
	{
		// Glob patterns are parsed here so an invalid one fails the load, as it always has; matching lives in ConfigurationFile.IsExcluded.
		var excludeGlobs = docSetFile.Exclude.Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith('!')).ToArray();
		var parsedGlobs = excludeGlobs.Select(Glob.Parse).ToArray();
		var exclusions = new DocumentationSetExclusions(
			excludeGlobs,
			[.. docSetFile.Exclude.Where(s => !string.IsNullOrEmpty(s) && s.StartsWith('!')).Select(s => s.TrimStart('!'))],
			docSetFile.FolderExcludedFiles
		);

		var registry = DocumentationSetRules.ParseRegistry(docSetFile.Registry);

		// cross_links and release_notes are reported together, in that order; what the rules reject is dropped
		Report(new DocumentationSetInCatalog(docSetFile, productsConfig), context);
		var crossLinkEntries = docSetFile
			.CrossLinks
			.Where(raw => !string.IsNullOrWhiteSpace(raw))
			.Select(raw => DocumentationSetRules.ResolveCrossLink(raw.Trim(), registry).Value)
			.OfType<CrossLinkAccepted>()
			.Select(accepted => accepted.Entry)
			.ToArray();

		// a release_notes entry with no value fails here
		var releaseNotesProducts = new List<string>();
		foreach (var reference in docSetFile.ReleaseNotes)
		{
			if (DocumentationSetRules.ResolveReleaseNotes(reference, productsConfig).Value is ReleaseNotesAccepted accepted)
				releaseNotesProducts.Add(accepted.ProductId);
		}

		// an extensions key with no value fails here, before the substitutions
		var extensions = new EnabledExtensions(docSetFile.Extensions);

		var substitutions = new Dictionary<string, string>(
			(IDictionary<string, string>)docSetFile.Substitutions,
			StringComparer.OrdinalIgnoreCase
		);

		// api and storybook are reported together, in that order
		Report(new DocumentationSetApis(docSetFile, productsConfig, context.ReadFileSystem, context.DocumentationSourceDirectory), context);
		Dictionary<string, ResolvedApiConfiguration>? apiConfigurations = null;
		if (docSetFile.Api.Count > 0)
		{
			var apiConfigs = new Dictionary<string, ResolvedApiConfiguration>(StringComparer.OrdinalIgnoreCase);
			foreach (var api in docSetFile.Api)
			{
				var resolution = DocumentationSetRules.ResolveApi(
					api,
					productsConfig,
					context.ReadFileSystem,
					context.DocumentationSourceDirectory
				);
				if (resolution.Value is ApiAccepted accepted)
					apiConfigs[api.ProductKey] = accepted.Configuration;
			}

			apiConfigurations = apiConfigs.Count > 0 ? apiConfigs : null;
		}

		string? storybookRegistry = null;
		string? storybookRegistryFallback = null;
		if (docSetFile.HasStorybook)
		{
			// The disallowed variables were reported with the other rules of this stage.
			var interpolated = EnvironmentInterpolation.Interpolate(docSetFile.StorybookRegistry?.Trim(), context.Environment);
			storybookRegistry = interpolated.Value;
			storybookRegistryFallback = interpolated.Fallback;
		}

		// Process products from docset - resolve ProductLinks to Product objects
		HashSet<Product> products = [];
		if (docSetFile.Products.Count > 0)
		{
			products =
				docSetFile
					.Products
					.Select(id => productsConfig.Products.GetValueOrDefault(id.Replace('_', '-')))
					.Where(product => product is not null)
					.ToHashSet()!;
		}

		// branding images and cta templates are reported together, in that order
		Report(new DocumentationSetPresentation(docSetFile, context.ReadFileSystem, context.DocumentationSourceDirectory), context);
		Branding? branding = null;
		if (docSetFile.Branding is not null)
			branding = ResolveBranding(docSetFile.Branding, context);

		// Process CTA templates - overlays onto (and may override) the built-in 'trial' default
		var ctas = new Dictionary<string, Cta>(StringComparer.OrdinalIgnoreCase) { [Cta.DefaultName] = Cta.Default };
		// a cta template with no value fails here
		foreach (var declared in docSetFile.Ctas)
		{
			if (DocumentationSetRules.CtaProblem(declared!) is not null)
				continue;
			ctas[declared!.Name] = new Cta
			{
				Name = declared.Name,
				Label = declared.Label!,
				Url = declared.Url!.Trim(),
				Benefits = declared.Benefits
			};
		}

		// default_cta and primary-nav are reported together, in that order
		Report(new DocumentationSetFeatures(docSetFile), context);

		// Process features
		var features = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
		if (docSetFile.Features.PrimaryNav.HasValue)
			features["primary-nav"] = docSetFile.Features.PrimaryNav.Value;
		if (docSetFile.Features.DisableGithubEditLink.HasValue)
			features["disable-github-edit-link"] = docSetFile.Features.DisableGithubEditLink.Value;
		if (docSetFile.Features.GuideNav.HasValue)
			features["guide-nav"] = docSetFile.Features.GuideNav.Value;

		// Add version substitutions
		foreach (var (id, system) in versionsConfig.VersioningSystems)
		{
			var name = id.ToStringFast(true);
			var alternativeName = name.Replace('-', '_');
			substitutions[$"version.{name}"] = system.Current;
			substitutions[$"version.{alternativeName}"] = system.Current;
			substitutions[$"version.{name}.base"] = system.Base;
			substitutions[$"version.{alternativeName}.base"] = system.Base;
		}

		// Add product substitutions (only for products with public-reference feature)
		foreach (var product in productsConfig.PublicReferenceProducts.Values)
		{
			var alternativeProductId = product.Id.Replace('-', '_');
			substitutions[$"product.{product.Id}"] = product.DisplayName;
			substitutions[$".{product.Id}"] = product.DisplayName;
			substitutions[$"product.{alternativeProductId}"] = product.DisplayName;
			substitutions[$".{alternativeProductId}"] = product.DisplayName;
		}

		var set = Empty(context) with
		{
			Project = docSetFile.Project,
			MaxTocDepth = docSetFile.MaxTocDepth,
			DevelopmentDocs = docSetFile.DevDocs,
			Exclusions = exclusions,
			Registry = registry,
			CrossLinkEntries = crossLinkEntries,
			CrossLinkRepositories = [.. crossLinkEntries.Select(e => e.Repository)],
			ReleaseNotesProducts = [.. releaseNotesProducts.Distinct(StringComparer.Ordinal)],
			Extensions = extensions,
			Products = products,
			Substitutions = substitutions,
			FeatureToggles = features,
			StorybookRegistry = storybookRegistry,
			StorybookRegistryFallback = storybookRegistryFallback,
			ApiConfigurations = apiConfigurations,
			SuppressDiagnostics = docSetFile.SuppressDiagnostics.ToHashSet(),
			Branding = branding,
			Ctas = ctas,
			TocDefaultCtas = docSetFile.TocDefaultCtas
		};
		return new Resolution(set, parsedGlobs);
	}

	/// <summary>Writes the findings of one stage of rules to the collector as they are made.</summary>
	private static void Report<T>(T stage, IDocumentationSetContext context) where T : class, Nullean.Entities.IHaveConstraints<T>
	{
		foreach (var finding in stage.Check(context.ConfigurationPath.FullName).Findings)
			context.Collector.Write(finding);
	}

	private static readonly string[] FaviconCandidates = ["favicon.ico", "favicon.png", "favicon.svg"];

	// The images a rule found wrong were reported with the stage's other findings and are dropped here; an unset favicon and apple-touch-icon are discovered.
	private static Branding ResolveBranding(BrandingDeclaration declared, IDocumentationSetContext context)
	{
		var icon = UsableBrandingImage(declared.Icon, "branding.icon", context);
		var ogImage = UsableBrandingImage(declared.OgImage, "branding.og-image", context);
		var favicon = string.IsNullOrEmpty(declared.Favicon)
			? DiscoverBrandingFile(FaviconCandidates, context)
			: UsableBrandingImage(declared.Favicon, "branding.favicon", context);
		var appleTouchIcon = string.IsNullOrEmpty(declared.AppleTouchIcon)
			? DiscoverBrandingFile(["apple-touch-icon.png"], context)
			: UsableBrandingImage(declared.AppleTouchIcon, "branding.apple-touch-icon", context);
		return new Branding
		{
			Icon = icon,
			HeaderBg = declared.HeaderBg,
			OgImage = ogImage,
			Favicon = favicon,
			AppleTouchIcon = appleTouchIcon,
			Copyright = declared.Copyright
		};
	}

	private static string? UsableBrandingImage(string? imagePath, string field, IDocumentationSetContext context) =>
		string.IsNullOrEmpty(imagePath)
			|| DocumentationSetRules.BrandingImageProblem(
				field,
				imagePath,
				context.ReadFileSystem,
				context.DocumentationSourceDirectory
			) is not null ? null : imagePath;

	private static string? DiscoverBrandingFile(string[] candidates, IDocumentationSetContext context)
	{
		foreach (var name in candidates)
		{
			var f = context.ReadFileSystem.FileInfo.New(Path.Join(context.DocumentationSourceDirectory.FullName, name));
			if (f.Exists && f.LinkTarget is null)
				return name;
		}
		return null;
	}
}
