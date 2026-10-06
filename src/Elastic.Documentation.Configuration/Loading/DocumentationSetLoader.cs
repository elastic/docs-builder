// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Converters;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// Reads a documentation set's own files into a <see cref="DocumentationSetDefinition"/>: <c>docset.yml</c>, its table
/// of contents with every nested <c>toc.yml</c>, and <c>redirects.yml</c>. It reports only what stops a definition from
/// being built (malformed YAML, an invalid table of contents); <see cref="DocumentationSetConstraints"/> checks the rest.
/// </summary>
public static class DocumentationSetLoader
{
	/// <summary>
	/// Loads <paramref name="docsetFile"/>. A missing file loads as an empty definition, which the constraints report.
	/// Input the parser rejects fails the load with its message as an error diagnostic.
	/// </summary>
	public static Result<DocumentationSetDefinition> Load(
		IFileInfo docsetFile,
		IDirectoryInfo sourceDirectory,
		IFileSystem fileSystem,
		ScopedFileSystem? scopedFileSystem = null
	)
	{
		if (!docsetFile.Exists)
			return Result.Success(ToDefinition(new DocumentationSetDto(), docsetFile, sourceDirectory, fileSystem, redirects: null));

		var buffer = new BufferingDiagnosticsCollector();
		DocumentationSetDto dto;
		try
		{
			dto = DocumentationSetDto.LoadAndResolve(buffer, docsetFile, scopedFileSystem);
		}
		catch (Exception e)
		{
			buffer.EmitError(docsetFile, e.Message);
			return Result.Failure<DocumentationSetDefinition>(buffer.Diagnostics);
		}

		RedirectFile redirectFile;
		try
		{
			redirectFile = new RedirectFile(docsetFile, buffer);
		}
		catch (Exception)
		{
			// RedirectFile has already reported the failure as an error.
			return Result.Failure<DocumentationSetDefinition>(buffer.Diagnostics);
		}

		return Result.Success(
			ToDefinition(dto, docsetFile, sourceDirectory, fileSystem, redirectFile.Redirects, redirectFile),
			buffer.Diagnostics
		);
	}

	/// <summary>Loads <paramref name="docsetFile"/> and its table of contents, reporting to <paramref name="collector"/>; no redirects.</summary>
	public static DocumentationSetDefinition LoadAndResolve(
		IDiagnosticsCollector collector,
		IFileInfo docsetFile,
		ScopedFileSystem? fileSystem = null,
		HashSet<HintType>? noSuppress = null
	)
	{
		var dto = DocumentationSetDto.LoadAndResolve(collector, docsetFile, fileSystem, noSuppress);
		return ToDefinition(dto, docsetFile, docsetFile.Directory!, fileSystem ?? docsetFile.FileSystem, redirects: null);
	}

	/// <summary>Loads a <c>docset.yml</c> given as text, declared in <paramref name="sourceDirectory"/>, reporting to <paramref name="collector"/>; no redirects.</summary>
	public static DocumentationSetDefinition LoadAndResolve(
		IDiagnosticsCollector collector,
		string yaml,
		IDirectoryInfo sourceDirectory,
		ScopedFileSystem? fileSystem = null,
		HashSet<HintType>? noSuppress = null
	)
	{
		var dto = DocumentationSetDto.LoadAndResolve(collector, yaml, sourceDirectory, fileSystem, noSuppress);
		var docsetFile = sourceDirectory.FileSystem.FileInfo.New(Path.Join(sourceDirectory.FullName, "docset.yml"));
		return ToDefinition(dto, docsetFile, sourceDirectory, fileSystem ?? sourceDirectory.FileSystem, redirects: null);
	}

	/// <summary>Reads <paramref name="docsetFile"/> without resolving its table of contents: for the metadata codex needs.</summary>
	public static DocumentationSetDefinition LoadMetadata(IFileInfo docsetFile) =>
		ToDefinition(
			DocumentationSetDto.LoadMetadata(docsetFile),
			docsetFile,
			docsetFile.Directory!,
			docsetFile.FileSystem,
			redirects: null
		);

	internal static DocumentationSetDefinition ToDefinition(
		DocumentationSetDto dto,
		IFileInfo docsetFile,
		IDirectoryInfo sourceDirectory,
		IFileSystem fileSystem,
		IReadOnlyDictionary<string, LinkRedirect>? redirects,
		RedirectFile? redirectFile = null
	) =>
		new()
		{
			SourceFile = docsetFile,
			SourceDirectory = sourceDirectory,
			FileSystem = fileSystem,
			Project = dto.Project,
			MaxTocDepth = dto.MaxTocDepth,
			DevDocs = dto.DevDocs,
			Island = dto.Island,
			CrossLinks = dto.CrossLinks,
			ReleaseNotesProducts = [.. dto.ReleaseNotes.Select(r => (string?)r.Product)],
			Exclude = dto.Exclude,
			Extensions = dto.Extensions,
			Substitutions = [.. dto.Subs],
#pragma warning disable CS0618 // carried over for codex, which still reads them
			DisplayName = dto.DisplayName,
			Description = dto.Description,
#pragma warning restore CS0618
			Icon = dto.Icon,
			Registry = dto.Registry,
			Features = new DeclaredFeatures(dto.Features.PrimaryNav, dto.Features.DisableGithubEditLink, dto.Features.GuideNav),
			Api = [.. dto.Api.Select(kv => ToApi(kv.Key, kv.Value))],
			Products = [.. dto.Products.Select(p => p.Id)],
			CodexGroup = dto.Codex?.Group,
			Branding = dto.Branding is { } b ? new BrandingDeclaration(b.Icon, b.HeaderBg, b.OgImage, b.Favicon, b.AppleTouchIcon) : null,
			HasStorybook = dto.Storybook is not null,
			StorybookRegistry = dto.Storybook?.Registry,
			Ctas = [.. dto.Cta.Select(kv => new CtaDeclaration(kv.Key, kv.Value.Button?.Label, kv.Value.Button?.Url, kv.Value.Benefits))],
			SuppressDiagnostics = dto.SuppressDiagnostics,
			DefaultCta = dto.DefaultCta,
			TableOfContents = dto.TableOfContents,
			FolderExcludedFiles = dto.FolderExcludedFiles,
			TocDefaultCtas = [.. dto.TocDefaultCtas],
			Redirects = redirects,
			RedirectsFile = redirectFile?.Source,
			DroppedRedirects = redirectFile?.Dropped ?? [],
			UnknownSuppressions = dto.SuppressDiagnostics is HintTypeSet set ? set.UnknownNames : [],
			TocAuthoring = dto.TocAuthoring
		};

	private static ApiDeclaration ToApi(string productKey, ApiProductSequenceDto sequence) =>
		new(
			productKey,
			[
				.. sequence.Entries.Select(
					e => new ApiDeclarationEntry
					{
						Spec = e.Spec,
						Product = e.Product,
						Repository = e.Repository,
						Children = [.. e.Children.Select(c => c.File)],
						UnknownKeys = e.UnknownKeys,
						CatalogCategories = e.Catalog?.Categories ?? [],
						Location = At(e.Line, e.Column),
						ProductLocation = At(e.ProductLine, e.ProductColumn),
						SpecLocation = At(e.SpecLine, e.SpecColumn),
						RepositoryLocation = At(e.RepositoryLine, e.RepositoryColumn),
						CatalogLocation = e.Catalog is { } c ? At(c.Line, c.Column) : null
					}
				)
			]
		);

	private static SourceLocation? At(int? line, int? column) => line is { } l && column is { } c ? new SourceLocation(l, c) : null;
}
