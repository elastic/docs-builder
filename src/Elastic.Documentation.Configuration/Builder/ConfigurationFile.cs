// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using DotNet.Globbing;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Links;
using Elastic.Documentation.Suggestions;
using static Elastic.Documentation.SymlinkValidator;

namespace Elastic.Documentation.Configuration.Builder;

public record ConfigurationFile
{
	private readonly IDocumentationSetContext _context;
	private readonly ResolvedDocumentationSet _resolved;
	private readonly Glob[] _exclude;

	public IFileInfo SourceFile => _context.ConfigurationPath;

	public string? Project => _resolved.Project;

	public string[] CrossLinkRepositories { get; }

	/// <summary>
	/// Registry for this documentation set. <c>Public</c> uses S3 link index; other values use codex-link-index.
	/// </summary>
	public DocSetRegistry Registry => _resolved.Registry;

	/// <summary>
	/// Parsed cross-link entries with registry for each target.
	/// </summary>
	public CrossLinkEntry[] CrossLinkEntries { get; }

	/// <summary>
	/// Canonical product ids declared under <c>release_notes</c> whose changelog content is sourced from
	/// the public CDN. Validated against products.yml; drives startup prefetch for the <c>{changelog}</c>
	/// directive and CDN sourcing for <c>changelog bundle</c>.
	/// </summary>
	public string[] ReleaseNotesProducts { get; }

	/// The maximum depth `toc.yml` files may appear
	public int MaxTocDepth => _resolved.MaxTocDepth;

	public EnabledExtensions Extensions => _resolved.Extensions;

	public Dictionary<string, LinkRedirect>? Redirects { get; }

	public IFileInfo? RedirectsSource { get; }

	public HashSet<Product> Products { get; }

	public IReadOnlyDictionary<string, string> Substitutions => _resolved.Substitutions;

	[field: AllowNull, MaybeNull]
	public FeatureFlags Features { get => field ??= new FeatureFlags([with(_resolved.FeatureToggles)], _context.Environment); private set; }

	internal void OverrideFeatures(Func<FeatureFlags, FeatureFlags> apply) => Features = apply(Features);

	public IDirectoryInfo ScopeDirectory => _resolved.ScopeDirectory;

	public string? StorybookRegistry => _resolved.StorybookRegistry;

	/// <summary>
	/// Environment-independent <c>storybook.registry</c> value (committed default). Non-null only when an allow-listed
	/// environment variable changed <see cref="StorybookRegistry"/>, so the directive can degrade to the committed
	/// registry when the environment-supplied one is unreachable (e.g. an ephemeral PR registry that is not yet published).
	/// </summary>
	public string? StorybookRegistryFallback => _resolved.StorybookRegistryFallback;

	/// <summary>
	/// Resolved API configurations with template and specification file information.
	/// </summary>
	public IReadOnlyDictionary<string, ResolvedApiConfiguration>? ApiConfigurations => _resolved.ApiConfigurations;

	/// <summary>
	/// Markdown paths (relative to <paramref name="documentationSourceDirectoryFullName"/>, <c>/</c> separated) that the
	/// API pipeline owns: <c>children:</c> pages and <c>op-*.md</c> / <c>tag-*.md</c> supplemental files.
	/// </summary>
	public HashSet<string> GetApiMarkdownExcludePaths(string documentationSourceDirectoryFullName)
	{
		var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (ApiConfigurations is null)
			return set;

		foreach (var apiConfig in ApiConfigurations.Values)
		{
			foreach (var path in apiConfig.GetMarkdownPathsToExclude(documentationSourceDirectoryFullName))
				_ = set.Add(path);
		}

		return set;
	}

	/// <summary>
	/// Set of diagnostic hint types to suppress for this documentation set.
	/// </summary>
	public HashSet<HintType> SuppressDiagnostics { get; }

	/// <summary>
	/// White-label branding overrides. When non-null, all Elastic-specific chrome is suppressed.
	/// </summary>
	public Branding? Branding => _resolved.Branding;

	/// <summary>
	/// Named right-gutter CTA templates declared under <c>docset.yml</c>'s <c>cta</c> map, keyed by name.
	/// Always contains at least the built-in <see cref="Cta.DefaultName"/> entry.
	/// </summary>
	public IReadOnlyDictionary<string, Cta> Ctas => _resolved.Ctas;

	/// This is a documentation set not linked to by assembler.
	/// Setting this to true relaxes a few restrictions such as mixing toc references with file and folder reference
	public bool DevelopmentDocs => _resolved.DevelopmentDocs;

	public bool IsExcluded(string relativePath)
	{
		var exclusions = _resolved.Exclusions;
		if (
			exclusions.IncludeOverrides.Count > 0
			&& exclusions.IncludeOverrides.Any(i => i.Equals(relativePath.OptionalWindowsReplace(), StringComparison.OrdinalIgnoreCase))
		)
			return false;
		if (exclusions.FolderExcludedFiles.Contains(relativePath.OptionalWindowsReplace()))
			return true;
		return _exclude.Any(g => g.IsMatch(relativePath));
	}

	public ConfigurationFile(
		DocumentationSetDefinition docSetFile,
		IDocumentationSetContext context,
		VersioningSystemCatalog versionsConfig,
		ProductCatalog productsConfig
	)
	{
		_context = context;
		DocumentationSetResolver.Resolution resolution;
		if (!context.ConfigurationPath.Exists)
			resolution = DocumentationSetResolver.Missing(context);
		else
		{
			var redirectFile = new RedirectFile(_context);
			Redirects = redirectFile.Redirects;
			RedirectsSource = redirectFile.Source;
			resolution = DocumentationSetResolver.Resolve(docSetFile, context, versionsConfig, productsConfig);
		}
		_resolved = resolution.Set;

		// the patterns parsed when the set was resolved
		_exclude = resolution.ExcludeGlobs;
		CrossLinkEntries = [.. _resolved.CrossLinkEntries];
		CrossLinkRepositories = [.. _resolved.CrossLinkRepositories];
		ReleaseNotesProducts = [.. _resolved.ReleaseNotesProducts];
		Products = [.. _resolved.Products];
		SuppressDiagnostics = [.. _resolved.SuppressDiagnostics];
	}

	/// <summary>
	/// Resolves the right-gutter CTA for a page. An explicit, known <c>cta</c> frontmatter <paramref name="id"/>
	/// always wins. Otherwise the template registered via <c>default_cta</c> on the page's navigation file
	/// applies, falling back to <see cref="Cta.DefaultName"/>.
	/// </summary>
	/// <param name="id">The page's <c>cta.id</c> frontmatter value, if any.</param>
	/// <param name="relativePath">The page's docset-root-relative source path, used for toc default lookup.</param>
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
		if (relativePath is { Length: > 0 })
		{
			var normalizedPath = DocumentationSetDto.NormalizeDocsetRelativePath(relativePath);
			if (_resolved.TocDefaultCtas.TryGetValue(normalizedPath, out var tocDefault) && Ctas.TryGetValue(tocDefault, out var scoped))
				return scoped;
		}
		return Ctas[Cta.DefaultName];
	}

	private static string UnknownCtaWarning(string ctaName, IEnumerable<string> knownCtaNames)
	{
		var known = knownCtaNames.ToHashSet();
		var hint = new Suggestion(known, ctaName).GetSuggestionQuestion();
		if (string.IsNullOrEmpty(hint))
		{
			hint = known.Count > 1
				? $"Available: {string.Join(", ", known.Order())}."
				: "No 'cta' templates are defined in this docset.yml yet. Add one under a top-level 'cta:' map, e.g.:\n"
					+ "cta:\n  mp:\n    button:\n      label: Get started on MP\n      url: https://example.com\n    benefits:\n      - \"Some benefit\"";
		}
		return $"'cta: {ctaName}' does not match any 'cta' template in docset.yml and is ignored. {hint}";
	}
}
