// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using DotNet.Globbing;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.Configuration.Builder;

public record ConfigurationFile
{
	private readonly IDocumentationSetContext _context;

	public IFileInfo SourceFile => _context.ConfigurationPath;

	public string? Project { get; }

	private Glob[] Exclude { get; } = [];
	private string[] Include { get; } = [];

	public string[] CrossLinkRepositories { get; } = [];

	/// The maximum depth `toc.yml` files may appear
	public int MaxTocDepth { get; } = 1;

	public EnabledExtensions Extensions { get; } = new([]);

	public Dictionary<string, LinkRedirect>? Redirects { get; }

	public HashSet<Product> Products { get; private set; } = [];

	private readonly Dictionary<string, string> _substitutions = new(StringComparer.OrdinalIgnoreCase);
	public IReadOnlyDictionary<string, string> Substitutions => _substitutions;

	private readonly Dictionary<string, bool> _features = new(StringComparer.OrdinalIgnoreCase);

	[field: AllowNull, MaybeNull]
	public FeatureFlags Features => field ??= new FeatureFlags(_features);

	public IDirectoryInfo ScopeDirectory { get; }

	public IReadOnlyDictionary<string, IFileInfo>? OpenApiSpecifications { get; }

	/// This is a documentation set not linked to by assembler.
	/// Setting this to true relaxes a few restrictions such as mixing toc references with file and folder reference
	public bool DevelopmentDocs { get; }

	public bool IsExcluded(string relativePath)
	{
		if (Include.Length > 0 && Include.Any(i => i.Equals(relativePath.OptionalWindowsReplace(), StringComparison.OrdinalIgnoreCase)))
			return false;
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

			// Set cross link repositories
			CrossLinkRepositories = [.. docSetFile.CrossLinks];

			// Extensions - assuming they're not in DocumentationSetFile yet
			Extensions = new EnabledExtensions(docSetFile.Extensions);

			// Read substitutions
			_substitutions = new(docSetFile.Subs, StringComparer.OrdinalIgnoreCase);

			// Process API specifications
			if (docSetFile.Api.Count > 0)
			{
				var specs = new Dictionary<string, IFileInfo>(StringComparer.OrdinalIgnoreCase);
				foreach (var (k, v) in docSetFile.Api)
				{
					var path = Path.Combine(context.DocumentationSourceDirectory.FullName, v);
					var fi = context.ReadFileSystem.FileInfo.New(path);
					specs[k] = fi;
				}
				OpenApiSpecifications = specs;
			}

			// Process products from docset - resolve ProductLinks to Product objects
			if (docSetFile.Products.Count > 0)
			{
				Products = docSetFile.Products
					.Select(link => productsConfig.Products.GetValueOrDefault(link.Id.Replace('_', '-')))
					.Where(product => product is not null)
					.ToHashSet()!;
			}

			// Process features
			_features = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
			if (docSetFile.Features.PrimaryNav.HasValue)
				_features["primary-nav"] = docSetFile.Features.PrimaryNav.Value;

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

			// Add product substitutions
			foreach (var product in productsConfig.Products.Values)
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

}
