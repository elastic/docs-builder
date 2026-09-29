// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// Reads <c>docset.yml</c> and resolves its table of contents, loading every nested <c>toc.yml</c>. Only the docset's
/// own files are read; joining against the catalog is <see cref="DocumentationSetResolver"/>'s job.
/// </summary>
public static class DocumentationSetLoader
{
	/// <summary>
	/// Loads <paramref name="docsetFile"/>. A missing file loads as an empty docset, which the resolver then reports.
	/// Input the parser rejects fails the load with its message as an error diagnostic.
	/// </summary>
	public static Result<DocumentationSetFile> Load(IFileInfo docsetFile, ScopedFileSystem? fileSystem = null)
	{
		if (!docsetFile.Exists)
			return Result.Success(new DocumentationSetFile());

		var buffer = new BufferingDiagnosticsCollector();
		try
		{
			var docset = DocumentationSetFile.LoadAndResolve(buffer, docsetFile, fileSystem);
			return Result.Success(docset, buffer.Diagnostics);
		}
		catch (Exception e)
		{
			buffer.EmitError(docsetFile, e.Message);
			return Result.Failure<DocumentationSetFile>(buffer.Diagnostics);
		}
	}
}
