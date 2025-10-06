// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation;

public interface IDocumentationContext
{
	IDiagnosticsCollector Collector { get; }
	IFileSystem ReadFileSystem { get; }
	IFileSystem WriteFileSystem { get; }
	IDirectoryInfo OutputDirectory { get; }
	IFileInfo ConfigurationPath { get; }
}

public interface IDocumentationSetContext : IDocumentationContext
{
	IDirectoryInfo DocumentationSourceDirectory { get; }
	GitCheckoutInformation Git { get; }
}

public static class DocumentationContextExtensions
{
	public static void EmitError(this IDocumentationContext context, IFileInfo file, string message, Exception? e = null) =>
		context.Collector.EmitError(file, message, e);

	public static void EmitWarning(this IDocumentationContext context, IFileInfo file, string message) =>
		context.Collector.EmitWarning(file, message);

}
