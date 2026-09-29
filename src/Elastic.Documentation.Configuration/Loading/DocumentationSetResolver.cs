// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using DotNet.Globbing;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Suggestions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Links;
using Elastic.Documentation.Publishing;
using static Elastic.Documentation.SymlinkValidator;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>Where a documentation set is read from, and the environment its env-dependent values resolve against.</summary>
/// <param name="ConfigurationPath">The <c>docset.yml</c> (or <c>_docset.yml</c>).</param>
/// <param name="SourceDirectory">The documentation source directory that docset-relative paths resolve from.</param>
public sealed record DocumentationSetSource(
	IFileInfo ConfigurationPath,
	IDirectoryInfo SourceDirectory,
	IDocumentationFileSystem ReadFileSystem,
	IEnvironmentVariables Environment
);

/// <summary>
/// Validates a loaded <c>docset.yml</c> and joins it against the catalog into a <see cref="ResolvedDocumentationSet"/>.
/// Reads <c>redirects.yml</c>, and checks branding images and API spec paths on disk. Diagnostics come back in the
/// order they are found, which is the order users have always seen them in.
/// </summary>
public static class DocumentationSetResolver
{
	public static Result<ResolvedDocumentationSet> Resolve(
		DocumentationSetFile docSetFile,
		IDocumentationSetContext context,
		VersioningSystemCatalog versionsConfig,
		ProductCatalog productsConfig
	) =>
		Resolve(
			docSetFile,
			new DocumentationSetSource(
				context.ConfigurationPath,
				context.DocumentationSourceDirectory,
				context.ReadFileSystem,
				context.Environment
			),
			versionsConfig,
			productsConfig
		);

	public static Result<ResolvedDocumentationSet> Resolve(
		DocumentationSetFile docSetFile,
		DocumentationSetSource source,
		VersioningSystemCatalog versionsConfig,
		ProductCatalog productsConfig
	)
	{
		var context = new ResolutionContext(source);
		var resolved = ResolveInto(docSetFile, context, versionsConfig, productsConfig);
		return new Result<ResolvedDocumentationSet>(resolved, context.Buffer.Diagnostics);
	}

	private static ResolvedDocumentationSet? ResolveInto(
		DocumentationSetFile docSetFile,
		ResolutionContext context,
		VersioningSystemCatalog versionsConfig,
		ProductCatalog productsConfig
	)
	{
		var empty = new ResolvedDocumentationSet
		{
			SourceFile = context.ConfigurationPath,
			ScopeDirectory = context.ConfigurationPath.Directory!
		};
		if (!context.ConfigurationPath.Exists)
		{
			context.EmitWarning(context.ConfigurationPath, "No configuration file found");
			return empty with { Project = "unknown" };
		}

		Dictionary<string, LinkRedirect>? redirects;
		try
		{
			redirects = new RedirectFile(context).Redirects;
		}
		catch (Exception)
		{
			// RedirectFile has already reported the failure as an error.
			return null;
		}

		try
		{
			// Glob patterns are parsed here so an invalid one fails the load, as it always has; matching lives in IsExcluded.
			var excludeGlobs = docSetFile.Exclude.Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith('!')).ToArray();
			_ = excludeGlobs.Select(Glob.Parse).ToArray();
			var exclusions = new DocumentationSetExclusions(
				excludeGlobs,
				[.. docSetFile.Exclude.Where(s => !string.IsNullOrEmpty(s) && s.StartsWith('!')).Select(s => s.TrimStart('!'))],
				docSetFile.FolderExcludedFiles
			);

			// Parse registry (null/empty/"public" -> Public)
			var registry = DocSetRegistry.Public;
			if (
				!string.IsNullOrWhiteSpace(docSetFile.Registry)
				&& DocSetRegistryExtensions.TryParse(docSetFile.Registry.Trim(), out var parsedRegistry, true)
			)
				registry = parsedRegistry;

			// Parse cross-link entries with optional registry prefix (e.g. public://elasticsearch)
			var crossLinkEntries = docSetFile
				.CrossLinks
				.Where(raw => !string.IsNullOrWhiteSpace(raw))
				.Select(raw => ParseCrossLinkEntry(raw.Trim(), registry, context.ConfigurationPath, context))
				.Where(entry => entry is not null)
				.Select(entry => entry!)
				.ToArray();

			// Parse and validate CDN-backed release-notes product declarations
			var releaseNotesProducts = ParseReleaseNotesProducts(docSetFile.ReleaseNotes, productsConfig, context);

			var substitutions = new Dictionary<string, string>(docSetFile.Subs, StringComparer.OrdinalIgnoreCase);

			// Process API configurations
			Dictionary<string, ResolvedApiConfiguration>? apiConfigurations = null;
			if (docSetFile.Api.Count > 0)
			{
				var apiConfigs = new Dictionary<string, ResolvedApiConfiguration>(StringComparer.OrdinalIgnoreCase);

				foreach (var (productKey, apiSequence) in docSetFile.Api)
				{
					if (ResolveApiEntry(productKey, apiSequence, productsConfig, context) is { } resolvedConfig)
						apiConfigs[productKey] = resolvedConfig;
				}

				apiConfigurations = apiConfigs.Count > 0 ? apiConfigs : null;
			}

			string? storybookRegistry = null;
			string? storybookRegistryFallback = null;
			if (docSetFile.Storybook is not null)
			{
				var interpolated = EnvironmentInterpolation.Interpolate(
					docSetFile.Storybook.Registry?.Trim(),
					context.Environment,
					name => context.EmitWarning(
						context.ConfigurationPath,
						$"'storybook.registry' references environment variable '{name}' which is not allow-listed for interpolation and is left literal. Allowed: {string.Join(", ", EnvironmentInterpolation.AllowedVariables)}."
					)
				);
				storybookRegistry = interpolated.Value;
				storybookRegistryFallback = interpolated.Fallback;
			}

			// Resolve ProductLinks to Product objects; unknown ids are dropped silently, as they always have been
			var products = new HashSet<Product>();
			if (docSetFile.Products.Count > 0)
			{
				products =
					docSetFile
						.Products
						.Select(link => productsConfig.Products.GetValueOrDefault(link.Id.Replace('_', '-')))
						.Where(product => product is not null)
						.ToHashSet()!;
			}

			Branding? branding = null;
			if (docSetFile.Branding is not null)
				branding = ValidateBranding(docSetFile.Branding, context);

			// CTA templates overlay onto (and may override) the built-in 'trial' default
			var ctas = new Dictionary<string, Cta>(StringComparer.OrdinalIgnoreCase) { [Cta.DefaultName] = Cta.Default };
			foreach (var (name, definition) in docSetFile.Cta)
			{
				if (ValidateCta(name, definition, context) is not { } cta)
					continue;
				ctas[name] = cta;
			}

			foreach (var (pagePath, ctaName) in docSetFile.TocDefaultCtas)
			{
				if (!ctas.ContainsKey(ctaName))
				{
					context.EmitError(
						context.ConfigurationPath,
						$"'default_cta: {ctaName}' on page '{pagePath}' does not match any 'cta' template in docset.yml."
					);
				}
			}

			var features = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
			if (docSetFile.Features.PrimaryNav.HasValue)
				features["primary-nav"] = docSetFile.Features.PrimaryNav.Value;
			if (docSetFile.Features.DisableGithubEditLink.HasValue)
				features["disable-github-edit-link"] = docSetFile.Features.DisableGithubEditLink.Value;
			if (docSetFile.Features.GuideNav.HasValue)
				features["guide-nav"] = docSetFile.Features.GuideNav.Value;

			// primary-nav requires the Elastic global navigation which is not available for white-label builds
			if (branding is not null && docSetFile.Features.PrimaryNav is true)
				context.EmitError(
					context.ConfigurationPath,
					"'features.primary-nav' cannot be used together with 'branding': the primary nav requires Elastic global navigation."
				);

			AddVersionSubstitutions(substitutions, versionsConfig);
			AddProductSubstitutions(substitutions, productsConfig);

			return empty with
			{
				Project = docSetFile.Project,
				MaxTocDepth = docSetFile.MaxTocDepth,
				DevelopmentDocs = docSetFile.DevDocs,
				Exclusions = exclusions,
				Registry = registry,
				CrossLinkEntries = crossLinkEntries,
				CrossLinkRepositories = [.. crossLinkEntries.Select(e => e.Repository)],
				ReleaseNotesProducts = releaseNotesProducts,
				Extensions = new EnabledExtensions(docSetFile.Extensions),
				Redirects = redirects,
				Products = products,
				Substitutions = substitutions,
				FeatureToggles = features,
				StorybookRegistry = storybookRegistry,
				StorybookRegistryFallback = storybookRegistryFallback,
				ApiConfigurations = apiConfigurations,
				SuppressDiagnostics = docSetFile.SuppressDiagnostics,
				Branding = branding,
				Ctas = ctas,
				TocDefaultCtas = docSetFile.TocDefaultCtas,
				TableOfContents = docSetFile.TableOfContents
			};
		}
		catch (Exception e)
		{
			context.EmitError(context.ConfigurationPath, $"Could not load docset.yml: {e.Message}");
			return null;
		}
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

	private static Cta? ValidateCta(string name, CtaDefinition definition, IDocumentationSetContext context)
	{
		if (string.IsNullOrWhiteSpace(definition.Button?.Label) || string.IsNullOrWhiteSpace(definition.Button?.Url))
		{
			context.EmitError(context.ConfigurationPath, $"'cta.{name}' must define both 'button.label' and 'button.url'.");
			return null;
		}
		var url = definition.Button.Url.Trim();
		if (
			Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri)
			&& uri.IsAbsoluteUri
			&& uri.Scheme != Uri.UriSchemeHttp
			&& uri.Scheme != Uri.UriSchemeHttps
		)
		{
			context.EmitError(context.ConfigurationPath, $"'cta.{name}.button.url' must use http/https or a relative URL.");
			return null;
		}
		if (definition.Benefits.Count > Cta.MaxBenefits)
		{
			context.EmitError(
				context.ConfigurationPath,
				$"'cta.{name}.benefits' has {definition.Benefits.Count} entries; a maximum of {Cta.MaxBenefits} is allowed."
			);
			return null;
		}
		return new Cta { Name = name, Label = definition.Button.Label, Url = url, Benefits = definition.Benefits };
	}

	private static readonly HashSet<string> AllowedImageExtensions = [".svg", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico"];

	private static Branding ValidateBranding(BrandingConfiguration branding, IDocumentationSetContext context)
	{
		var icon = ValidateBrandingImage(branding.Icon, "branding.icon", context);
		var ogImage = ValidateBrandingImage(branding.OgImage, "branding.og-image", context);
		var favicon = string.IsNullOrEmpty(branding.Favicon)
			? DiscoverBrandingFile(["favicon.ico", "favicon.png", "favicon.svg"], context)
			: ValidateBrandingImage(branding.Favicon, "branding.favicon", context);
		var appleTouchIcon = string.IsNullOrEmpty(branding.AppleTouchIcon)
			? DiscoverBrandingFile(["apple-touch-icon.png"], context)
			: ValidateBrandingImage(branding.AppleTouchIcon, "branding.apple-touch-icon", context);
		return new Branding
		{
			Icon = icon,
			HeaderBg = branding.HeaderBg,
			OgImage = ogImage,
			Favicon = favicon,
			AppleTouchIcon = appleTouchIcon
		};
	}

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

	private static string? ValidateBrandingImage(string? imagePath, string fieldName, IDocumentationSetContext context)
	{
		if (string.IsNullOrEmpty(imagePath))
			return null;

		var ext = Path.GetExtension(imagePath).ToLowerInvariant();
		if (!AllowedImageExtensions.Contains(ext))
		{
			context.EmitError(
				context.ConfigurationPath,
				$"'{fieldName}' has unsupported extension '{ext}'. Allowed: {string.Join(", ", AllowedImageExtensions)}"
			);
			return null;
		}

		var resolved = context
			.ReadFileSystem
			.FileInfo
			.New(Path.GetFullPath(Path.Join(context.DocumentationSourceDirectory.FullName, imagePath)));

		if (!resolved.IsSubPathOf(context.DocumentationSourceDirectory))
		{
			context.EmitError(context.ConfigurationPath, $"'{fieldName}' path '{imagePath}' escapes the documentation source directory.");
			return null;
		}

		var symlinkError = ValidateFileAccess(resolved, context.DocumentationSourceDirectory);
		if (symlinkError is not null)
		{
			context.EmitError(context.ConfigurationPath, $"'{fieldName}' path '{imagePath}' is unsafe: {symlinkError}");
			return null;
		}

		if (!resolved.Exists)
		{
			context.EmitError(context.ConfigurationPath, $"'{fieldName}' file '{imagePath}' does not exist.");
			return null;
		}

		return imagePath;
	}

	private static string[] ParseReleaseNotesProducts(
		IReadOnlyList<ReleaseNotesProductReference> references,
		ProductCatalog productsConfig,
		IDocumentationSetContext context
	)
	{
		if (references.Count == 0)
			return [];

		var products = new List<string>(references.Count);
		foreach (var reference in references)
		{
			var product = reference.Product?.Trim();
			if (string.IsNullOrEmpty(product))
			{
				context.EmitError(context.ConfigurationPath, "A 'release_notes' entry is missing a 'product' value.");
				continue;
			}

			if (!IsValidProductId(product))
			{
				context.EmitError(
					context.ConfigurationPath,
					$"Invalid 'release_notes' product '{product}'. Product ids must match [a-zA-Z0-9_-]+."
				);
				continue;
			}

			// products.yml keys are hyphenated; accept the underscore variant for parity with `products`.
			var normalized = product.Replace('_', '-');
			if (!productsConfig.Products.TryGetValue(normalized, out var resolved))
			{
				context.EmitError(
					context.ConfigurationPath,
					$"Unknown 'release_notes' product '{product}'. It must be a product id defined in products.yml."
				);
				continue;
			}

			if (!resolved.Features.ParticipatesInReleaseNotes)
			{
				context.EmitError(
					context.ConfigurationPath,
					$"Product '{product}' declared in 'release_notes' does not participate in the release-notes system (it lacks the 'release-notes' feature in products.yml)."
				);
				continue;
			}

			if (!products.Contains(resolved.Id, StringComparer.Ordinal))
				products.Add(resolved.Id);
		}

		return [.. products];
	}

	private static bool IsValidProductId(string product) =>
		product.Length > 0 && product.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');

	private static ResolvedApiConfiguration? ResolveApiEntry(
		string productKey,
		ApiProductSequence apiSequence,
		ProductCatalog productsConfig,
		IDocumentationSetContext context
	)
	{
		if (apiSequence.SingleEntry is not { } entry)
		{
			context.EmitError(
				context.ConfigurationPath,
				$"API configuration for '{productKey}' must have exactly one entry, found {apiSequence.Entries.Count}."
			);
			return null;
		}

		if (!entry.HasProduct)
		{
			context.Collector.Write(new Diagnostic
			{
				Severity = Severity.Error,
				File = context.ConfigurationPath.FullName,
				Line = entry.Line,
				Column = entry.Column,
				Message = $"API '{productKey}' is missing required 'product:'. It must match a product id defined in products.yml."
			});
			return null;
		}

		var normalizedProduct = entry.Product!.Trim().Replace('_', '-');
		if (!productsConfig.Products.TryGetValue(normalizedProduct, out var product))
		{
			var hint = new Suggestion(productsConfig.Products.Keys.ToHashSet(), normalizedProduct).GetSuggestionQuestion();
			context.Collector.Write(new Diagnostic
			{
				Severity = Severity.Error,
				File = context.ConfigurationPath.FullName,
				Line = entry.ProductLine,
				Column = entry.ProductColumn,
				Message =
					$"Unknown 'product: {entry.Product}' for API '{productKey}'. It must be a product id defined in products.yml.{(string.IsNullOrEmpty(hint) ? "" : $" {hint}")}"
			});
			return null;
		}

		if (!entry.HasSpec)
		{
			context.Collector.Write(new Diagnostic
			{
				Severity = Severity.Error,
				File = context.ConfigurationPath.FullName,
				Line = entry.Line,
				Column = entry.Column,
				Message = $"API '{productKey}' is missing required 'spec:'. Its basename is required to resolve " +
					"the remote version index, even when the file is not present locally."
			});
			return null;
		}

		var specFileName = Path.GetFileName(entry.Spec!.Trim());
		if (string.IsNullOrEmpty(specFileName))
		{
			context.Collector.Write(new Diagnostic
			{
				Severity = Severity.Error,
				File = context.ConfigurationPath.FullName,
				Line = entry.SpecLine ?? entry.Line,
				Column = entry.SpecColumn ?? entry.Column,
				Message = $"'spec: {entry.Spec}' for API '{productKey}' does not resolve to a file name."
			});
			return null;
		}

		var fullSpecPath = Path.GetFullPath(Path.Join(context.DocumentationSourceDirectory.FullName, entry.Spec));
		var specFile = context.ReadFileSystem.FileInfo.New(fullSpecPath);
		if (!specFile.IsSubPathOf(context.DocumentationSourceDirectory))
		{
			context.Collector.Write(new Diagnostic
			{
				Severity = Severity.Error,
				File = context.ConfigurationPath.FullName,
				Line = entry.SpecLine ?? entry.Line,
				Column = entry.SpecColumn ?? entry.Column,
				Message = $"'spec: {entry.Spec}' for API '{productKey}' escapes the documentation source directory."
			});
			return null;
		}

		// A missing local file is expected, not an error: docsets that don't carry the spec
		// locally resolve the current version from S3 via the version index instead.
		IFileInfo? localSpecFile = null;
		if (specFile.Exists)
		{
			var symlinkError = ValidateFileAccess(specFile, context.DocumentationSourceDirectory);
			if (symlinkError is not null)
			{
				context.Collector.Write(new Diagnostic
				{
					Severity = Severity.Error,
					File = context.ConfigurationPath.FullName,
					Line = entry.SpecLine ?? entry.Line,
					Column = entry.SpecColumn ?? entry.Column,
					Message = $"'spec: {entry.Spec}' for API '{productKey}' is unsafe: {symlinkError}"
				});
				return null;
			}
			localSpecFile = specFile;
		}

		string? repository = null;
		if (!string.IsNullOrWhiteSpace(entry.Repository))
		{
			var candidate = entry.Repository.Trim();
			var separator = candidate.IndexOf('/');
			var isWellFormed = separator > 0 && separator < candidate.Length - 1;
			if (!isWellFormed)
			{
				context.Collector.Write(new Diagnostic
				{
					Severity = Severity.Error,
					File = context.ConfigurationPath.FullName,
					Line = entry.RepositoryLine ?? entry.Line,
					Column = entry.RepositoryColumn ?? entry.Column,
					Message =
						$"'repository: {entry.Repository}' for API '{productKey}' must be in 'org/repo' form, e.g. 'elastic/elasticsearch-specification'."
				});
				return null;
			}
			repository = candidate;
		}

		var apiContentDirectory = context
			.ReadFileSystem
			.DirectoryInfo
			.New(Path.Join(context.DocumentationSourceDirectory.FullName, "api", productKey));
		var children = ResolveApiChildren(productKey, entry.Children, context, apiContentDirectory);

		return new ResolvedApiConfiguration
		{
			ProductKey = productKey,
			Product = product,
			SpecFileName = specFileName,
			LocalSpecFile = localSpecFile,
			Repository = repository,
			Children = children,
			ApiContentDirectory = apiContentDirectory,
			CatalogCategories = ResolveCatalogCategories(productKey, entry, context)
		};
	}

	private static IReadOnlyList<string> ResolveCatalogCategories(
		string productKey,
		ApiProductEntry entry,
		IDocumentationSetContext context
	)
	{
		var raw = entry.Catalog?.Categories ?? [];
		if (raw.Count == 0)
			return [];

		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var item in raw)
		{
			if (ApiCatalogCategory.Normalize(item) is { } canonical)
			{
				_ = seen.Add(canonical);
				continue;
			}

			context.Collector.Write(new Diagnostic
			{
				Severity = Severity.Error,
				File = context.ConfigurationPath.FullName,
				Line = entry.Catalog?.Line ?? entry.Line,
				Column = entry.Catalog?.Column ?? entry.Column,
				Message =
					$"Unknown catalog category '{item}' for API '{productKey}'. Valid categories: {string.Join(", ", ApiCatalogCategory.DisplayOrder)} (or {ApplicabilityKeys.Ech} for {ApplicabilityKeys.Ess})."
			});
		}

		return ApiCatalogCategory.DisplayOrder.Where(seen.Contains).ToArray();
	}

	/// Children resolve only under 'api/&lt;key&gt;/'; escaping paths and symlinks are rejected the
	/// same way branding image paths are (see <see cref="ValidateBrandingImage"/>).
	private static List<IFileInfo> ResolveApiChildren(
		string productKey,
		List<ApiEntryChild> children,
		IDocumentationSetContext context,
		IDirectoryInfo childrenDirectory
	)
	{
		if (children.Count == 0)
			return [];

		var resolved = new List<IFileInfo>();
		foreach (var child in children)
		{
			if (string.IsNullOrWhiteSpace(child.File))
			{
				context.EmitError(context.ConfigurationPath, $"A 'children' entry for API '{productKey}' is missing a 'file' value.");
				continue;
			}

			var fullPath = Path.GetFullPath(Path.Join(childrenDirectory.FullName, child.File));
			var childFile = context.ReadFileSystem.FileInfo.New(fullPath);

			if (!childFile.IsSubPathOf(childrenDirectory))
			{
				context.EmitError(
					context.ConfigurationPath,
					$"Child page '{child.File}' for API '{productKey}' escapes 'api/{productKey}/'."
				);
				continue;
			}

			var symlinkError = ValidateFileAccess(childFile, childrenDirectory);
			if (symlinkError is not null)
			{
				context.EmitError(context.ConfigurationPath, $"Child page '{child.File}' for API '{productKey}' is unsafe: {symlinkError}");
				continue;
			}

			if (!childFile.Exists)
			{
				context.EmitError(
					context.ConfigurationPath,
					$"Child page '{child.File}' for API '{productKey}' does not exist under 'api/{productKey}/'."
				);
				continue;
			}

			if (IsTopLevelSupplementalChild(childFile, childrenDirectory))
			{
				context.EmitError(
					context.ConfigurationPath,
					$"Child page '{child.File}' for API '{productKey}' uses a supplemental file name (op-*.md / tag-*.md). Those files are auto-discovered and cannot be listed under children:."
				);
				continue;
			}

			resolved.Add(childFile);
		}

		return resolved;
	}

	private static bool IsTopLevelSupplementalChild(IFileInfo childFile, IDirectoryInfo apiDirectory) =>
		childFile.Directory is not null
			&& string.Equals(childFile.Directory.FullName, apiDirectory.FullName, StringComparison.OrdinalIgnoreCase)
			&& ResolvedApiConfiguration.IsSupplementalFileName(childFile.Name);

	private static CrossLinkEntry? ParseCrossLinkEntry(
		string raw,
		DocSetRegistry docsetRegistry,
		IFileInfo configPath,
		IDocumentationContext context
	)
	{
		DocSetRegistry entryRegistry;
		string repository;

		var colonSlash = raw.IndexOf("://", StringComparison.Ordinal);
		if (colonSlash >= 0)
		{
			var prefix = raw[..colonSlash];
			repository = raw[(colonSlash + 3)..];
			if (string.IsNullOrWhiteSpace(repository))
			{
				context.EmitError(configPath, $"Cross-link '{raw}' has empty repository after registry prefix.");
				return null;
			}
			if (!DocSetRegistryExtensions.TryParse(prefix, out entryRegistry, true))
			{
				context.EmitError(configPath, $"Cross-link '{raw}' uses unknown registry '{prefix}'. Use 'public' or 'internal'.");
				return null;
			}
		}
		else
		{
			repository = raw;
			entryRegistry = docsetRegistry;
		}

		if (docsetRegistry == DocSetRegistry.Public && entryRegistry != DocSetRegistry.Public)
		{
			context.EmitError(
				configPath,
				$"Public documentation cannot link to codex docs. Cross-link '{raw}' targets registry '{entryRegistry.ToStringFast()}'. Remove it or use a public docset."
			);
			return null;
		}

		return new CrossLinkEntry(repository, entryRegistry);
	}

	/// <summary>
	/// Adapts the validation helpers, which report through an <see cref="IDocumentationSetContext"/>, to a
	/// <see cref="Result{T}"/>: everything they emit is buffered instead of reaching the caller's collector.
	/// </summary>
	private sealed class ResolutionContext(DocumentationSetSource source) : IDocumentationSetContext
	{
		public BufferingDiagnosticsCollector Buffer { get; } = new();
		public IDiagnosticsCollector Collector => Buffer;
		public IFileInfo ConfigurationPath => source.ConfigurationPath;
		public IDocumentationFileSystem ReadFileSystem => source.ReadFileSystem;
		public IDirectoryInfo DocumentationSourceDirectory => source.SourceDirectory;
		public IEnvironmentVariables Environment => source.Environment;
		public DocumentationWriteFileSystem WriteFileSystem => throw new NotSupportedException("Resolution does not write");
		public IDirectoryInfo OutputDirectory => throw new NotSupportedException("Resolution does not write");
		public BuildType BuildType => throw new NotSupportedException("Resolution is independent of the build type");
		public GitCheckoutInformation Git => throw new NotSupportedException("Resolution is independent of the checkout");
	}
}
