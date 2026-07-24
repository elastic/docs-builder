// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using DotNet.Globbing;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Suggestions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Links;
using static Elastic.Documentation.SymlinkValidator;

namespace Elastic.Documentation.Configuration.Builder;

public record ConfigurationFile
{
	private readonly IDocumentationSetContext _context;

	public IFileInfo SourceFile => _context.ConfigurationPath;

	public string? Project { get; }

	private Glob[] Exclude { get; } = [];
	private string[] Include { get; } = [];

	public string[] CrossLinkRepositories { get; } = [];

	/// <summary>
	/// Registry for this documentation set. <c>Public</c> uses S3 link index; other values use codex-link-index.
	/// </summary>
	public DocSetRegistry Registry { get; } = DocSetRegistry.Public;

	/// <summary>
	/// Parsed cross-link entries with registry for each target.
	/// </summary>
	public CrossLinkEntry[] CrossLinkEntries { get; } = [];

	/// <summary>
	/// Canonical product ids declared under <c>release_notes</c> whose changelog content is sourced from
	/// the public CDN. Validated against products.yml; drives startup prefetch for the <c>{changelog}</c>
	/// directive and CDN sourcing for <c>changelog bundle</c>.
	/// </summary>
	public string[] ReleaseNotesProducts { get; } = [];

	/// The maximum depth `toc.yml` files may appear
	public int MaxTocDepth { get; } = 1;

	public EnabledExtensions Extensions { get; } = new([]);

	public Dictionary<string, LinkRedirect>? Redirects { get; }

	public HashSet<Product> Products { get; private set; } = [];

	private readonly Dictionary<string, string> _substitutions = [with(StringComparer.OrdinalIgnoreCase)];
	public IReadOnlyDictionary<string, string> Substitutions => _substitutions;

	private readonly Dictionary<string, bool> _features = [with(StringComparer.OrdinalIgnoreCase)];

	[field: AllowNull, MaybeNull]
	public FeatureFlags Features => field ??= new FeatureFlags(_features);

	public IDirectoryInfo ScopeDirectory { get; }

	public IReadOnlyDictionary<string, IFileInfo>? OpenApiSpecifications { get; }

	public string? StorybookRegistry { get; }

	/// <summary>
	/// Environment-independent <c>storybook.registry</c> value (committed default). Non-null only when an allow-listed
	/// environment variable changed <see cref="StorybookRegistry"/>, so the directive can degrade to the committed
	/// registry when the environment-supplied one is unreachable (e.g. an ephemeral PR registry that is not yet published).
	/// </summary>
	public string? StorybookRegistryFallback { get; }

	/// <summary>
	/// Resolved API configurations with template and specification file information.
	/// </summary>
	public IReadOnlyDictionary<string, ResolvedApiConfiguration>? ApiConfigurations { get; }

	/// <summary>
	/// Set of diagnostic hint types to suppress for this documentation set.
	/// </summary>
	public HashSet<HintType> SuppressDiagnostics { get; } = [];

	/// <summary>
	/// White-label branding overrides. When non-null, all Elastic-specific chrome is suppressed.
	/// </summary>
	public BrandingConfiguration? Branding { get; private set; }

	private readonly Dictionary<string, Cta> _ctas = new(StringComparer.OrdinalIgnoreCase) { [Cta.DefaultName] = Cta.Default };

	// Path scopes declared via `cta.<name>.paths`, as (normalized prefix, template name) pairs ordered
	// longest-prefix-first so the most specific scope wins during resolution.
	private readonly List<KeyValuePair<string, string>> _ctaPathScopes = [];

	/// <summary>
	/// Named right-gutter CTA templates declared under <c>docset.yml</c>'s <c>cta</c> map, keyed by name.
	/// Always contains at least the built-in <see cref="Cta.DefaultName"/> entry.
	/// </summary>
	public IReadOnlyDictionary<string, Cta> Ctas => _ctas;

	/// This is a documentation set not linked to by assembler.
	/// Setting this to true relaxes a few restrictions such as mixing toc references with file and folder reference
	public bool DevelopmentDocs { get; }

	// Files excluded via folder-level `exclude` in toc.yml need to be excluded from processing too,
	// otherwise the builder crashes with "Could not find current in navigation" when rendering them.
	private HashSet<string> FolderExcludedFiles { get; } = [];

	public bool IsExcluded(string relativePath)
	{
		if (Include.Length > 0 && Include.Any(i => i.Equals(relativePath.OptionalWindowsReplace(), StringComparison.OrdinalIgnoreCase)))
			return false;
		if (FolderExcludedFiles.Contains(relativePath.OptionalWindowsReplace()))
			return true;
		return Exclude.Any(g => g.IsMatch(relativePath));
	}

	public ConfigurationFile(DocumentationSetFile docSetFile, IDocumentationSetContext context, VersionsConfiguration versionsConfig, ProductsConfiguration productsConfig)
	{
		_context = context;
		ScopeDirectory = context.ConfigurationPath.Directory!;
		if (!context.ConfigurationPath.Exists)
		{
			Project = "unknown";
			context.EmitWarning(context.ConfigurationPath, "No configuration file found");
			return;
		}


		var redirectFile = new RedirectFile(_context);
		Redirects = redirectFile.Redirects;

		try
		{
			// Read values from DocumentationSetFile
			Project = docSetFile.Project;
			MaxTocDepth = docSetFile.MaxTocDepth;
			DevelopmentDocs = docSetFile.DevDocs;

			// Convert exclude patterns to Glob
			Exclude = [.. docSetFile.Exclude.Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith('!')).Select(Glob.Parse)];
			Include = [.. docSetFile.Exclude.Where(s => !string.IsNullOrEmpty(s) && s.StartsWith('!')).Select(s => s.TrimStart('!'))];
			FolderExcludedFiles = docSetFile.FolderExcludedFiles;

			// Parse registry (null/empty/"public" -> Public)
			var registry = DocSetRegistry.Public;
			if (!string.IsNullOrWhiteSpace(docSetFile.Registry) &&
				DocSetRegistryExtensions.TryParse(docSetFile.Registry.Trim(), out var parsedRegistry, true))
				registry = parsedRegistry;

			Registry = registry;

			// Parse cross-link entries with optional registry prefix (e.g. public://elasticsearch)
			CrossLinkEntries = docSetFile.CrossLinks
				.Where(raw => !string.IsNullOrWhiteSpace(raw))
				.Select(raw => ParseCrossLinkEntry(raw.Trim(), registry, context.ConfigurationPath, context))
				.Where(entry => entry is not null)
				.Select(entry => entry!)
				.ToArray();

			CrossLinkRepositories = CrossLinkEntries.Select(e => e.Repository).ToArray();

			// Parse and validate CDN-backed release-notes product declarations
			ReleaseNotesProducts = ParseReleaseNotesProducts(docSetFile.ReleaseNotes, productsConfig, context);

			// Extensions - assuming they're not in DocumentationSetFile yet
			Extensions = new EnabledExtensions(docSetFile.Extensions);

			// Copy suppression settings
			SuppressDiagnostics = docSetFile.SuppressDiagnostics;

			// Read substitutions
			_substitutions = new(docSetFile.Subs, StringComparer.OrdinalIgnoreCase);

			// Process API configurations
			if (docSetFile.Api.Count > 0)
			{
				var specs = new Dictionary<string, IFileInfo>(StringComparer.OrdinalIgnoreCase);
				var apiConfigs = new Dictionary<string, ResolvedApiConfiguration>(StringComparer.OrdinalIgnoreCase);

				foreach (var (productKey, apiSequence) in docSetFile.Api)
				{
					if (!apiSequence.IsValid)
					{
						context.EmitError(
							context.ConfigurationPath,
							$"API configuration for '{productKey}' is invalid. Must have at least one spec and all entries must be valid."
						);
						continue;
					}

					// Resolve intro markdown files
					var introMarkdownFiles = new List<IFileInfo>();
					foreach (var introPath in apiSequence.GetIntroMarkdownFiles())
					{
						var fullPath = Path.Join(context.DocumentationSourceDirectory.FullName, introPath);
						var introFile = context.ReadFileSystem.FileInfo.New(fullPath);
						if (!introFile.Exists)
						{
							context.EmitWarning(
								context.ConfigurationPath,
								$"Intro markdown file '{introPath}' for API '{productKey}' does not exist."
							);
						}
						else
						{
							introMarkdownFiles.Add(introFile);
						}
					}

					// Resolve outro markdown files
					var outroMarkdownFiles = new List<IFileInfo>();
					foreach (var outroPath in apiSequence.GetOutroMarkdownFiles())
					{
						var fullPath = Path.Join(context.DocumentationSourceDirectory.FullName, outroPath);
						var outroFile = context.ReadFileSystem.FileInfo.New(fullPath);
						if (!outroFile.Exists)
						{
							context.EmitWarning(
								context.ConfigurationPath,
								$"Outro markdown file '{outroPath}' for API '{productKey}' does not exist."
							);
						}
						else
						{
							outroMarkdownFiles.Add(outroFile);
						}
					}

					// Resolve specification files
					var specFiles = new List<IFileInfo>();
					foreach (var specPath in apiSequence.GetSpecPaths())
					{
						var fullPath = Path.Join(context.DocumentationSourceDirectory.FullName, specPath);
						var specFile = context.ReadFileSystem.FileInfo.New(fullPath);
						if (!specFile.Exists)
						{
							context.EmitError(
								context.ConfigurationPath,
								$"API specification file '{specPath}' for product '{productKey}' does not exist."
							);
							continue;
						}
						specFiles.Add(specFile);
					}

					if (specFiles.Count == 0)
					{
						context.EmitError(
							context.ConfigurationPath,
							$"No valid specification files found for API product '{productKey}'."
						);
						continue;
					}

					// Create resolved configuration
					var resolvedConfig = new ResolvedApiConfiguration
					{
						ProductKey = productKey,
						IntroMarkdownFiles = introMarkdownFiles,
						SpecFiles = specFiles,
						OutroMarkdownFiles = outroMarkdownFiles
					};

					apiConfigs[productKey] = resolvedConfig;

					// For backward compatibility, populate OpenApiSpecifications with primary spec
					specs[productKey] = resolvedConfig.PrimarySpecFile;
				}

				OpenApiSpecifications = specs.Count > 0 ? specs : null;
				ApiConfigurations = apiConfigs.Count > 0 ? apiConfigs : null;
			}

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
				StorybookRegistry = interpolated.Value;
				StorybookRegistryFallback = interpolated.Fallback;
			}

			// Process products from docset - resolve ProductLinks to Product objects
			if (docSetFile.Products.Count > 0)
			{
				Products = docSetFile.Products
					.Select(link => productsConfig.Products.GetValueOrDefault(link.Id.Replace('_', '-')))
					.Where(product => product is not null)
					.ToHashSet()!;
			}

			// Process branding with validation
			if (docSetFile.Branding is not null)
				Branding = ValidateBranding(docSetFile.Branding, context);

			// Process CTA templates - overlays onto (and may override) the built-in 'trial' default
			var ctaPathScopes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var (name, definition) in docSetFile.Cta)
			{
				if (ValidateCta(name, definition, context) is not { } cta)
					continue;
				_ctas[name] = cta;
				CollectCtaPathScopes(name, definition.Paths, ctaPathScopes, context);
			}
			_ctaPathScopes = [.. ctaPathScopes.OrderByDescending(kv => kv.Key.Length)];

			// Process features
			_features = [with(StringComparer.OrdinalIgnoreCase)];
			if (docSetFile.Features.PrimaryNav.HasValue)
				_features["primary-nav"] = docSetFile.Features.PrimaryNav.Value;
			if (docSetFile.Features.DisableGithubEditLink.HasValue)
				_features["disable-github-edit-link"] = docSetFile.Features.DisableGithubEditLink.Value;

			// primary-nav requires the Elastic global navigation which is not available for white-label builds
			if (Branding is not null && docSetFile.Features.PrimaryNav is true)
				context.EmitError(context.ConfigurationPath, "'features.primary-nav' cannot be used together with 'branding': the primary nav requires Elastic global navigation.");

			// Add version substitutions
			foreach (var (id, system) in versionsConfig.VersioningSystems)
			{
				var name = id.ToStringFast(true);
				var alternativeName = name.Replace('-', '_');
				_substitutions[$"version.{name}"] = system.Current;
				_substitutions[$"version.{alternativeName}"] = system.Current;
				_substitutions[$"version.{name}.base"] = system.Base;
				_substitutions[$"version.{alternativeName}.base"] = system.Base;
			}

			// Add product substitutions (only for products with public-reference feature)
			foreach (var product in productsConfig.PublicReferenceProducts.Values)
			{
				var alternativeProductId = product.Id.Replace('-', '_');
				_substitutions[$"product.{product.Id}"] = product.DisplayName;
				_substitutions[$".{product.Id}"] = product.DisplayName;
				_substitutions[$"product.{alternativeProductId}"] = product.DisplayName;
				_substitutions[$".{alternativeProductId}"] = product.DisplayName;
			}
		}
		catch (Exception e)
		{
			context.EmitError(context.ConfigurationPath, $"Could not load docset.yml: {e.Message}");
			throw;
		}
	}

	/// <summary>
	/// Resolves the right-gutter CTA for a page. An explicit, known <c>cta</c> frontmatter <paramref name="id"/>
	/// always wins. Otherwise the template whose <c>paths</c> scope matches <paramref name="relativePath"/>
	/// applies (most specific prefix first), falling back to <see cref="Cta.DefaultName"/>.
	/// </summary>
	/// <param name="id">The page's <c>cta.id</c> frontmatter value, if any.</param>
	/// <param name="relativePath">The page's docset-root-relative source path, used for path-scope matching.</param>
	/// <param name="warning">Set when <paramref name="id"/> is unknown, so the caller can report it.</param>
	public Cta ResolveCta(string? id, string? relativePath, out string? warning)
	{
		warning = null;
		if (id is not null)
		{
			if (Ctas.TryGetValue(id, out var selected))
				return selected;
			// Unknown id: warn, then resolve as if the page had no `cta` frontmatter.
			warning = UnknownCtaWarning(id, Ctas.Keys);
		}
		if (relativePath is { Length: > 0 } && MatchCtaPathScope(relativePath) is { } scoped)
			return scoped;
		return Ctas[Cta.DefaultName];
	}

	private Cta? MatchCtaPathScope(string relativePath)
	{
		if (_ctaPathScopes.Count == 0)
			return null;
		var normalized = relativePath.Replace('\\', '/').TrimStart('/');
		foreach (var (prefix, name) in _ctaPathScopes)
		{
			// Whole-segment prefix match: "solutions/observability" must not match "solutions/observability-labs/...".
			if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
				&& (normalized.Length == prefix.Length || normalized[prefix.Length] == '/'))
				return Ctas[name];
		}
		return null;
	}

	private static void CollectCtaPathScopes(string name, List<string> paths, Dictionary<string, string> scopes, IDocumentationSetContext context)
	{
		foreach (var path in paths)
		{
			var prefix = path.Trim().Replace('\\', '/').Trim('/');
			if (string.IsNullOrEmpty(prefix))
			{
				context.EmitError(context.ConfigurationPath, $"'cta.{name}.paths' contains an empty path.");
				continue;
			}
			if (!scopes.TryAdd(prefix, name))
				context.EmitError(context.ConfigurationPath, $"'cta.{name}.paths' declares '{prefix}' which is already claimed by 'cta.{scopes[prefix]}'. Each path can only map to one CTA template.");
		}
	}

	private static string UnknownCtaWarning(string ctaName, IEnumerable<string> knownCtaNames)
	{
		var known = knownCtaNames.ToHashSet();
		var hint = new Suggestion(known, ctaName).GetSuggestionQuestion();
		if (string.IsNullOrEmpty(hint))
		{
			hint = known.Count > 1
				? $"Available: {string.Join(", ", known.Order())}."
				: "No 'cta' templates are defined in this docset.yml yet. Add one under a top-level 'cta:' map, e.g.:\n" +
					"cta:\n  mp:\n    button:\n      label: Get started on MP\n      url: https://example.com\n    benefits:\n      - \"Some benefit\"";
		}
		return $"'cta: {ctaName}' does not match any 'cta' template in docset.yml and is ignored. {hint}";
	}

	private static Cta? ValidateCta(string name, CtaDefinition definition, IDocumentationSetContext context)
	{
		if (string.IsNullOrWhiteSpace(definition.Button?.Label) || string.IsNullOrWhiteSpace(definition.Button?.Url))
		{
			context.EmitError(context.ConfigurationPath, $"'cta.{name}' must define both 'button.label' and 'button.url'.");
			return null;
		}
		var url = definition.Button.Url.Trim();
		if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri) && uri.IsAbsoluteUri &&
			uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
		{
			context.EmitError(context.ConfigurationPath, $"'cta.{name}.button.url' must use http/https or a relative URL.");
			return null;
		}
		if (definition.Benefits.Count > Cta.MaxBenefits)
		{
			context.EmitError(context.ConfigurationPath, $"'cta.{name}.benefits' has {definition.Benefits.Count} entries; a maximum of {Cta.MaxBenefits} is allowed.");
			return null;
		}
		return new Cta
		{
			Name = name,
			Label = definition.Button.Label,
			Url = url,
			Benefits = definition.Benefits
		};
	}

	private static readonly HashSet<string> AllowedImageExtensions =
		[".svg", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico"];

	private static BrandingConfiguration ValidateBranding(BrandingConfiguration branding, IDocumentationSetContext context)
	{
		branding.Icon = ValidateBrandingImage(branding.Icon, "branding.icon", context);
		branding.OgImage = ValidateBrandingImage(branding.OgImage, "branding.og-image", context);
		branding.Favicon = string.IsNullOrEmpty(branding.Favicon)
			? DiscoverBrandingFile(["favicon.ico", "favicon.png", "favicon.svg"], context)
			: ValidateBrandingImage(branding.Favicon, "branding.favicon", context);
		branding.AppleTouchIcon = string.IsNullOrEmpty(branding.AppleTouchIcon)
			? DiscoverBrandingFile(["apple-touch-icon.png"], context)
			: ValidateBrandingImage(branding.AppleTouchIcon, "branding.apple-touch-icon", context);
		return branding;
	}

	private static string? DiscoverBrandingFile(string[] candidates, IDocumentationSetContext context)
	{
		foreach (var name in candidates)
		{
			var f = context.ReadFileSystem.FileInfo.New(
				Path.Join(context.DocumentationSourceDirectory.FullName, name));
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
			context.EmitError(context.ConfigurationPath,
				$"'{fieldName}' has unsupported extension '{ext}'. Allowed: {string.Join(", ", AllowedImageExtensions)}");
			return null;
		}

		var resolved = context.ReadFileSystem.FileInfo.New(
			Path.GetFullPath(Path.Join(context.DocumentationSourceDirectory.FullName, imagePath))
		);

		if (!resolved.IsSubPathOf(context.DocumentationSourceDirectory))
		{
			context.EmitError(context.ConfigurationPath,
				$"'{fieldName}' path '{imagePath}' escapes the documentation source directory.");
			return null;
		}

		var symlinkError = ValidateFileAccess(resolved, context.DocumentationSourceDirectory);
		if (symlinkError is not null)
		{
			context.EmitError(context.ConfigurationPath,
				$"'{fieldName}' path '{imagePath}' is unsafe: {symlinkError}");
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
		ProductsConfiguration productsConfig,
		IDocumentationSetContext context)
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
				context.EmitError(context.ConfigurationPath,
					$"Invalid 'release_notes' product '{product}'. Product ids must match [a-zA-Z0-9_-]+.");
				continue;
			}

			// products.yml keys are hyphenated; accept the underscore variant for parity with `products`.
			var normalized = product.Replace('_', '-');
			if (!productsConfig.Products.TryGetValue(normalized, out var resolved))
			{
				context.EmitError(context.ConfigurationPath,
					$"Unknown 'release_notes' product '{product}'. It must be a product id defined in products.yml.");
				continue;
			}

			if (!resolved.Features.ReleaseNotes)
			{
				context.EmitError(context.ConfigurationPath,
					$"Product '{product}' declared in 'release_notes' does not participate in the release-notes system (it lacks the 'release-notes' feature in products.yml).");
				continue;
			}

			if (!products.Contains(resolved.Id, StringComparer.Ordinal))
				products.Add(resolved.Id);
		}

		return [.. products];
	}

	private static bool IsValidProductId(string product) =>
		product.Length > 0 && product.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');

	private static CrossLinkEntry? ParseCrossLinkEntry(string raw, DocSetRegistry docsetRegistry, IFileInfo configPath, IDocumentationContext context)
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
			context.EmitError(configPath, $"Public documentation cannot link to codex docs. Cross-link '{raw}' targets registry '{entryRegistry.ToStringFast()}'. Remove it or use a public docset.");
			return null;
		}

		return new CrossLinkEntry(repository, entryRegistry);
	}
}
