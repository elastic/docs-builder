// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using DotNet.Globbing;
using Elastic.Documentation.Configuration.TableOfContents;
using Elastic.Documentation.Links;
using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Configuration.Builder;

public record ConfigurationFile : ITableOfContentsScope
{
	private readonly IDocumentationContext _context;

	public IFileInfo SourceFile => _context.ConfigurationPath;

	public string? Project { get; }

	public Glob[] Exclude { get; } = [];

	public string[] CrossLinkRepositories { get; } = [];

	/// The maximum depth `toc.yml` files may appear
	public int MaxTocDepth { get; } = 1;

	public EnabledExtensions Extensions { get; } = new([]);

	public IReadOnlyCollection<ITocItem> TableOfContents { get; } = [];

	public HashSet<string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, LinkRedirect>? Redirects { get; }

	public HashSet<string> ImplicitFolders { get; } = new(StringComparer.OrdinalIgnoreCase);

	public Glob[] Globs { get; } = [];

	private readonly Dictionary<string, string> _substitutions = new(StringComparer.OrdinalIgnoreCase);
	public IReadOnlyDictionary<string, string> Substitutions => _substitutions;

	private readonly Dictionary<string, bool> _features = new(StringComparer.OrdinalIgnoreCase);
	private FeatureFlags? _featureFlags;
	public FeatureFlags Features => _featureFlags ??= new FeatureFlags(_features);

	public IDirectoryInfo ScopeDirectory { get; }

	/// This is a documentation set that is not linked to by assembler.
	/// Setting this to true relaxes a few restrictions such as mixing toc references with file and folder reference
	public bool DevelopmentDocs { get; }

	// TODO ensure project key is `docs-content`
	public bool IsNarrativeDocs =>
		Project is not null
		&& Project.Equals("Elastic documentation", StringComparison.OrdinalIgnoreCase);

	public ConfigurationFile(IDocumentationContext context)
	{
		_context = context;
		ScopeDirectory = context.ConfigurationPath.Directory!;
		if (!context.ConfigurationPath.Exists)
		{
			Project = "unknown";
			context.EmitWarning(context.ConfigurationPath, "No configuration file found");
			return;
		}

		var sourceFile = context.ConfigurationPath;
		var redirectFileName = sourceFile.Name.StartsWith('_') ? "_redirects.yml" : "redirects.yml";
		var redirectFileInfo = sourceFile.FileSystem.FileInfo.New(Path.Combine(sourceFile.Directory!.FullName, redirectFileName));
		var redirectFile = new RedirectFile(redirectFileInfo, _context);
		Redirects = redirectFile.Redirects;

		var reader = new YamlStreamReader(sourceFile, _context.Collector);
		try
		{
			foreach (var entry in reader.Read())
			{
				switch (entry.Key)
				{
					case "project":
						Project = reader.ReadString(entry.Entry);
						break;
					case "max_toc_depth":
						MaxTocDepth = int.TryParse(reader.ReadString(entry.Entry), out var maxTocDepth) ? maxTocDepth : 1;
						break;
					case "dev_docs":
						DevelopmentDocs = bool.TryParse(reader.ReadString(entry.Entry), out var devDocs) && devDocs;
						break;
					case "exclude":
						var excludes = YamlStreamReader.ReadStringArray(entry.Entry);
						Exclude = [.. excludes.Where(s => !string.IsNullOrEmpty(s)).Select(Glob.Parse)];
						break;
					case "cross_links":
						CrossLinkRepositories = [.. YamlStreamReader.ReadStringArray(entry.Entry)];
						break;
					case "extensions":
						Extensions = new([.. YamlStreamReader.ReadStringArray(entry.Entry)]);
						break;
					case "subs":
						_substitutions = reader.ReadDictionary(entry.Entry);
						break;
					case "toc":
						// read this later
						break;
					case "features":
						_features = reader.ReadDictionary(entry.Entry).ToDictionary(k => k.Key, v => bool.Parse(v.Value), StringComparer.OrdinalIgnoreCase);
						break;
					case "external_hosts":
						reader.EmitWarning($"{entry.Key} has been deprecated and will be removed", entry.Key);
						break;
					default:
						reader.EmitWarning($"{entry.Key} is not a known configuration", entry.Key);
						break;
				}
			}

			var toc = new TableOfContentsConfiguration(this, sourceFile, ScopeDirectory, _context, 0, "");
			TableOfContents = toc.TableOfContents;
			Files = toc.Files;
		}
		catch (Exception e)
		{
			reader.EmitError("Could not load docset.yml", e);
			throw;
		}

		Globs = [.. ImplicitFolders.Select(f => Glob.Parse($"{f}{Path.DirectorySeparatorChar}*.md"))];
	}

}
